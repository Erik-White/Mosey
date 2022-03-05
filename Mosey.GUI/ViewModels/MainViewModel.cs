using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mosey.GUI.Configuration;
using Mosey.GUI.Models;
using Mosey.GUI.ViewModels.Extensions;
using Mosey.Models;
using Mosey.Models.Imaging;

namespace Mosey.GUI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IViewModelParent<IViewModel>, IClosing, IDisposable
    {
        // From IoC container
        private readonly IFactory<IIntervalTimer> _timerFactory;
        private readonly Services.UIServices _uiServices;
        private readonly IViewModel _settingsViewModel;
        private readonly IOptionsMonitor<AppSettings> _appSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<MainViewModel> _log;

        // From constructor
        private readonly IIntervalTimer _scanTimer;
        private readonly IIntervalTimer _uiTimer;
        private readonly DialogViewModel _dialog;

        // Configuration
        private IImagingDeviceConfig _imageConfig;
        private IImageFileConfig _imageFileConfig;
        private IIntervalTimerConfig _scanTimerConfig;
        private DeviceConfig _userDeviceConfig;

        // Threading
        private readonly object _scanningDevicesLock = new();
        private static readonly StaTaskScheduler _staQueue = new(concurrencyLevel: 1);
        private static readonly SemaphoreSlim _staSemaphore = new(1, 1);
        private CancellationTokenSource _cancelScanTokenSource = new();

        #region Properties
        public string ImageFormat
        {
            get => _imageFileConfig.Format;
            set
            {
                _imageFileConfig.Format = value;
                RaisePropertyChanged(nameof(ImageFormat));
            }
        }

        public List<string> ImageFormatSupported
            => _imageFileConfig.SupportedFormats;

        private string _imageSavePath;
        /// <summary>
        /// The directory path used to store images obtained when scanning.
        /// </summary>
        public string ImageSavePath
        {
            get
            {
                _imageSavePath ??= _imageFileConfig.Directory;

                return _imageSavePath;
            }
            set
            {
                _imageSavePath = value;
                RaisePropertyChanged(nameof(ImageSavePath));
            }
        }

        /// <summary>
        /// The estimated amount of disk space required for a full run of images, in bytes.
        /// </summary>
        /// <remarks>
        /// Calculated by multiplying the current number of <see cref="ScanRepetitions"/> by the
        /// number of currently enabled scanners, by the selected image resolution file size.
        /// </remarks>
        public long ImagesRequiredDiskSpace
            => ScanRepetitions
                * ScanningDevices.Devices.Count(d => d.IsEnabled)
                * _userDeviceConfig.GetResolutionMetaData(_imageConfig.Resolution).FileSize;

        private bool _isWaiting;
        public bool IsWaiting
        {
            get => _isWaiting;
            set
            {
                _isWaiting = value;
                RaisePropertyChanged(nameof(IsWaiting));
            }
        }

        private TimeSpan _scanDelay;
        public int ScanDelay
        {
            get
            {
                // Use the default setting if no value is set
                _scanDelay = _scanDelay > TimeSpan.Zero ? _scanDelay : _scanTimerConfig.Delay;

                return (int)_scanDelay.TotalMinutes;
            }
            set
            {
                _scanDelay = TimeSpan.FromMinutes(value < 1 ? 1 : value);
                RaisePropertyChanged(nameof(ScanDelay));
            }
        }

        private TimeSpan _scanInterval;
        public int ScanInterval
        {
            get
            {
                // Use the default setting if no value is set
                _scanInterval = _scanInterval > TimeSpan.Zero ? _scanInterval : _scanTimerConfig.Interval;

                return (int)_scanInterval.TotalMinutes;
            }
            set
            {
                _scanInterval = TimeSpan.FromMinutes(value < 1 ? 1 : value);
                RaisePropertyChanged(nameof(ScanInterval));
            }
        }

        private int _scanRepetitions;
        public int ScanRepetitions
        {
            get
            {
                // Use the default setting if no value is set
                _scanRepetitions = _scanRepetitions > 0
                    ? _scanRepetitions
                    : _scanTimerConfig.Repetitions;

                return _scanRepetitions;
            }
            set
            {
                _scanRepetitions = value < 1 ? 1 : value;
                RaisePropertyChanged(nameof(ScanRepetitions));
            }
        }

        public int ScanRepetitionsCount
            => _scanTimer is not null ? _scanTimer.RepetitionsCount : 0;

        public bool IsScanRunning
            => _scanTimer is not null && (_scanTimer.Enabled || ScanningDevices.IsImagingInProgress);

        public TimeSpan ScanNextTime
        {
            get
            {
                if (IsScanRunning && _scanTimer.RepetitionsCount != 0)
                {
                    var scanNext = _scanTimer.StartTime.Add((_scanTimer.RepetitionsCount) * _scanTimer.Interval);
                    return scanNext.Subtract(DateTime.Now);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public DateTime ScanFinishTime
            => IsScanRunning ? _scanTimer.FinishTime : DateTime.MinValue;

        /// <inheritdoc cref="IImagingDevices{T}"/>
        public IImagingDevices<IImagingDevice> ScanningDevices { get; private set; }

        public ICollection<IViewModel> ViewModelChildren { get; private set; }
            = new System.Collections.ObjectModel.ObservableCollection<IViewModel>();

        public SettingsViewModel SettingsViewModel
        {
            get
            {
                var settingsViewModel = ViewModelChildren.FirstOrDefault(child => child is SettingsViewModel);
                if (settingsViewModel is null)
                {
                    settingsViewModel = AddChildViewModel(_settingsViewModel);
                }

                return (SettingsViewModel)settingsViewModel;
            }
        }
        #endregion Properties

        public MainViewModel(
            IFactory<IIntervalTimer> intervalTimerFactory,
            IImagingDevices<IImagingDevice> imagingDevices,
            Services.UIServices uiServices,
            IViewModel settingsViewModel,
            IOptionsMonitor<AppSettings> appSettings,
            IFileSystem fileSystem,
            ILogger<MainViewModel> logger)
        {
            _timerFactory = intervalTimerFactory;
            ScanningDevices = imagingDevices;
            _uiServices = uiServices;
            _settingsViewModel = settingsViewModel;
            _appSettings = appSettings;
            _fileSystem = fileSystem;
            _log = logger;

            _scanTimer = intervalTimerFactory.Create();
            _uiTimer = intervalTimerFactory.Create();
            _dialog = new DialogViewModel(this, _uiServices, _log);

            Initialize();
        }

        public override IViewModel Create()
            => new MainViewModel(
                intervalTimerFactory: _timerFactory,
                imagingDevices: ScanningDevices,
                uiServices: _uiServices,
                settingsViewModel: _settingsViewModel,
                appSettings: _appSettings,
                fileSystem: _fileSystem,
                logger: _log);

        #region Commands
        private ICommand _EnableScannersCommand;
        public ICommand EnableScannersCommand
        {
            get
            {
                _EnableScannersCommand ??= new RelayCommand(
                    o => ScanningDevices.EnableAll(),
                    o => !ScanningDevices.IsEmpty && !IsScanRunning);

                return _EnableScannersCommand;
            }
        }

        private ICommand _ManualScanCommand;
        public ICommand ManualScanCommand
        {
            get
            {
                _ManualScanCommand ??= new RelayCommand(
                    o => _ = ScanAsync(),
                    o => !ScanningDevices.IsEmpty && !ScanningDevices.IsImagingInProgress && !IsScanRunning);

                return _ManualScanCommand;
            }
        }

        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                _StartScanCommand ??= new AsyncCommand(
                    () => StartScanWithDialog(),
                    _ => !ScanningDevices.IsEmpty && !ScanningDevices.IsImagingInProgress && !IsScanRunning && !_scanTimer.Paused);

                return _StartScanCommand;
            }
        }

        public ICommand StartStopScanCommand
            => IsScanRunning ? StopScanCommand : StartScanCommand;

        private ICommand _PauseScanCommand;
        public ICommand PauseScanCommand
        {
            get
            {
                _PauseScanCommand ??= new RelayCommand(o => _scanTimer.Pause(), o => IsScanRunning && !_scanTimer.Paused);

                return _PauseScanCommand;
            }
        }

        private ICommand _ResumeScanCommand;
        public ICommand ResumeScanCommand
        {
            get
            {
                _ResumeScanCommand ??= new RelayCommand(o => _scanTimer.Resume(), o => _scanTimer.Paused);

                return _ResumeScanCommand;
            }
        }

        private IAsyncCommand _StopScanCommand;
        public IAsyncCommand StopScanCommand
        {
            get
            {
                _StopScanCommand ??= new AsyncCommand(
                    () => StopScanWithDialog(),
                    _ => IsScanRunning && !_cancelScanTokenSource.IsCancellationRequested);

                return _StopScanCommand;
            }
        }

        private ICommand _RefreshScannersCommand;
        public ICommand RefreshScannersCommand
        {
            get
            {
                _RefreshScannersCommand ??= new RelayCommand(
                    o => RefreshDevices(),
                    o => !ScanningDevices.IsImagingInProgress && !IsScanRunning);

                return _RefreshScannersCommand;
            }
        }

        private ICommand _SelectFolderCommand;
        public ICommand SelectFolderCommand
        {
            get
            {
                _SelectFolderCommand ??= new RelayCommand(o => ImageDirectoryDialog());

                return _SelectFolderCommand;
            }
        }
        #endregion Commands

        /// <summary>
        /// Add an <see cref="IViewModel"/> to this parent collection
        /// </summary>
        /// <param name="viewModelFactory"></param>
        /// <returns>A new <see cref="IViewModel"/> instance</returns>
        public IViewModel AddChildViewModel(IFactory<IViewModel> viewModelFactory)
        {
            var viewModel = viewModelFactory.Create();
            ViewModelChildren.Add(viewModel);

            return viewModel;
        }

        /// <summary>
        /// Load configuration and set initial state.
        /// </summary>
        private void Initialize()
        {
            _log.LogDebug($"ViewModel initialization starting.");

            // Load saved configuration values
            var userSettings = _appSettings.Get("UserSettings");
            UpdateConfig(userSettings);

            // Lock scanners collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ScanningDevices.Devices, _scanningDevicesLock);

            // Register event callbacks
            _appSettings.OnChange(UpdateConfig);
            _scanTimer.Tick += ScanTimer_Tick;
            _scanTimer.Complete += ScanTimer_Complete;
            _uiTimer.Tick += UITimer_Tick;
            _uiTimer.Start(userSettings.UITimer.Delay, userSettings.UITimer.Interval);

            // Start a task loop to update the scanners collection
            _ = RefreshDevicesAsync();
            ScanningDevices.EnableAll();

            _log.LogDebug($"ViewModel initialization complete.");
        }

        /// <summary>
        /// Update local configuration from supplied <see cref="AppSettings"/>.
        /// </summary>
        /// <param name="settings">Application configuration settings</param>
        internal void UpdateConfig(AppSettings settings)
        {
            if (IsScanRunning)
            {
                return;
            }

            _scanTimerConfig = (IIntervalTimerConfig)settings.ScanTimer.Clone();
            _imageConfig = (IImagingDeviceConfig)settings.Image.Clone();
            _imageFileConfig = (IImageFileConfig)settings.ImageFile.Clone();
            _userDeviceConfig = settings.Device;

            RaisePropertyChanged(nameof(ScanInterval));
            RaisePropertyChanged(nameof(ScanRepetitions));
            RaisePropertyChanged(nameof(ImageSavePath));
            _log.LogDebug($"Configuration updated with {nameof(UpdateConfig)}.");
        }

        /// <summary>
        /// Begin repeated scanning with an <see cref="IIntervalTimer"/>.
        /// </summary>
        public void StartScan()
        {
            _cancelScanTokenSource = new CancellationTokenSource();
            _scanTimer.Start(_scanDelay, _scanInterval, ScanRepetitions);
            _log.LogDebug("IntervalTimer started. Delay: {Delay} Interval: {Interval} Repetitions: {Repetitions}", _scanDelay, _scanInterval, ScanRepetitions);

            RaisePropertyChanged(nameof(IsScanRunning));
            RaisePropertyChanged(nameof(ScanFinishTime));
            RaisePropertyChanged(nameof(StartStopScanCommand));
            _log.LogInformation("Scanning started with {ScanRepetitions} repetitions to complete.", ScanRepetitions);
        }

        /// <summary>
        /// Begin repeated scanning, after first checking if checking interval time and free disk space are sufficient.
        /// </summary>
        public async Task StartScanWithDialog()
        {
            // Check that interval time is sufficient for selected resolution
            var imagingTime = ScanningDevices.Devices.Count(d => d.IsEnabled) * _userDeviceConfig.GetResolutionMetaData(_imageConfig.Resolution).ImagingTime;
            if (imagingTime * 1.5 > TimeSpan.FromMinutes(ScanInterval) && !await _dialog.ImagingTimeDialog(TimeSpan.FromMinutes(ScanInterval), imagingTime))
            {
                _log.LogDebug($"Scanning not started due to low interval time: {imagingTime.TotalMinutes} minutes required, {ScanInterval} minutes selected.");
                return;
            }

            // Check that disk space is sufficient for selected resolution
            try
            {
                var availableDiskSpace = FileSystemExtensions.AvailableFreeSpace(
                    _fileSystem.Path.GetPathRoot(_imageFileConfig.Directory),
                    _fileSystem);
                if (ImagesRequiredDiskSpace * 1.5 > availableDiskSpace && !await _dialog.DiskSpaceDialog(ImagesRequiredDiskSpace, availableDiskSpace))
                {
                    _log.LogDebug($"Scanning not started due to low disk space: {StringFormat.ByteSize(ImagesRequiredDiskSpace)} required, {StringFormat.ByteSize(availableDiskSpace)} available.");
                    return;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.LogWarning(ex, $"Unable to show {nameof(DialogViewModel.DiskSpaceDialog)} on path {_imageFileConfig.Directory} due to {ex.GetType()}");
            }

            StartScan();
        }

        /// <summary>
        /// Halts scanning and scan timer.
        /// </summary>
        public void StopScan()
        {
            _log.LogInformation("Scanning stopped with {ScanRepetitionsCount} of {ScanRepetitions} repetitions completed.", ScanRepetitionsCount, ScanRepetitions);
            _cancelScanTokenSource.Cancel();
            _scanTimer.Stop();
        }

        /// <summary>
        /// Stops scanning based on user input.
        /// </summary>
        public async Task StopScanWithDialog()
        {
            if (await _dialog.StopScanDialog(cancellationToken: _cancelScanTokenSource.Token))
            {
                StopScan();
            }
        }

        /// <summary>
        /// Initiate scanning with <see cref="ScanAsync"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ScanTimer_Tick(object sender, EventArgs e)
        {
            _log.LogDebug($"{nameof(Scan)} event.");

            try
            {
                await ScanAsync(_cancelScanTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                _log.LogInformation(ex, "Scanning cancelled before it could be completed");
            }
            catch (Exception ex)
            {
                // An unhandled error occurred, notify the user and cancel scanning
                _log.LogError(ex, ex.Message);
                await _dialog.ExceptionDialog(ex);
                StopScan();
            }

            // Update progress
            RaisePropertyChanged(nameof(ScanNextTime));
            RaisePropertyChanged(nameof(ScanRepetitionsCount));
        }

        /// <summary>
        /// Tidy up after scanning is completed
        /// Raised by <see cref="_scanTimer"/>
        /// </summary>
        private async void ScanTimer_Complete(object sender, EventArgs e)
        {
            _log.LogDebug($"{nameof(ScanTimer_Complete)} event.");
            // Wait for scanning to complete
            await _staSemaphore.WaitAsync();

            // Ensure all other scanning related operations are stopped
            _cancelScanTokenSource.Cancel();

            // Apply any changes to settings that were made during scanning
            UpdateConfig(_appSettings.Get("UserSettings"));

            // Update properties
            RaisePropertyChanged(nameof(ScanRepetitionsCount));
            RaisePropertyChanged(nameof(IsScanRunning));
            RaisePropertyChanged(nameof(ScanFinishTime));
            RaisePropertyChanged(nameof(StartStopScanCommand));

            _log.LogInformation("Scanning complete.");
            _staSemaphore.Release();
        }

        /// <summary>
        /// Update user interface components
        /// Raised by <see cref="_uiTimer"/>
        /// </summary>
        private void UITimer_Tick(object sender, EventArgs e)
            => RaisePropertyChanged(nameof(ScanNextTime));

        /// <summary>
        /// Update <see cref="ScanningDevices"/> with any newly connected scanners
        /// </summary>
        private void RefreshDevices()
        {
            _log.LogDebug($"Device refresh initiated with {nameof(RefreshDevices)}");
            var enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;
            ScanningDevices.RefreshDevices(_imageConfig, enableDevices);

            RaisePropertyChanged(nameof(ScanningDevices));
            RaisePropertyChanged(nameof(StartScanCommand));
            RaisePropertyChanged(nameof(StartStopScanCommand));
            _log.LogDebug($"Device refresh complete with {nameof(RefreshDevices)}");
        }

        /// <summary>
        /// Starts a loop that continually refreshes the list of available scanners
        /// </summary>
        /// <param name="intervalSeconds">The duration between refreshes</param>
        /// <param name="cancellationToken">Used to stop the refresh loop</param>
        internal async Task RefreshDevicesAsync(int intervalSeconds = 1, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Device refresh initiated with {nameof(RefreshDevicesAsync)}");
            while (true)
            {
                // Exit at a safe point in the loop, if requested
                if (cancellationToken.IsCancellationRequested)
                {
                    _log.LogDebug($"Device refresh in {nameof(RefreshDevicesAsync)} cancelled with CancellationToken.");
                    return;
                }

                // Wait until all other staQueue operations are complete
                await _staSemaphore.WaitAsync(CancellationToken.None);

                try
                {
                    _log.LogTrace($"Initiating device refresh in {nameof(RefreshDevicesAsync)}");
                    var enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;

                    // Use a dedicated thread for refresh tasks
                    // The apartment state MUST be single threaded for COM interop
                    await Task.Factory.StartNew(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(intervalSeconds)).Wait();
                        ScanningDevices.RefreshDevices(_imageConfig, enableDevices);
                    }, cancellationToken, TaskCreationOptions.None, _staQueue);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Error while attempting to refresh devices.");
                    await _dialog.ExceptionDialog(ex, 5000, CancellationToken.None);
                }
                finally
                {
                    RaisePropertyChanged(nameof(ScanningDevices));
                    RaisePropertyChanged(nameof(StartScanCommand));
                    RaisePropertyChanged(nameof(StartStopScanCommand));
                    _log.LogTrace($"Device refresh completed in {nameof(RefreshDevicesAsync)}");
                    _staSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s
        /// </summary>
        /// <param name="cancellationToken">Used to stop current scanning operations</param>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        /// <exception cref="OperationCanceledException">If scanning was cancelled before completion</exception>
        public List<string> Scan(CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Scanning initiated with {nameof(Scan)} method.");
            var scannerIDStr = string.Empty;
            var saveDateTime = DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat));
            var saveDirectory = ImageSavePath;
            var imagePaths = new List<string>();

            // Order devices by ID to provide clearer feedback to users
            foreach (var scanner in ScanningDevices.Devices.OrderBy(o => o.DeviceID).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    scannerIDStr = scanner.ID.ToString();

                    if (scanner.IsConnected && scanner.IsEnabled && !scanner.IsImaging)
                    {
                        // Update image config in case of changes
                        scanner.ImageSettings = _imageConfig;

                        // Set desired resolution
                        if (_userDeviceConfig.UseHighestResolution)
                        {
                            scanner.ImageSettings.Resolution = scanner.SupportedResolutions.Max();
                        }

                        // Run the scanner and retrieve the image(s) to memory
                        _log.LogDebug("Attempting to retrieve image on {ScannerName} (#{DeviceID})", scanner.Name, scannerIDStr);
                        scanner.GetImage();

                        if (scanner.Images.Count > 0)
                        {
                            _log.LogDebug("{ImageCount} images retrieved from scanner #{DeviceID}", scanner.Images.Count, scanner.ID);
                            var fileName = string.Join("_", _imageFileConfig.Prefix, saveDateTime);
                            var directory = _fileSystem.Path.Combine(saveDirectory, string.Join(string.Empty, "Scanner", scannerIDStr));
                            _fileSystem.Directory.CreateDirectory(directory);

                            // Write image(s) to filesystem and retrieve a list of saved file names
                            var savedImages = scanner.SaveImage(fileName, directory: directory, fileFormat: ImageFormat);
                            foreach (var image in savedImages)
                            {
                                imagePaths.Add(image);
                                _log.LogInformation("Saved image from scanner #{DeviceID} to file: {ImagePath}", scanner.ID, image);
                            }
                        }
                        else
                        {
                            _log.LogWarning("Scanning successful, but no images retrieved from {DeviceName} (#{DeviceID})", scanner.Name, scanner.ID);
                        }
                    }
                }
                catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
                {
                    // Device will show as disconnected, no images returned
                    _log.LogWarning(ex, "Communication error on scanner {DeviceName} (#{DeviceID}) while attempting scan.", scanner.Name, scannerIDStr);
                    return imagePaths;
                }
            }

            _log.LogDebug($"Scan completed with {nameof(Scan)} method.");

            return imagePaths;
        }

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s
        /// </summary>
        /// <param name="cancellationToken">Used to stop current scanning operations</param>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        /// <exception cref="OperationCanceledException">If scanning was cancelled before completion</exception>
        private async Task<List<string>> ScanAsync(CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Scan initiated with {nameof(ScanAsync)} method.");
            var results = new List<string>();

            // Ensure device refresh or other operations are complete
            await _staSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                // The apartment state MUST be single threaded for COM interop
                // Runtime callable wrappers must be disposed manually to prevent problems with early disposal of COM servers
                using (var staQueue = new StaTaskScheduler(concurrencyLevel: 1, disableComObjectEagerCleanup: true))
                {
                    await Task.Factory.StartNew(() =>
                    {
                        // Obtain images from scanners
                        results = Scan(cancellationToken);
                    }, cancellationToken, TaskCreationOptions.LongRunning, staQueue)
                    .ContinueWith(t =>
                    {
                        // Manually clear (RCWs) to prevent memory leaks
                        System.Runtime.InteropServices.Marshal.CleanupUnusedObjectsInCurrentContext();
                        // Force any pending exceptions to propagate up
                        t.Wait();
                    }, staQueue);
                }
            }
            catch (AggregateException aggregateEx)
            {
                // Unpack the aggregate
                var innerException = aggregateEx.Flatten().InnerExceptions.FirstOrDefault();
                // Ensure stack trace is preserved and rethrow
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(innerException).Throw();
            }
            finally
            {
                _staSemaphore.Release();
            }

            _log.LogDebug($"Scan completed with {nameof(ScanAsync)} method.");

            return results;
        }

        /// <summary>
        /// Display a dialog window that allows the user to select the image save location.
        /// </summary>
        private void ImageDirectoryDialog()
        {
            // Go up one level so users can see the initial directory instead of starting inside it
            var initialDirectory = _fileSystem.Directory.GetParent(ImageSavePath).FullName;
            if (string.IsNullOrWhiteSpace(initialDirectory))
            {
                initialDirectory = ImageSavePath;
            }

            var selectedDirectory = _dialog.FolderBrowserDialog(
                initialDirectory,
                "Choose the image file save location");

            // Only update the property if a path was actually returned
            if (!string.IsNullOrWhiteSpace(selectedDirectory))
            {
                ImageSavePath = selectedDirectory;
            }
        }

        public bool OnClosing() => false;

        public async void OnClosingAsync()
        {
            if (IsScanRunning)
            {
                // Check with user before exiting
                var dialogResult = await _dialog.StopScanDialog(cancellationToken: _cancelScanTokenSource.Token);
                if (dialogResult)
                {
                    // Wait for current scan operation to complete, then exit
                    _log.LogDebug($"Waiting for scanning operations to complete before shutting down from {nameof(OnClosingAsync)}.");
                    IsWaiting = true;
                    await _staSemaphore?.WaitAsync();
                    _log.LogInformation("User closing application before all scans completed. {ScanRepetitionsCount} of {ScanRepetitions} repetitions completed.", ScanRepetitionsCount, ScanRepetitions);
                    _log.LogDebug($"Application shutdown requested from {nameof(OnClosingAsync)}.");
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                // Exit immediately
                _log.LogDebug($"Application shutdown requested from {nameof(OnClosingAsync)}.");
                System.Windows.Application.Current.Shutdown();
            }
        }

        public void Dispose()
        {
            _scanTimer.Tick -= ScanTimer_Tick;
            _scanTimer.Complete -= ScanTimer_Complete;
            _uiTimer.Tick -= UITimer_Tick;

            _cancelScanTokenSource?.Cancel();
            _cancelScanTokenSource?.Dispose();
            _scanTimer?.Dispose();
            _uiTimer?.Dispose();
            _staQueue?.Dispose();
        }
    }
}
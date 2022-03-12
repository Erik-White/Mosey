using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IFactory<IIntervalTimer> _timerFactory;
        private readonly IImagingHost _scanningHost;
        private readonly Services.UIServices _uiServices;
        private readonly IViewModel _settingsViewModel;
        private readonly IOptionsMonitor<AppSettings> _appSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<MainViewModel> _log;
        private readonly object _scanningDevicesLock = new();

        private readonly IIntervalTimer _scanTimer;
        private readonly IIntervalTimer _uiTimer;
        private readonly DialogViewModel _dialog;

        private ImagingDeviceConfig _imageConfig;
        private ImageFileConfig _imageFileConfig;
        private IntervalTimerConfig _scanTimerConfig;
        private ScanningDeviceConfig _userDeviceConfig;

        private CancellationTokenSource _cancelScanTokenSource = new();

        #region Properties
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
            => _scanTimer is not null && (_scanTimer.Enabled || _scanningHost.ImagingDevices.IsImagingInProgress);

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

        /// <inheritdoc cref="IImagingDevices{T}"/>
        public ObservableItemsCollection<IImagingDevice> ObservableDevices
            => (ObservableItemsCollection<IImagingDevice>)ScanningDevices.Devices;

        public ICollection<IViewModel> ViewModelChildren { get; private set; }
            = new System.Collections.ObjectModel.ObservableCollection<IViewModel>();

        public SettingsViewModel SettingsViewModel
        {
            get
            {
                if (!ViewModelChildren.Any(ViewModels => ViewModels is SettingsViewModel))
                {
                    AddChildViewModel(_settingsViewModel);
                }

                return (SettingsViewModel)_settingsViewModel;
            }
        }
        #endregion Properties

        public MainViewModel(
            IFactory<IIntervalTimer> intervalTimerFactory,
            IImagingHost scanningHost,
            Services.UIServices uiServices,
            IViewModel settingsViewModel,
            IOptionsMonitor<AppSettings> appSettings,
            IFileSystem fileSystem,
            ILogger<MainViewModel> logger)
        {
            _timerFactory = intervalTimerFactory;
            _scanningHost = scanningHost;
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
                scanningHost: _scanningHost,
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
                    o => _ = ScanAndSaveImagesAsync(),
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

        private IAsyncCommand _RefreshScannersCommand;
        public IAsyncCommand RefreshScannersCommand
        {
            get
            {
                _RefreshScannersCommand ??= new AsyncCommand(
                    () => RefreshDevicesAsync(),
                    _ => !ScanningDevices.IsImagingInProgress && !IsScanRunning);

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

            ScanningDevices = _scanningHost.ImagingDevices;

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
            _ = BeginRefreshDevicesAsync();
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

            _scanTimerConfig = settings.ScanTimer with { };
            _imageConfig = settings.Image with { };
            _imageFileConfig = settings.ImageFile with { };
            _userDeviceConfig = settings.Device;

            _scanningHost.UpdateConfig(_imageConfig);

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
        /// Begin scanning at intervals, after first checking if checking interval time and free disk space are sufficient.
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
        /// Initiate scanning with <see cref="ScanAndSaveImagesAsync"/>.
        /// </summary>
        private async void ScanTimer_Tick(object sender, EventArgs e)
        {
            _log.LogDebug($"{nameof(ScanTimer_Tick)} event.");

            try
            {
                await ScanAndSaveImagesAsync();
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
        /// </summary>
        private async void ScanTimer_Complete(object sender, EventArgs e)
        {
            _log.LogDebug($"{nameof(ScanTimer_Complete)} event.");

            if (!_cancelScanTokenSource.Token.IsCancellationRequested)
            {
                await _scanningHost.WaitForImagingToComplete(_cancelScanTokenSource.Token);
            }

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
        }

        /// <summary>
        /// Update user interface components
        /// </summary>
        private void UITimer_Tick(object sender, EventArgs e)
            => RaisePropertyChanged(nameof(ScanNextTime));

        /// <summary>
        /// Starts a loop that continually refreshes the list of available scanners
        /// </summary>
        /// <param name="interval">The duration between refreshes, default is one second</param>
        /// <param name="cancellationToken">Used to stop the refresh loop</param>
        internal async Task BeginRefreshDevicesAsync(TimeSpan? interval = null, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Device refresh initiated with {nameof(BeginRefreshDevicesAsync)}");
            interval ??= TimeSpan.FromSeconds(1);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _log.LogTrace($"Initiating device refresh in {nameof(BeginRefreshDevicesAsync)}");
                    var enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;

                    await Task.Delay(interval.Value, cancellationToken).ConfigureAwait(false);
                    await _scanningHost.RefreshDevicesAsync(enableDevices, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Error while attempting to refresh devices.");
                    await _dialog.ExceptionDialog(ex, 5000, CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    RaisePropertyChanged(nameof(ScanningDevices));
                    RaisePropertyChanged(nameof(StartScanCommand));
                    RaisePropertyChanged(nameof(StartStopScanCommand));
                    _log.LogTrace($"Device refresh completed in {nameof(BeginRefreshDevicesAsync)}");
                }
            }
        }

        internal static string GetImageFilePath(IImagingHost.CapturedImage image, ImageFileConfig config, bool appendIndex, DateTime saveDateTime)
        {
            string index = appendIndex ? image.Index.ToString() : null;
            var dateTime = saveDateTime.ToString(string.Join("_", config.DateFormat, config.TimeFormat));
            var directory = Path.Combine(config.Directory, $"Scanner{image.DeviceId}");
            var fileName = string.Join("_", config.Prefix, dateTime, index);

            return Path.Combine(directory, Path.ChangeExtension(fileName, config.ImageFormat.ToString().ToLower()));
        }

        private async Task<IEnumerable<IImagingHost.CapturedImage>> ScanImagesAsync()
            => await _scanningHost
                .PerformImagingAsync(_userDeviceConfig.UseHighestResolution, _cancelScanTokenSource.Token)
                .ConfigureAwait(false);

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s
        /// and save any captured images to disk
        /// </summary>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        private async Task<IEnumerable<string>> ScanAndSaveImagesAsync(bool createDirectoryPath = true)
        {
            _log.LogDebug($"Scan initiated with {nameof(ScanAndSaveImagesAsync)} method.");

            var results = new List<string>();
            var saveDateTime = DateTime.Now;

            try
            {
                var images = (await ScanImagesAsync().ConfigureAwait(false)).ToList();

                foreach (var scannedImage in images)
                {
                    var filePath = GetImageFilePath(scannedImage, _imageFileConfig, images.Count > 1, saveDateTime);
                    if (createDirectoryPath)
                    {
                        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(filePath));
                    }
                    _scanningHost.ImageFileHandler.SaveImage(scannedImage.Image, _imageFileConfig.ImageFormat, filePath);

                    results.Add(filePath);
                }
            }
            catch (IOException ex)
            {
                _log.LogError("An error occured when attempting to aquire images, or when saving them to disk", ex);
                await _dialog.ExceptionDialog(ex, 5000, _cancelScanTokenSource.Token).ConfigureAwait(false);
            }

            _log.LogDebug($"Scan completed with {nameof(ScanAndSaveImagesAsync)} method.");

            return results;
        }

        /// <summary>
        /// Update <see cref="ScanningDevices"/> with any newly connected scanners
        /// </summary>
        private async Task RefreshDevicesAsync()
        {
            _log.LogDebug($"Device refresh initiated with {nameof(RefreshDevicesAsync)}");
            var enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;
            await _scanningHost.RefreshDevicesAsync(enableDevices);

            RaisePropertyChanged(nameof(ScanningDevices));
            RaisePropertyChanged(nameof(StartScanCommand));
            RaisePropertyChanged(nameof(StartStopScanCommand));
            _log.LogDebug($"Device refresh complete with {nameof(RefreshDevicesAsync)}");
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
                    _cancelScanTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
                    await _scanningHost.WaitForImagingToComplete(_cancelScanTokenSource.Token);
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
        }
    }
}
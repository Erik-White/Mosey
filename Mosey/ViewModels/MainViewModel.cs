using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncAwaitBestPractices.MVVM;
using Mosey.Models;
using Mosey.Configuration;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase, IViewModelParent<IViewModel>, IClosing, IDisposable
    {
        private ILogger<MainViewModel> _log;
        private IIntervalTimer _uiTimer;
        private IIntervalTimer _scanTimer;
        private IImagingDevices<IImagingDevice> _DevicesCollection;
        private Models.Dialog.IDialogManager _dialogManager;
        private Models.Dialog.IFolderBrowserDialog _folderBrowserDialog;
        private IViewModel _settingsViewModel;

        private DialogViewModel _dialog;
        private IOptionsMonitor<AppSettings> _appSettings;
        private IImagingDeviceConfig _imageConfig;
        private IImageFileConfig _imageFileConfig;
        private IIntervalTimerConfig _scanTimerConfig;
        private ITimerConfig _uiTimerConfig;
        private DeviceConfig _userDeviceConfig;

        private readonly object _DevicesCollectionLock = new object();
        private static StaTaskScheduler _staQueue = new StaTaskScheduler(numberOfThreads: 1);
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancelScanTokenSource = new CancellationTokenSource();
        private bool _disposed;

         #region Properties
        public string ImageFormat
        {
            get
            {
                return _imageFileConfig.Format;
            }
            set
            {
                _imageFileConfig.Format = value;
                RaisePropertyChanged(nameof(ImageFormat));
            }
        }

        public List<string> ImageFormatSupported
        {
            get
            {
                return _imageFileConfig.SupportedFormats;
            }
        }

        public string ImageSavePath
        {
            get
            {
                return _folderBrowserDialog.SelectedPath ?? _folderBrowserDialog.InitialDirectory;
            }
            set
            {
                _folderBrowserDialog.SelectedPath = value;
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
        {
            get
            {
                return ScanRepetitions
                    * _DevicesCollection.GetByEnabled(true).Count()
                    * _userDeviceConfig.GetResolutionMetaData(_imageConfig.Resolution).FileSize;
            }
        }

        private bool _isWaiting;
        public bool IsWaiting
        {
            get
            {
                return _isWaiting;
            }
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
                if (value < 1)
                {
                    value = 1;
                }
                _scanDelay = TimeSpan.FromMinutes(value);
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
                if (value < 1)
                {
                    value = 1;
                }
                _scanInterval = TimeSpan.FromMinutes(value);
                RaisePropertyChanged(nameof(ScanInterval));
            }
        }

        private int _scanRepetitions;
        public int ScanRepetitions
        {
            get
            {
                // Use the default setting if no value is set
                _scanRepetitions = _scanRepetitions > 0 ? _scanRepetitions : _scanTimerConfig.Repetitions;

                return _scanRepetitions;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanRepetitions = value;
                RaisePropertyChanged(nameof(ScanRepetitions));
            }
        }

        public int ScanRepetitionsCount
        {
            get
            {
                if (_scanTimer != null)
                {
                    return _scanTimer.RepetitionsCount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public bool IsScanRunning
        {
            get
            {
                if (_scanTimer != null)
                {
                    return _scanTimer.Enabled || _DevicesCollection.IsImagingInProgress;
                }
                else
                {
                    return false;
                }
            }
        }

        public TimeSpan ScanNextTime
        {
            get
            {
                if (IsScanRunning && _scanTimer.RepetitionsCount != 0)
                {
                    DateTime scanNext = _scanTimer.StartTime.Add((_scanTimer.RepetitionsCount) * _scanTimer.Interval);
                    return scanNext.Subtract(DateTime.Now);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public DateTime ScanFinishTime
        {
            get
            {
                if (IsScanRunning)
                {
                    return _scanTimer.FinishTime;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        public IImagingDevices<IImagingDevice> DevicesCollection
        {
            get
            {
                return _DevicesCollection;
            }
            set
            {
                _DevicesCollection = value;
                RaisePropertyChanged(nameof(DevicesCollection));
            }
        }

        public ObservableItemsCollection<IImagingDevice> Devices
        {
            get
            {
                return (ObservableItemsCollection<IImagingDevice>)_DevicesCollection.Devices;
            }
        }

        public ICollection<IViewModel> ViewModelChildren { get; private set; } = new System.Collections.ObjectModel.ObservableCollection<IViewModel>();

        public SettingsViewModel SettingsViewModel
        {
            get
            {
                var settingsViewModel = ViewModelChildren.Where(child => child is SettingsViewModel).FirstOrDefault();
                if (settingsViewModel is null)
                {
                    settingsViewModel = AddChildViewModel(_settingsViewModel);
                }

                return (SettingsViewModel)settingsViewModel;
            }
        }
        #endregion Properties

        public MainViewModel(
            IIntervalTimer intervalTimer,
            IImagingDevices<IImagingDevice> imagingDevices,
            Services.UIServices uiServices,
            IViewModel settingsViewModel,
            IOptionsMonitor<AppSettings> appSettings,
            ILogger<MainViewModel> logger
            )
        {
            _scanTimer = intervalTimer;
            _uiTimer = (IIntervalTimer)intervalTimer.Clone();
            _DevicesCollection = imagingDevices;
            _dialogManager = uiServices.DialogManager;
            _folderBrowserDialog = uiServices.FolderBrowserDialog;
            _settingsViewModel = settingsViewModel;

            _appSettings = appSettings;
            _log = logger;
            _dialog = new DialogViewModel(this, _dialogManager, _log);

            // Initialise configuration options
            SetConfiguration();

            _appSettings.OnChange<AppSettings>(UpdateConfig);
            _scanTimer.Tick += ScanTimer_Tick;
            _scanTimer.Complete += ScanTimer_Complete;
            _uiTimer.Tick += UITimer_Tick;
            _uiTimer.Start(_uiTimerConfig.Delay, _uiTimerConfig.Interval);

            // Start a task loop to update the scanners collection
            _ = RefreshDevicesAsync();
        }

        public override IViewModel Create()
        {
            return new MainViewModel(
                intervalTimer: _scanTimer,
                imagingDevices: _DevicesCollection,
                uiServices: new Services.UIServices(
                    dialogManager: _dialogManager,
                    folderBrowserDialog: _folderBrowserDialog
                    ),
                settingsViewModel: _settingsViewModel,
                appSettings: _appSettings,
                logger: _log
                );
        }

        private void SetConfiguration()
        {
            AppSettings userSettings = _appSettings.Get("UserSettings");

            // Load saved configuration values
            _uiTimerConfig = userSettings.UITimer;
            UpdateConfig(userSettings);

            // Lock scanners collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_DevicesCollection.Devices, _DevicesCollectionLock);

            _folderBrowserDialog.InitialDirectory = _imageFileConfig.Directory;
            _folderBrowserDialog.SelectedPath = _folderBrowserDialog.InitialDirectory;

            DevicesCollection.EnableAll();
            _log.LogDebug($"Configuration initialised with {nameof(SetConfiguration)}.");
        }

        private void UpdateConfig(AppSettings settings)
        {
            if (!IsScanRunning)
            {
                _scanTimerConfig = (IIntervalTimerConfig)settings.ScanTimer.Clone();
                _imageConfig = (IImagingDeviceConfig)settings.Image.Clone();
                _imageFileConfig = (IImageFileConfig)settings.ImageFile.Clone();
                _userDeviceConfig = settings.Device;

                RaisePropertyChanged(nameof(ScanInterval));
                RaisePropertyChanged(nameof(ScanRepetitions));
                RaisePropertyChanged(nameof(ImageSavePath));
                _log.LogDebug($"Configuration updated with {nameof(UpdateConfig)}.");
            }
        }

        #region Commands
        private ICommand _EnableScannersCommand;
        public ICommand EnableScannersCommand
        {
            get
            {
                _EnableScannersCommand = new RelayCommand(o => _DevicesCollection.EnableAll(), o => !DevicesCollection.IsEmpty && !IsScanRunning);

                return _EnableScannersCommand;
            }
        }

        private ICommand _ManualScanCommand;
        public ICommand ManualScanCommand
        {
            get
            {
                _ManualScanCommand = new RelayCommand(o => _ = ScanAsync(), o => !DevicesCollection.IsEmpty && !DevicesCollection.IsImagingInProgress && !IsScanRunning);

                return _ManualScanCommand;
            }
        }

        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                _StartScanCommand = new RelayCommand(o => StartScanWithDialog(), o => !DevicesCollection.IsEmpty && !DevicesCollection.IsImagingInProgress && !IsScanRunning && !_scanTimer.Paused);

                return _StartScanCommand;
            }
        }

        public ICommand StartStopScanCommand
        {
            get
            {
                if (IsScanRunning)
                {
                    return StopScanCommand;
                }
                else
                {
                    return StartScanCommand;
                }

            }
        }

        private ICommand _PauseScanCommand;
        public ICommand PauseScanCommand
        {
            get
            {
                if (_PauseScanCommand == null)
                    _PauseScanCommand = new RelayCommand(o => _scanTimer.Pause(), o => IsScanRunning && !_scanTimer.Paused);

                return _PauseScanCommand;
            }
        }

        private ICommand _ResumeScanCommand;
        public ICommand ResumeScanCommand
        {
            get
            {
                if (_ResumeScanCommand == null)
                    _ResumeScanCommand = new RelayCommand(o => _scanTimer.Resume(), o => _scanTimer.Paused);

                return _ResumeScanCommand;
            }
        }

        private IAsyncCommand _StopScanCommand;
        public IAsyncCommand StopScanCommand
        {
            get
            {
                if (_StopScanCommand == null)
                    _StopScanCommand = new AsyncCommand(() => StopScanWithDialog(), _ => IsScanRunning && !_cancelScanTokenSource.IsCancellationRequested);

                return _StopScanCommand;
            }
        }

        private ICommand _RefreshScannersCommand;
        public ICommand RefreshScannersCommand
        {
            get
            {
                if (_RefreshScannersCommand == null)
                    _RefreshScannersCommand = new RelayCommand(o => RefreshDevices(), o => !DevicesCollection.IsImagingInProgress && !IsScanRunning);

                return _RefreshScannersCommand;
            }
        }

        private ICommand _SelectFolderCommand;
        public ICommand SelectFolderCommand
        {
            get
            {
                if (_SelectFolderCommand == null)
                    _SelectFolderCommand = new RelayCommand(o => OpenFolderDialog());

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
        /// Begin repeated scanning, after first checking interval time and free disk space are sufficient.
        /// </summary>
        public async void StartScanWithDialog()
        {
            // TODO: Check that interval time is sufficient for selected resolution

            // Check that disk space is sufficient for selected resolution
            long availableDiskSpace = FileSystemExtensions.AvailableFreeSpace(Path.GetPathRoot(_imageFileConfig.Directory));
            if (ImagesRequiredDiskSpace * 1.5 > availableDiskSpace)
            {
                if (!await _dialog.DiskSpaceDialog(ImagesRequiredDiskSpace, availableDiskSpace))
                {
                    _log.LogDebug($"Scanning not started due to low disk space: {Format.ByteSize(ImagesRequiredDiskSpace)} required, {Format.ByteSize(availableDiskSpace)} available.");
                    return;
                }
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
            if (await _dialog.StopScanDialog(cancellationToken: _cancelScanTokenSource.Token)){
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
            await ScanAsync(_cancelScanTokenSource.Token);

            // Update progress
            RaisePropertyChanged(nameof(ScanNextTime));
            RaisePropertyChanged(nameof(ScanRepetitionsCount));
        }

        /// <summary>
        /// Tidy up after scanning is completed
        /// Raised by <see cref="_scanTimer"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ScanTimer_Complete(object sender, EventArgs e)
        {
            _log.LogDebug($"{nameof(ScanTimer_Complete)} event.");
            // Wait for scanning to complete
            await _semaphore.WaitAsync();

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
            _semaphore.Release();
        }

        /// <summary>
        /// Update user interface components
        /// Raised by <see cref="_uiTimer"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UITimer_Tick(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(ScanNextTime));
        }

        /// <summary>
        /// Update <see cref="DevicesCollection"/> with any newly connected scanners
        /// </summary>
        private void RefreshDevices()
        {
            _log.LogDebug($"Device refresh initiated with {nameof(RefreshDevices)}");
            bool enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;
            DevicesCollection.RefreshDevices(_imageConfig, enableDevices);

            RaisePropertyChanged(nameof(DevicesCollection));
            RaisePropertyChanged(nameof(StartScanCommand));
            RaisePropertyChanged(nameof(StartStopScanCommand));
            _log.LogDebug($"Device refresh complete with {nameof(RefreshDevices)}");
        }

        /// <summary>
        /// Starts a loop that continually refreshes the list of available scanners
        /// </summary>
        /// <param name="intervalSeconds">The duration between refreshes</param>
        /// <param name="cancellationToken">Used to stop the refresh loop</param>
        /// <returns></returns>
        private async Task RefreshDevicesAsync(int intervalSeconds = 1, CancellationToken cancellationToken = default)
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
                await _semaphore.WaitAsync();

                try
                {
                    _log.LogTrace($"Initiating device refresh in {nameof(RefreshDevicesAsync)}");
                    bool enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;

                    // Use a dedicated thread for refresh tasks
                    // The apartment state MUST be single threaded for COM interop
                    await Task.Factory.StartNew(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(intervalSeconds)).Wait();
                        DevicesCollection.RefreshDevices(_imageConfig, enableDevices);
                    }, cancellationToken, TaskCreationOptions.None, _staQueue);
                }
                finally
                {
                    RaisePropertyChanged(nameof(DevicesCollection));
                    RaisePropertyChanged(nameof(StartScanCommand));
                    RaisePropertyChanged(nameof(StartStopScanCommand));
                    _log.LogTrace($"Device refresh completed in {nameof(RefreshDevicesAsync)}");
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s
        /// </summary>
        /// <param name="cancellationToken">Used to stop current scanning operations</param>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        public List<string> Scan(CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Scan initiated with {nameof(Scan)} method.");
            string scannerIDStr = string.Empty;
            string saveDateTime = DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat));
            string saveDirectory = ImageSavePath;
            List<string> imagePaths = new List<string>();

            try
            {
                // Order devices by ID to provide clearer feedback to users
                foreach (IImagingDevice scanner in DevicesCollection.Devices.OrderBy(o => o.DeviceID).ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (scanner.IsConnected && scanner.IsEnabled && !scanner.IsImaging)
                    {
                        scannerIDStr = scanner.ID.ToString();

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

                        if (scanner.Images.Count() > 0)
                        {
                            string fileName = string.Join("_", _imageFileConfig.Prefix, saveDateTime);
                            string directory = Path.Combine(saveDirectory, string.Join(string.Empty, "Scanner", scannerIDStr));
                            Directory.CreateDirectory(directory);

                            // Write image(s) to filesystem and retrieve a list of saved file names
                            IEnumerable<string> savedImages = scanner.SaveImage(fileName, directory: directory, fileFormat: ImageFormat);
                            foreach (string image in savedImages)
                            {
                                imagePaths.Add(image);
                                _log.LogInformation("Saved image from scanner #{DeviceID} to file: {ImagePath}", scannerIDStr, image);
                            }
                        }
                        else
                        {
                            _log.LogWarning("Unable to retrieve image from {DeviceName} (#{DeviceID})", scanner.Name, scannerIDStr);
                        }
                    }
                    if (!scanner.IsConnected)
                    {
                        _log.LogError("Connection to {DeviceName} (#{DeviceID}) lost while attempting scan.", scanner.Name, scannerIDStr);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _log.LogInformation(ex, $"Scanning operation cancelled.");
                return imagePaths;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to retrive image on scanner #{DeviceID}.", scannerIDStr);
                throw;
            }
            _log.LogDebug($"Scan completed with {nameof(Scan)} method.");

            return imagePaths;
        }

        /// <summary>
        /// Initiate scanning with all available <see cref="ScanningDevice"/>s
        /// </summary>
        /// <param name="cancellationToken">Used to stop current scanning operations</param>
        /// <returns><see cref="string"/>s representing file paths for scanned images</returns>
        private async Task<List<string>> ScanAsync(CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"Scan initiated with {nameof(ScanAsync)} method.");
            List<string> results = new List<string>();

            // Ensure device refresh or other operations are complete
            await _semaphore.WaitAsync();

            try
            {
                // The apartment state MUST be single threaded for COM interop
                // Runtime callable wrappers must be disposed manually to prevent problems with early disposal of COM servers
                using (StaTaskScheduler staQueue = new StaTaskScheduler(numberOfThreads: 1, disableComObjectEagerCleanup: true))
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
                    }, staQueue);
                }
            }
            finally
            {
                _semaphore.Release();
            }
            _log.LogDebug($"Scan completed with {nameof(ScanAsync)} method.");

            return results;
        }

        /// <summary>
        /// Display a dialog window that allows the user to select an image save location
        /// </summary>
        private void OpenFolderDialog()
        {
            _folderBrowserDialog.Title = "Choose the image file save location";
            // Go up one directory level, otherwise the dialog starts inside the selected directory
            _folderBrowserDialog.InitialDirectory = Directory.GetParent(ImageSavePath).FullName;

            _folderBrowserDialog.ShowDialog();

            RaisePropertyChanged(nameof(ImageSavePath));
        }

        public bool OnClosing()
        {
            return false;
        }

        public async void OnClosingAsync()
        {
            if (IsScanRunning)
            {
                // Check with user before exiting
                bool dialogResult = await _dialog.StopScanDialog(cancellationToken: _cancelScanTokenSource.Token);
                if (dialogResult)
                {
                    // Wait for current scan operation to complete, then exit
                    _log.LogDebug($"Waiting for scanning operations to complete before shutting down from {nameof(OnClosingAsync)}.");
                    IsWaiting = true;
                    await _semaphore?.WaitAsync();
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
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
                _disposed = true;
            }
        }

        ~MainViewModel()
        {
            Dispose(false);
        }
    }
}
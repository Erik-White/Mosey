using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mosey.Configuration;
using Mosey.Models;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase, IViewModelParent<IViewModel>, IDisposable
    {
        private ILogger<MainViewModel> _log;
        private IIntervalTimer _uiTimer;
        private IIntervalTimer _scanTimer;
        private IImagingDevices<IImagingDevice> _scannerDevices;
        private IFolderBrowserDialog _folderBrowserDialog;
        private IViewModel _settingsViewModel;

        private IOptionsMonitor<AppSettings> _appSettings;
        private IImagingDeviceConfig _imageConfig;
        private IImageFileConfig _imageFileConfig;
        private IIntervalTimerConfig _scanTimerConfig;
        private ITimerConfig _uiTimerConfig;
        private DeviceConfig _userDeviceConfig;

        private readonly object _scannerDevicesLock = new object();
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

        public int ScanDelay
        {
            get
            {
                return (int)_scanTimerConfig.Delay.TotalMinutes;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanTimerConfig.Delay = TimeSpan.FromMinutes(value);
                RaisePropertyChanged(nameof(ScanDelay));
            }
        }

        public int ScanInterval
        {
            get
            {
                return (int)_scanTimerConfig.Interval.TotalMinutes;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanTimerConfig.Interval = TimeSpan.FromMinutes(value);
                RaisePropertyChanged(nameof(ScanInterval));
            }
        }

        public int ScanRepetitions
        {
            get
            {
                return _scanTimerConfig.Repetitions;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanTimerConfig.Repetitions = value;
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
                    return _scanTimer.Enabled;
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
                if (IsScanRunning)
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

        public IImagingDevices<IImagingDevice> ScannerDevices
        {
            get
            {
                return _scannerDevices;
            }
            set
            {
                _scannerDevices = value;
                RaisePropertyChanged(nameof(ScannerDevices));
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
            ILogger<MainViewModel> logger,
            IIntervalTimer intervalTimer,
            IImagingDevices<IImagingDevice> imagingDevices,
            IFolderBrowserDialog folderBrowserDialog,
            IViewModel settingsViewModel,
            IOptionsMonitor<AppSettings> appSettings
        )
        {
            _log = logger;
            _scanTimer = intervalTimer;
            _uiTimer = (IIntervalTimer)intervalTimer.Clone();
            _scannerDevices = imagingDevices;
            _folderBrowserDialog = folderBrowserDialog;
            _settingsViewModel = settingsViewModel;

            _appSettings = appSettings;

            // Set configuration options
            SetConfiguration();

            _appSettings.OnChange<AppSettings>(UpdateConfig);
            _scanTimer.Tick += ScanTimer_Tick;
            _scanTimer.Complete += ScanTimer_Complete;
            _uiTimer.Tick += UITimer_Tick;
            _uiTimer.Start(_uiTimerConfig.Delay, _uiTimerConfig.Interval);

            // Start a task loop to update the scanners collection
            RefreshDevicesAsync();
        }

        public override IViewModel Create()
        {
            return new MainViewModel(
                logger: _log,
                intervalTimer: _scanTimer,
                imagingDevices: _scannerDevices,
                folderBrowserDialog: _folderBrowserDialog,
                settingsViewModel: _settingsViewModel,
                appSettings: _appSettings
                );
        }

        private void SetConfiguration()
        {
            AppSettings userSettings = _appSettings.Get("UserSettings");

            _uiTimerConfig = userSettings.UITimer;
            _scanTimerConfig = (IIntervalTimerConfig)userSettings.ScanTimer.Clone();
            _imageConfig = (IImagingDeviceConfig)userSettings.Image.Clone();
            _imageFileConfig = (IImageFileConfig)userSettings.ImageFile.Clone();
            _userDeviceConfig = userSettings.Device;

            // Lock scanners collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_scannerDevices, _scannerDevicesLock);

            _folderBrowserDialog.InitialDirectory = _imageFileConfig.Directory;
            _folderBrowserDialog.SelectedPath = _folderBrowserDialog.InitialDirectory;

            ScannerDevices.EnableAll();
        }

        private void UpdateConfig(AppSettings settings)
        {
            if (!IsScanRunning)
            {
                _scanTimerConfig = (IIntervalTimerConfig)settings.ScanTimer.Clone();
                _imageConfig = (IImagingDeviceConfig)settings.Image.Clone();
                _imageFileConfig = (IImageFileConfig)settings.ImageFile.Clone();

                RaisePropertyChanged(nameof(ScanInterval));
                RaisePropertyChanged(nameof(ScanRepetitions));
                RaisePropertyChanged(nameof(ImageSavePath));
            }
        }

        #region Commands
        private ICommand _EnableScannersCommand;
        public ICommand EnableScannersCommand
        {
            get
            {
                _EnableScannersCommand = new RelayCommand(o => _scannerDevices.EnableAll(), o => !ScannerDevices.IsEmpty && !IsScanRunning);

                return _EnableScannersCommand;
            }
        }

        private ICommand _ManualScanCommand;
        public ICommand ManualScanCommand
        {
            get
            {
                _ManualScanCommand = new RelayCommand(o => ScanAsync(), o => !ScannerDevices.IsEmpty && !ScannerDevices.IsImagingInProgress && !IsScanRunning);

                return _ManualScanCommand;
            }
        }

        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                _StartScanCommand = new RelayCommand(o => StartScan(), o => !ScannerDevices.IsEmpty && !ScannerDevices.IsImagingInProgress && !IsScanRunning && !_scanTimer.Paused);

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

        private ICommand _StopScanCommand;
        public ICommand StopScanCommand
        {
            get
            {
                if (_StopScanCommand == null)
                    _StopScanCommand = new RelayCommand(o => StopScan(), o => IsScanRunning);

                return _StopScanCommand;
            }
        }

        private ICommand _RefreshScannersCommand;
        public ICommand RefreshScannersCommand
        {
            get
            {
                if (_RefreshScannersCommand == null)
                    _RefreshScannersCommand = new RelayCommand(o => RefreshDevices(), o => !ScannerDevices.IsImagingInProgress && !IsScanRunning);

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

        public IViewModel AddChildViewModel(IFactory<IViewModel> viewModelFactory)
        {
            var viewModel = viewModelFactory.Create();
            ViewModelChildren.Add(viewModel);

            return viewModel;
        }

        public void StartScan()
        {
            _cancelScanTokenSource = new CancellationTokenSource();

            _scanTimer.Start(_scanTimerConfig.Delay, _scanTimerConfig.Interval, _scanTimerConfig.Repetitions);

            RaisePropertyChanged(nameof(IsScanRunning));
            RaisePropertyChanged(nameof(ScanFinishTime));
            RaisePropertyChanged(nameof(StartStopScanCommand));
        }
        public void StopScan()
        {
            _scanTimer.Stop();
            _cancelScanTokenSource.Cancel();
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            ScanAsync(_cancelScanTokenSource.Token);

            // Update progress
            RaisePropertyChanged(nameof(ScanNextTime));
            RaisePropertyChanged(nameof(ScanRepetitionsCount));
        }

        private void ScanTimer_Complete(object sender, EventArgs e)
        {
            // Apply any changes to settings that were made during scanning
            UpdateConfig(_appSettings.Get("UserSettings"));

            // Update properties
            RaisePropertyChanged(nameof(ScanRepetitionsCount));
            RaisePropertyChanged(nameof(IsScanRunning));
            RaisePropertyChanged(nameof(ScanFinishTime));
            RaisePropertyChanged(nameof(StartStopScanCommand));

            _log.LogInformation($"Scanning complete at {DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat))} with {ScanRepetitionsCount} repetitions.");
        }

        private void UITimer_Tick(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(ScanNextTime));
        }

        private void RefreshDevices()
        {
            bool enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;
            ScannerDevices.RefreshDevices(_imageConfig, enableDevices);

            RaisePropertyChanged(nameof(ScannerDevices));
            RaisePropertyChanged(nameof(StartScanCommand));
            RaisePropertyChanged(nameof(StartStopScanCommand));
        }

        private async void RefreshDevicesAsync(int intervalSeconds = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                // Exit at a safe point in the loop, if requested
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Wait until all other staQueue operations are complete
                await _semaphore.WaitAsync();

                try
                {
                    bool enableDevices = !IsScanRunning ? _userDeviceConfig.EnableWhenConnected : _userDeviceConfig.EnableWhenScanning;

                    // Use a dedicated thread for refresh tasks
                    // The apartment state MUST be single threaded for COM interop
                    await Task.Factory.StartNew(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(intervalSeconds)).Wait();
                        ScannerDevices.RefreshDevices(_imageConfig, enableDevices);
                    }, cancellationToken, TaskCreationOptions.None, _staQueue);
                }
                finally
                {
                    RaisePropertyChanged(nameof(ScannerDevices));
                    RaisePropertyChanged(nameof(StartScanCommand));
                    RaisePropertyChanged(nameof(StartStopScanCommand));
                    _semaphore.Release();
                }
            }
        }

        private async void ScanAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Ensure device refresh is complete
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
                        Scan(cancellationToken);
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
        }

        public List<string> Scan(CancellationToken cancellationToken = default(CancellationToken))
        {
            string scannerIDStr = string.Empty;
            string saveDateTime = DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat));
            string saveDirectory = _imageFileConfig.Directory;
            List<string> imagePaths = new List<string>();

            try
            {
                // Order devices by ID to provide clearer feedback to users
                foreach (IImagingDevice scanner in ScannerDevices.OrderBy(o => o.DeviceID).ToList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (scanner.IsConnected && scanner.IsEnabled && !scanner.IsImaging)
                    {
                        scannerIDStr = scanner.ID.ToString();

                        // Run the scanner and retrieve the image(s) to memory
                        scanner.GetImage();
                        _log.LogDebug($"Retrieved image on {scanner.Name} (#{scannerIDStr}) at {saveDateTime}");

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
                                _log.LogInformation($"Saved image file {image} from scanner #{scannerIDStr} at {saveDateTime}");
                            }
                        }
                        else
                        {
                            _log.LogDebug($"Unable to retrieve image on {scanner.Name} (#{scannerIDStr}) at {saveDateTime}");
                        }
                    }
                    if (!scanner.IsConnected)
                    {
                        _log.LogDebug($"Connection to {scanner.Name} (#{scannerIDStr}) lost when attempting scan at {saveDateTime}");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _log.LogInformation(ex, $"Scanning operation cancelled at {saveDateTime}");
                return imagePaths;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Failed to scan image on scanner #{scannerIDStr} at {saveDateTime}");
                throw;
            }

            return imagePaths;
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

                    if (_staQueue != null)
                    {
                        _staQueue.Dispose();
                    }
                    if (_cancelScanTokenSource != null)
                    {
                        _cancelScanTokenSource.Cancel();
                        _cancelScanTokenSource.Dispose();
                    }
                    if (_scanTimer != null)
                    {
                        _scanTimer.Dispose();
                    }
                    if (_uiTimer != null)
                    {
                        _uiTimer.Dispose();
                    }
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
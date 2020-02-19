using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mosey.Models;
using Mosey.Services;

namespace Mosey.ViewModels
{
    public class MainViewModel : ViewModelBase, IViewModelParent<IViewModel>, IDisposable
    {
        private ILogger<MainViewModel> _log;
        private IConfiguration _appSettings;
        private IIntervalTimer _uiTimer;
        private IIntervalTimer _scanTimer;
        private IImagingDevices<IImagingDevice> _scannerDevices;
        private IFolderBrowserDialog _folderBrowserDialog;
        private IViewModel _settingsViewModel;

        private readonly object _scannerDevicesLock = new object();
        private IImagingDeviceSettings _imageConfig;
        private ImageFileConfig _imageFileConfig;
        private IIntervalTimerConfig _scanTimerConfig;
        private IIntervalTimerConfig _uiTimerConfig;
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
                RaisePropertyChanged("ImageFormat");
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
                RaisePropertyChanged("ImageSavePath");
            }
        }

        public int ScanDelay
        {
            get
            {
                return _scanTimerConfig.Delay;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanTimerConfig.Delay = value;
                RaisePropertyChanged("ScanDelay");
            }
        }

        public int ScanInterval
        {
            get
            {
                return _scanTimerConfig.Interval;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                _scanTimerConfig.Interval = value;
                RaisePropertyChanged("ScanInterval");
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
                RaisePropertyChanged("ScanRepetitions");
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
                RaisePropertyChanged("ScannerDevices");
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
            IConfiguration appSettings,
            IIntervalTimer intervalTimer,
            IImagingDevices<IImagingDevice> imagingDevices,
            IFolderBrowserDialog folderBrowserDialog,
            IViewModel settingsViewModel
        )
        {
            _log = logger;
            _appSettings = appSettings;
            _scanTimer = intervalTimer;
            _uiTimer = (IIntervalTimer)intervalTimer.Clone();
            _scannerDevices = imagingDevices;
            _folderBrowserDialog = folderBrowserDialog;
            _settingsViewModel = settingsViewModel;

            // Lock scanners collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_scannerDevices, _scannerDevicesLock);

            // Load configuration options from JSON file
            SetConfiguration();

            _scanTimer.Tick += ScanTimer_Tick;
            _scanTimer.Complete += ScanTimer_Complete;
            _uiTimer.Tick += UITimer_Tick;
            _uiTimer.Start(TimeSpan.FromMilliseconds(_uiTimerConfig.Delay), TimeSpan.FromMilliseconds(_uiTimerConfig.Interval), _uiTimerConfig.Repetitions);

            // Start a task loop to update the scanners collection
            RefreshDevicesAsync();
        }

        public override IViewModel Create()
        {
            return new MainViewModel(
                logger: _log,
                appSettings: _appSettings,
                intervalTimer: _scanTimer,
                imagingDevices: _scannerDevices,
                folderBrowserDialog: _folderBrowserDialog,
                //settingsViewModelFactory: _settingsViewModelFactory
                settingsViewModel: _settingsViewModel
                );
        }

        private void SetConfiguration()
        {
            // TODO: Load device image config in ViewModel
            //_imageConfig = Common.Configuration.GetSection("Image").Get<ImagingDeviceSettings>();
            _scanTimerConfig = _appSettings.GetSection("Timers:Scan").Get<IntervalTimerConfig>();
            _uiTimerConfig = _appSettings.GetSection("Timers:UI").Get<IntervalTimerConfig>();
            _imageFileConfig = _appSettings.GetSection("Image:File").Get<ImageFileConfig>();

            _folderBrowserDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString();
            _folderBrowserDialog.SelectedPath = _folderBrowserDialog.InitialDirectory;

            ScannerDevices.EnableAll();
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

            _scanTimer.Start(TimeSpan.FromMinutes(ScanDelay), TimeSpan.FromMinutes(ScanInterval), ScanRepetitions);

            RaisePropertyChanged("IsScanRunning");
            RaisePropertyChanged("ScanFinishTime");
            RaisePropertyChanged("StartStopScanCommand");
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
            RaisePropertyChanged("ScanNextTime");
            RaisePropertyChanged("ScanRepetitionsCount");
        }

        private void ScanTimer_Complete(object sender, EventArgs e)
        {
            RaisePropertyChanged("ScanRepetitionsCount");
            RaisePropertyChanged("IsScanRunning");
            RaisePropertyChanged("ScanFinishTime");
            RaisePropertyChanged("StartStopScanCommand");
            _log.LogInformation($"Scan timer complete at {DateTime.Now.ToString(string.Join("_", _imageFileConfig.DateFormat, _imageFileConfig.TimeFormat))} with {ScanRepetitionsCount} repetitions.");
        }

        private void UITimer_Tick(object sender, EventArgs e)
        {
            RaisePropertyChanged("ScanNextTime");
        }

        private void RefreshDevices()
        {
            ScannerDevices.RefreshDevices(_imageConfig, true);
            RaisePropertyChanged("ScannerDevices");
            RaisePropertyChanged("StartScanCommand");
            RaisePropertyChanged("StartStopScanCommand");
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
                    // Use a dedicated thread for refresh tasks
                    // The apartment state MUST be single threaded for COM interop
                    await Task.Factory.StartNew(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(intervalSeconds)).Wait();
                        ScannerDevices.RefreshDevices();
                    }, cancellationToken, TaskCreationOptions.None, _staQueue);
                }
                finally
                {
                    RaisePropertyChanged("ScannerDevices");
                    RaisePropertyChanged("StartScanCommand");
                    RaisePropertyChanged("StartStopScanCommand");
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
            string saveDirectory = _imageFileConfig.Path;
            List<string> imagePaths = new List<string>();

            //Default to user's Pictures directory if none is specified
            if (string.IsNullOrWhiteSpace(saveDirectory))
            {
                saveDirectory = Path.Combine
                (
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString(),
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name
                );
            }

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
            if (ImageSavePath != Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString())
            {
                // Go up one directory level, otherwise the dialog starts inside the selected directory
                _folderBrowserDialog.InitialDirectory = Directory.GetParent(ImageSavePath).FullName;
            }
            _folderBrowserDialog.ShowDialog();

            RaisePropertyChanged("ImageSavePath");
        }

        public class ImageFileConfig
        {
            public string Path { get; set; }
            public string Prefix { get; set; }
            public string Format { get; set; }
            public List<string> SupportedFormats { get; set; }
            public string DateFormat { get; set; }
            public string TimeFormat { get; set; }
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
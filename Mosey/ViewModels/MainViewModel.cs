using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mosey.Models;

namespace Mosey.ViewModels
{
    // TODO: Implement IDisposable if needed
    public class MainViewModel : ViewModelBase
    {
        private ILogger<MainViewModel> _log;
        private IIntervalTimer _uiTimer;
        private IIntervalTimer _scanTimer;
        private IExternalInstance _scanAnalysis;
        private IImagingDevices<IImagingDevice> _scannerDevices;
        private object _scannerDevicesLock = new object();
        private ImageFileConfig _imageConfig;
        private IntervalTimerConfig _scanTimerConfig;
        private IntervalTimerConfig _uiTimerConfig;

        #region Properties
        public string ImageFormat
        {
            get
            {
                return _imageConfig.Format;
            }
            set
            {
                _imageConfig.Format = value;
                RaisePropertyChanged("ImageFormat");
            }
        }

        public List<string> ImageFormatSupported
        {
            get
            {
                return _imageConfig.SupportedFormats;
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
                if(value < 1)
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
                if(_scanTimer != null)
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
        #endregion Properties

        public MainViewModel(ILogger<MainViewModel> logger, IIntervalTimer intervalTimer, IImagingDevices<IImagingDevice> imagingDevices)
        {
            _log = logger;
            _scanTimer = intervalTimer;
            _uiTimer = (IIntervalTimer)intervalTimer.Clone();
            _scannerDevices = imagingDevices;

            // Lock collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_scannerDevices, _scannerDevicesLock);
            SetConfiguration();

            _scanTimer.Tick += ScanTimer_Tick;
            _scanTimer.Complete += ScanTimer_Complete;
            _uiTimer.Tick += UITimer_Tick;
            _uiTimer.Start(TimeSpan.FromSeconds(_uiTimerConfig.Delay), TimeSpan.FromSeconds(_uiTimerConfig.Interval), _uiTimerConfig.Repetitions);
        }

        private void SetConfiguration()
        {
            _scanTimerConfig = Common.Configuration.GetSection("Timers:Scan").Get<IntervalTimerConfig>();
            _uiTimerConfig = Common.Configuration.GetSection("Timers:UI").Get<IntervalTimerConfig>();
            _imageConfig = Common.Configuration.GetSection("Image:File").Get<ImageFileConfig>();

            ScannerDevices.EnableAll();
        }

        #region Commands
        private ICommand _EnableScannersCommand;
        public ICommand EnableScannersCommand
        {
            get
            {
                if (_EnableScannersCommand == null)
                    _EnableScannersCommand = new RelayCommand(o => _scannerDevices.EnableAll(), o => !ScannerDevices.IsEmpty && !IsScanRunning);

                return _EnableScannersCommand;
            }
        }

        private ICommand _ManualScanCommand;
        public ICommand ManualScanCommand
        {
            get
            {
                if (_ManualScanCommand == null)
                    _ManualScanCommand = new RelayCommand(o => ScanAsync(), o => !ScannerDevices.IsEmpty && !ScannerDevices.IsScanInProgress && !IsScanRunning);

                return _ManualScanCommand;
            }
        }

        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                if (_StartScanCommand == null)
                    _StartScanCommand = new RelayCommand(o => StartScan(), o => !ScannerDevices.IsEmpty && !ScannerDevices.IsScanInProgress && !IsScanRunning && !_scanTimer.Paused);

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
                    _StopScanCommand = new RelayCommand(o => _scanTimer.Stop(), o => IsScanRunning);

                return _StopScanCommand;
            }
        }

        private ICommand _RefreshScannersCommand;
        public ICommand RefreshScannersCommand
        {
            get
            {
                if (_RefreshScannersCommand == null)
                    _RefreshScannersCommand = new RelayCommand(o => RefreshDevicesAsync(), o => !ScannerDevices.IsScanInProgress && !IsScanRunning);

                return _RefreshScannersCommand;
            }
        }
        #endregion Commands

        public void StartScan()
        {
            //scanLagTimer.Start(TimeSpan.FromMinutes(ScanDelay), TimeSpan.FromMinutes(ScanInterval), ScanRepetitions);
            //Use seconds instead of minutes for testing functionality
            _scanTimer.Start(TimeSpan.FromSeconds(ScanDelay), TimeSpan.FromSeconds(ScanInterval), ScanRepetitions);
            RaisePropertyChanged("IsScanRunning");
            RaisePropertyChanged("ScanFinishTime");
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            // Update progress
            RaisePropertyChanged("ScanRepetitionsCount");

            ScanAsync();
        }

        private void ScanTimer_Complete(object sender, EventArgs e)
        {
            RaisePropertyChanged("ScanRepetitionsCount");
            RaisePropertyChanged("IsScanRunning");
            RaisePropertyChanged("ScanFinishTime");
            _log.LogInformation($"Scan timer complete at {DateTime.Now.ToString(string.Join("_", _imageConfig.DateFormat, _imageConfig.TimeFormat))} with {ScanRepetitionsCount} repetitions.");

            // Run ScanLagAnalysis if required
            //ScanLagAnalysis();
        }

        private void UITimer_Tick(object sender, EventArgs e)
        {
            // Update devices
            if (!ScannerDevices.IsScanInProgress)
            {
                RefreshDevicesAsync();
            }

            RaisePropertyChanged("ScanNextTime");
        }

        private async void RefreshDevicesAsync()
        {
            // Start scanners on a separate thread
            // COM objects remain bound to the thread that they were started on
            // This prevents locking the UI and ensures multiple scanners can run together
            await Task.Run(() => ScannerDevices.RefreshDevices());
            RaisePropertyChanged("ScannerDevices");
        }

        private async void ScanAsync()
        {
            // Run scanning on another thread
            List<string> result = await Task.Run(() => Scan());
        }

        public List<string> Scan()
        {
            string scannerIDStr = string.Empty;
            string saveDateTime = DateTime.Now.ToString(string.Join("_", _imageConfig.DateFormat, _imageConfig.TimeFormat));
            string saveDirectory = _imageConfig.Path;
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
                // TODO: Order devices by ID before scanning
                // Provides clearer feedback to users than unordered scanning
                foreach(IImagingDevice scanner in ScannerDevices)
                {
                    if (scanner.IsConnected && scanner.IsEnabled)
                    {
                        scannerIDStr = scanner.ID.ToString();

                        // Run the scanner and retrieve the image(s) to memory
                        scanner.GetImage();
                        if(scanner.ErrorState != null)
                        {
                            _log.LogError(scanner.ErrorState, $"Unable to retrieve image on {scanner.Name} (#{scannerIDStr}) at {saveDateTime}");
                            continue;
                        }
                        _log.LogDebug($"Retrieved image on {scanner.Name} (#{scannerIDStr}) at {saveDateTime}");

                        string fileName = String.Join("_", _imageConfig.Prefix, saveDateTime);
                        string directory = Path.Combine(saveDirectory, String.Join(String.Empty, "Scanner", scannerIDStr));
                        Directory.CreateDirectory(directory);

                        // Write image(s) to filesystem and retrieve a list of saved file names
                        IEnumerable<string> savedImages = scanner.SaveImage(fileName, directory: directory, fileFormat: ImageFormat);
                        foreach (string image in savedImages)
                        {
                            imagePaths.Add(image);
                            _log.LogInformation($"Saved image file {image} from scanner #{scannerIDStr} at {saveDateTime}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Failed to scan image on scanner #{scannerIDStr} at {saveDateTime}");
                throw;
            }

            return imagePaths;
        }

        public void ScanAnalysis()
        {
            // Create a new ScanLag object using IronPython
            //IExternalInstance scanLag = new ExternalInstance(Python.CreateEngine(), "", "ScanLag");
            //ScanLag.ExecuteMethod("ScanLag", param1, param2);
            throw new NotImplementedException();
        }

        public class IntervalTimerConfig
        {
            public int Delay { get; set; }
            public int Interval { get; set; }
            public int Repetitions { get; set; }
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
    }
}

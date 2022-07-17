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
using Mosey.Gui.Models;
using Mosey.Gui.ViewModels.Extensions;
using Mosey.Core;
using Mosey.Core.Imaging;
using Mosey.Application.Configuration;
using Mosey.Application;

namespace Mosey.Gui.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IViewModelParent<IViewModel>, IClosing, IDisposable
    {
        private readonly IIntervalScanningService _scanningService;
        private readonly IViewModel _settingsViewModel;
        private readonly Services.UIServices _uiServices;
        private readonly IOptionsMonitor<AppSettings> _appSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<MainViewModel> _log;

        private readonly DialogViewModel _dialog;
        private readonly object _scanningDevicesLock = new();

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
                _imageSavePath ??= _appSettings.CurrentValue.ImageFile.Directory;

                return _imageSavePath;
            }
            set
            {
                _imageSavePath = value;
                RaisePropertyChanged(nameof(ImageSavePath));
            }
        }

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
                _scanDelay = _scanDelay > TimeSpan.Zero ? _scanDelay : _appSettings.CurrentValue.ScanTimer.Delay;

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
                _scanInterval = _scanInterval > TimeSpan.Zero ? _scanInterval : _appSettings.CurrentValue.ScanTimer.Interval;

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
                    : _appSettings.CurrentValue.ScanTimer.Repetitions;

                return _scanRepetitions;
            }
            set
            {
                _scanRepetitions = value < 1 ? 1 : value;
                RaisePropertyChanged(nameof(ScanRepetitions));
            }
        }

        public bool IsScanRunning
            => _scanningService.IsScanRunning;

        public int ScanRepetitionsCount { get; private set; }

        public TimeSpan ScanNextTime
        {
            get
            {
                if (IsScanRunning && ScanRepetitionsCount != 0 && ScanRepetitionsCount < ScanRepetitions)
                {
                    var scanNext = _scanningService.StartTime.Add(ScanRepetitionsCount * _scanInterval);
                    return scanNext.Subtract(DateTime.Now);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
        }

        public DateTime ScanFinishTime
            => IsScanRunning
                ? _scanningService.FinishTime
                : DateTime.MinValue;

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
            IIntervalScanningService scanningService,
            Services.UIServices uiServices,
            IViewModel settingsViewModel,
            IOptionsMonitor<AppSettings> appSettings,
            IFileSystem fileSystem,
            ILogger<MainViewModel> logger)
        {
            _scanningService = scanningService;
            _uiServices = uiServices;
            _settingsViewModel = settingsViewModel;
            _appSettings = appSettings;
            _fileSystem = fileSystem;
            _log = logger;

            _dialog = new DialogViewModel(this, _uiServices, _log);

            Initialize();
        }

        public void Dispose()
        {
            _scanningService.DevicesRefreshed -= ScanningDevices_Refreshed;

            _cancelScanTokenSource?.Cancel();
            _cancelScanTokenSource?.Dispose();
        }

        public override IViewModel Create()
            => new MainViewModel(
                scanningService: _scanningService,
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

        private ICommand _StartScanCommand;
        public ICommand StartScanCommand
        {
            get
            {
                _StartScanCommand ??= new AsyncCommand(
                    () => StartScanningWithDialog(),
                    _ => !ScanningDevices.IsEmpty && !ScanningDevices.IsImagingInProgress && !IsScanRunning);

                return _StartScanCommand;
            }
        }

        public ICommand StartStopScanCommand
            => !IsScanRunning ? StartScanCommand : StopScanCommand;

        private IAsyncCommand _StopScanCommand;
        public IAsyncCommand StopScanCommand
        {
            get
            {
                _StopScanCommand ??= new AsyncCommand(
                    () => StopScanningWithDialog(),
                    _ => IsScanRunning && !_cancelScanTokenSource.IsCancellationRequested);

                return _StopScanCommand;
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
                    _log.LogTrace($"Waiting for scanning operations to complete before shutting down from {nameof(OnClosingAsync)}.");
                    IsWaiting = true;
                    StopScanning();
                }
            }

            _log.LogDebug($"Application shutdown requested from {nameof(OnClosingAsync)}.");
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Begin scanning at set intervals
        /// </summary>
        public async Task StartScanning()
        {
            _cancelScanTokenSource = new CancellationTokenSource();
            var progress = new Progress<ScanningProgress>(async (report) =>
            {
                if (report.Result == ScanningProgress.ProgressResult.Success)
                {
                    ScanRepetitionsCount = report.RepetitionCount;
                    RaisePropertyChanged(nameof(ScanRepetitionsCount));

                    if (report.RepetitionCount >= ScanRepetitions)
                    {
                        // Apply any changes to settings that were made during scanning
                        UpdateConfig(_appSettings.Get(AppSettings.UserSettingsKey));

                        // Update properties, scanning finished
                        RaisePropertyChanged(nameof(IsScanRunning));
                        RaisePropertyChanged(nameof(ScanFinishTime));
                        RaisePropertyChanged(nameof(StartStopScanCommand));
                    }
                }
                else
                {
                    // An unhandled error occurred
                    _log.LogError(report.Exception, "{message}", report.Exception.Message);
                    await _dialog.ExceptionDialog(report.Exception);
                    StopScanning(false);
                }
            });

            await _scanningService
                .StartScanning(new IntervalTimerConfig(_scanDelay, _scanInterval, ScanRepetitions), progress)
                .ConfigureAwait(false);

            // Finsihed scanning
            ScanRepetitionsCount = 0;
        }

        /// <summary>
        /// Begin scanning at intervals, after first checking if checking interval time and free disk space are sufficient.
        /// </summary>
        public async Task StartScanningWithDialog()
        {
            // Check that interval time is sufficient for selected resolution
            var imagingTime = _scanningService.GetRequiredScanningTime();
            if (imagingTime * 1.5 > TimeSpan.FromMinutes(ScanInterval) && !await _dialog.ImagingTimeDialog(TimeSpan.FromMinutes(ScanInterval), imagingTime))
            {
                _log.LogDebug($"Scanning not started due to low interval time: {imagingTime.TotalMinutes} minutes required, {ScanInterval} minutes selected.");
                return;
            }

            // Check that disk space is sufficient for selected resolution
            try
            {
                var availableDiskSpace = FileSystemExtensions.AvailableFreeSpace(
                    _fileSystem.Path.GetPathRoot(_appSettings.CurrentValue.ImageFile.Directory),
                    _fileSystem);
                var imagesRequiredDiskSpace = _scanningService.GetRequiredDiskSpace(ScanRepetitions);
                if (imagesRequiredDiskSpace * 1.5 > availableDiskSpace && !await _dialog.DiskSpaceDialog(imagesRequiredDiskSpace, availableDiskSpace))
                {
                    _log.LogDebug($"Scanning not started due to low disk space: {StringFormat.ByteSize(imagesRequiredDiskSpace)} required, {StringFormat.ByteSize(availableDiskSpace)} available.");
                    return;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.LogWarning(ex, $"Unable to show {nameof(DialogViewModel.DiskSpaceDialog)} on path {_appSettings.CurrentValue.ImageFile.Directory} due to {ex.GetType()}");
            }

            await StartScanning().ConfigureAwait(false);
        }

        /// <summary>
        /// Stop interval scanning
        /// </summary>
        /// <param name="waitForCompletion">Allow any current scanning operation to run to completion</param>
        public void StopScanning(bool waitForCompletion = true)
        {
            _log.LogInformation("Scanning stopped with {ScanRepetitionsCount} of {ScanRepetitions} repetitions completed.", ScanRepetitionsCount, ScanRepetitions);
            _scanningService.StopScanning(waitForCompletion);
        }

        /// <summary>
        /// Stops scanning based on user input
        /// </summary>
        public async Task StopScanningWithDialog()
        {
            if (await _dialog.StopScanDialog(cancellationToken: _cancelScanTokenSource.Token))
            {
                StopScanning();
            }
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

            var appSettings = _appSettings.CurrentValue;

            // Copy settings as new instances
            appSettings.ScanTimer = settings.ScanTimer with { };
            appSettings.Image = settings.Image with { };
            appSettings.ImageFile = settings.ImageFile with { };
            appSettings.Device = settings.Device with { };

            _scanningService.UpdateConfig(new IIntervalScanningService.ScanningConfig(appSettings.Device, appSettings.Image, appSettings.ImageFile));

            RaisePropertyChanged(nameof(ScanInterval));
            RaisePropertyChanged(nameof(ScanRepetitions));
            RaisePropertyChanged(nameof(ImageSavePath));

            _log.LogTrace($"Configuration updated with {nameof(UpdateConfig)}.");
        }

        private void ScanningDevices_Refreshed(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(ScanningDevices));
        }

        /// <summary>
        /// Update user interface components
        /// </summary>
        private async Task BeginRefreshUI(TimeSpan interval)
        {
            interval = new[] { interval, TimeSpan.FromMilliseconds(100) }.Max();

            while (!System.Windows.Application.Current?.Dispatcher?.HasShutdownStarted ?? true)
            {
                await Task.Delay(interval);
                RaisePropertyChanged(nameof(IsScanRunning));
                RaisePropertyChanged(nameof(ScanNextTime));
                RaisePropertyChanged(nameof(ScanFinishTime));
                RaisePropertyChanged(nameof(ScanRepetitionsCount));
                RaisePropertyChanged(nameof(StartStopScanCommand));
            }
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

        private void Initialize()
        {
            _log.LogTrace($"ViewModel initialization starting.");

            ScanningDevices = _scanningService.Scanners;

            // Load saved configuration values
            var userSettings = _appSettings.Get(AppSettings.UserSettingsKey);
            UpdateConfig(userSettings);

            // Lock scanners collection across threads to prevent conflicts
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(ScanningDevices.Devices, _scanningDevicesLock);

            // Register event callbacks
            _appSettings.OnChange(UpdateConfig);
            _scanningService.DevicesRefreshed += ScanningDevices_Refreshed;

            // Start a task loop to update UI
            _ = BeginRefreshUI(userSettings.UITimer.Interval);

            ScanningDevices.EnableAll();

            _log.LogTrace($"ViewModel initialization complete.");
        }
    }
}
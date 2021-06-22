using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Mosey.GUI.Configuration;
using Mosey.GUI.Models;
using Mosey.GUI.Services;

namespace Mosey.GUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ILogger<SettingsViewModel> _log;
        private readonly IWritableOptions<AppSettings> _appSettings;
        private AppSettings _userSettings;
        private readonly UIServices _uiServices;
        private readonly DialogViewModel _dialog;
        private readonly IFileSystem _fileSystem;

        #region Properties
        public string ImageSavePath
        {
            get => _userSettings.ImageFile.Directory ?? _appSettings.Value.ImageFile.Directory;
            set
            {
                _userSettings.ImageFile.Directory = value;
                _appSettings.Update(c => c.ImageFile.Directory = value);
                RaisePropertyChanged(nameof(ImageSavePath));

                _log.LogInformation($"{nameof(ImageSavePath)} changed to {value}");
            }
        }

        public int DefaultResolution
        {
            get => _userSettings.Image.Resolution;
            set
            {
                _userSettings.Image.Resolution = value;
                _appSettings.Update(c => c.Image.Resolution = value);
                RaisePropertyChanged(nameof(DefaultResolution));

                _log.LogInformation($"{nameof(DefaultResolution)} changed to {value}");
            }
        }

        public IEnumerable<int> StandardResolutions => _userSettings.Device.StandardResolutions;

        public bool ScannersEnableOnConnect
        {
            get => _userSettings.Device.EnableWhenConnected;
            set
            {
                _userSettings.Device.EnableWhenConnected = value;
                _appSettings.Update(c => c.Device.EnableWhenConnected = value);
                RaisePropertyChanged(nameof(ScannersEnableOnConnect));

                _log.LogInformation($"{nameof(ScannersEnableOnConnect)} changed to {value}");
            }
        }

        public bool ScannersEnableWhenScanning
        {
            get => _userSettings.Device.EnableWhenScanning;
            set
            {
                _userSettings.Device.EnableWhenScanning = value;
                _appSettings.Update(c => c.Device.EnableWhenScanning = value);
                RaisePropertyChanged(nameof(ScannersEnableWhenScanning));

                _log.LogInformation($"{nameof(ScannersEnableWhenScanning)} changed to {value}");
            }
        }

        public bool ScanHighestResolution
        {
            get => _userSettings.Device.UseHighestResolution;
            set
            {
                _userSettings.Device.UseHighestResolution = value;
                _appSettings.Update(c => c.Device.UseHighestResolution = value);
                RaisePropertyChanged(nameof(ScanHighestResolution));

                _log.LogInformation($"{nameof(ScanHighestResolution)} changed to {value}");
            }
        }

        public int ScanInterval
        {
            get => (int)_userSettings.ScanTimer.Interval.TotalMinutes;
            set
            {
                _userSettings.ScanTimer.Interval = TimeSpan.FromMinutes(value);
                _appSettings.Update(c => c.ScanTimer.Interval = _userSettings.ScanTimer.Interval);
                RaisePropertyChanged(nameof(ScanInterval));

                _log.LogInformation($"{nameof(ScanInterval)} changed to {value}");
            }
        }

        public int ScanRepetitions
        {
            get => _userSettings.ScanTimer.Repetitions;
            set
            {
                _userSettings.ScanTimer.Repetitions = value;
                _appSettings.Update(c => c.ScanTimer.Repetitions = value);
                RaisePropertyChanged(nameof(ScanRepetitions));

                _log.LogInformation($"{nameof(ScanRepetitions)} changed to {value}");
            }
        }

        public bool ScanningDelay
        {
            get => _userSettings.ScanTimer.Delay != TimeSpan.Zero;
            set
            {
                if (value)
                {
                    _userSettings.ScanTimer.Delay = _userSettings.ScanTimer.Interval;
                }
                else
                {
                    _userSettings.ScanTimer.Delay = TimeSpan.Zero;
                }

                _appSettings.Update(c => c.ScanTimer.Delay = _userSettings.ScanTimer.Delay);
                RaisePropertyChanged(nameof(ScanningDelay));

                _log.LogInformation($"{nameof(ScanningDelay)} changed to {value}");
            }
        }

        public static string Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        #endregion Properties

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            UIServices uiServices,
            IWritableOptions<AppSettings> appSettings,
            IFileSystem fileSystem)
        {
            _log = logger;
            _uiServices = uiServices;
            _appSettings = appSettings;
            _userSettings = appSettings.Get("UserSettings");
            _dialog = new DialogViewModel(this, _uiServices, _log);
            _fileSystem = fileSystem;
        }

        public override IViewModel Create()
        {
            return new SettingsViewModel(
                logger: _log,
                uiServices: _uiServices,
                appSettings: _appSettings,
                fileSystem: _fileSystem);
        }

        #region Commands
        private ICommand _SelectFolderCommand;
        public ICommand SelectFolderCommand
        {
            get
            {
                if (_SelectFolderCommand is null)
                {
                    _SelectFolderCommand = new RelayCommand(o => ImageDirectoryDialog());
                }

                return _SelectFolderCommand;
            }
        }

        private ICommand _ResetOptionsCommand;
        public ICommand ResetOptionsCommand
        {
            get
            {
                if (_ResetOptionsCommand is null)
                {
                    _ResetOptionsCommand = new RelayCommand(o => ResetUserOptions());
                }

                return _ResetOptionsCommand;
            }
        }
        #endregion Commands

        /// <summary>
        /// Return all user options to the default values
        /// </summary>
        private void ResetUserOptions()
        {
            _log.LogDebug("Overwriting user settings with defaults.");

            // Copy default settings and write to disk
            _userSettings = _appSettings.Value.Copy();
            _appSettings.Update(c =>
            {
                c.ScanTimer = _userSettings.ScanTimer;
                c.ImageFile = _userSettings.ImageFile;
                c.Image = _userSettings.Image;
                c.Device = _userSettings.Device;
            });

            RaisePropertyChanged(nameof(ImageSavePath));
            RaisePropertyChanged(nameof(ScanningDelay));
            RaisePropertyChanged(nameof(ScanInterval));
            RaisePropertyChanged(nameof(ScanRepetitions));
            RaisePropertyChanged(nameof(ScannersEnableOnConnect));
            RaisePropertyChanged(nameof(ScannersEnableWhenScanning));
            RaisePropertyChanged(nameof(ScanHighestResolution));

            _log.LogInformation("User settings reset to default.");
        }

        /// <summary>
        /// Display a dialog window that allows the user to select the default image save location.
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
                "Choose the default image file save location"
                );

            // Only update the property if a path was actually returned
            if (!string.IsNullOrWhiteSpace(selectedDirectory))
            {
                ImageSavePath = selectedDirectory;
            }
        }
    }
}
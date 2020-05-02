using System;
using System.IO;
using System.Windows.Input;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mosey.Configuration;
using Mosey.Models;

namespace Mosey.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private ILogger<SettingsViewModel> _log;
        private IFolderBrowserDialog _folderBrowserDialog;
        private IWritableOptions<AppSettings> _appSettings;
        private AppSettings _userSettings;

        #region Properties
        public string ImageSavePath
        {
            get
            {
                return _userSettings.ImageFile.Directory ?? _appSettings.Value.ImageFile.Directory;
            }
            set
            {
                _userSettings.ImageFile.Directory = value;
                _appSettings.Update(c => c.ImageFile.Directory = value);
                RaisePropertyChanged(nameof(ImageSavePath));
            }
        }

        public int DefaultResolution
        {
            get
            {
                return _userSettings.Image.Resolution;
            }
            set
            {
                _userSettings.Image.Resolution = value;
                _appSettings.Update(c => c.Image.Resolution = value);
                RaisePropertyChanged(nameof(DefaultResolution));
            }
        }

        public IEnumerable<int> StandardResolutions { get { return _userSettings.Device.StandardResolutions; } }

        public bool ScannersEnableOnConnect
        {
            get
            {
                return _userSettings.Device.EnableWhenConnected;
            }
            set
            {
                _userSettings.Device.EnableWhenConnected = value;
                _appSettings.Update(c => c.Device.EnableWhenConnected = value);
                RaisePropertyChanged(nameof(ScannersEnableOnConnect));
            }
        }

        public bool ScannersEnableWhenScanning
        {
            get
            {
                return _userSettings.Device.EnableWhenScanning;
            }
            set
            {
                _userSettings.Device.EnableWhenScanning = value;
                _appSettings.Update(c => c.Device.EnableWhenScanning = value);
                RaisePropertyChanged(nameof(ScannersEnableWhenScanning));
            }
        }

        public bool ScanHighestResolution
        {
            get
            {
                return _userSettings.Device.UseHighestResolution;
            }
            set
            {
                _userSettings.Device.UseHighestResolution = value;
                _appSettings.Update(c => c.Device.UseHighestResolution = value);
                RaisePropertyChanged(nameof(ScanHighestResolution));
            }
        }

        public int ScanInterval
        {
            get
            {
                return (int)_userSettings.ScanTimer.Interval.TotalMinutes;
            }
            set
            {
                _userSettings.ScanTimer.Interval = TimeSpan.FromMinutes(value);
                _appSettings.Update(c => c.ScanTimer.Interval = _userSettings.ScanTimer.Interval);
                RaisePropertyChanged(nameof(ScanInterval));
            }
        }

        public int ScanRepetitions
        {
            get
            {
                return _userSettings.ScanTimer.Repetitions;
            }
            set
            {
                _userSettings.ScanTimer.Repetitions = value;
                _appSettings.Update(c => c.ScanTimer.Repetitions = value);
                RaisePropertyChanged(nameof(ScanRepetitions));
            }
        }

        public bool ScanningDelay
        {
            get
            {
                return _userSettings.ScanTimer.Delay != TimeSpan.Zero;
            }
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
            }
        }
        #endregion Properties

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            IFolderBrowserDialog folderBrowserDialog,
            IWritableOptions<AppSettings> appSettings
            )
        {
            _log = logger;
            _folderBrowserDialog = folderBrowserDialog;

            _appSettings = appSettings;
            _userSettings = appSettings.Get("UserSettings");
        }

        public override IViewModel Create()
        {
            return new SettingsViewModel(
                logger: _log,
                folderBrowserDialog: _folderBrowserDialog,
                appSettings: _appSettings
                );
        }

        #region Commands
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

        private ICommand _ResetOptionsCommand;
        public ICommand ResetOptionsCommand
        {
            get
            {
                if (_ResetOptionsCommand == null)
                    _ResetOptionsCommand = new RelayCommand(o => ResetUserOptions());

                return _ResetOptionsCommand;
            }
        }
        #endregion Commands

        /// <summary>
        /// Return all user options to the default values
        /// </summary>
        private void ResetUserOptions()
        {
            // Copy default settings and write to disk
            _userSettings = _appSettings.Value.Copy();
            _appSettings.Update(c => {
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
        }

        /// <summary>
        /// Display a dialog window that allows the user to select a default image save location
        /// </summary>
        private void OpenFolderDialog()
        {
            _folderBrowserDialog.Title = "Choose the default image file save location";
            // Go up one directory level, otherwise the dialog starts inside the selected directory
            _folderBrowserDialog.InitialDirectory = Directory.GetParent(ImageSavePath).FullName;
            // Ensure SelectedPath reflects current value
            _folderBrowserDialog.SelectedPath = ImageSavePath;

            _folderBrowserDialog.ShowDialog();
            ImageSavePath = _folderBrowserDialog.SelectedPath;
        }
    }
}
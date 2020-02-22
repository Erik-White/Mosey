using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mosey.Configuration;
using Mosey.Models;

namespace Mosey.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private ILogger<SettingsViewModel> _log;
        private IFolderBrowserDialog _folderBrowserDialog;
        private IOptionsSnapshot<AppSettings> _appSettings;
        private AppSettings _userSettings;

        #region Properties
        public string ImageSavePath
        {
            get
            {
                //return _folderBrowserDialog.SelectedPath ?? _folderBrowserDialog.InitialDirectory;
                return _userSettings.ImageFile.Directory ?? _appSettings.Value.ImageFile.Directory;
            }
            set
            {
                //_folderBrowserDialog.SelectedPath = value;
                _userSettings.ImageFile.Directory = value;
                RaisePropertyChanged(nameof(ImageSavePath));
            }
        }

        public bool ScannersEnableOnConnect
        {
            get
            {
                return _userSettings.Device.EnableWhenConnected;
            }
            set
            {
                _userSettings.Device.EnableWhenConnected = value;
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
                RaisePropertyChanged(nameof(ScannersEnableWhenScanning));
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
                RaisePropertyChanged(nameof(ScanningDelay));
            }
        }
        #endregion Properties

        public SettingsViewModel(
            ILogger<SettingsViewModel> logger,
            IFolderBrowserDialog folderBrowserDialog,
            IOptionsSnapshot<AppSettings> appSettings
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
            _userSettings = _appSettings.Value.Copy();

            RaisePropertyChanged(nameof(ImageSavePath));
            RaisePropertyChanged(nameof(ScanningDelay));
            RaisePropertyChanged(nameof(ScanInterval));
            RaisePropertyChanged(nameof(ScanRepetitions));
            RaisePropertyChanged(nameof(ScannersEnableOnConnect));
            RaisePropertyChanged(nameof(ScannersEnableWhenScanning));
        }

        /// <summary>
        /// Display a dialog window that allows the user to select a default image save location
        /// </summary>
        private void OpenFolderDialog()
        {
            _folderBrowserDialog.Title = "Choose the default image file save location";
            if (ImageSavePath != _appSettings.Value.ImageFile.Directory)
            {
                // Go up one directory level, otherwise the dialog starts inside the selected directory
                _folderBrowserDialog.InitialDirectory = Directory.GetParent(ImageSavePath).FullName;
            }
            _folderBrowserDialog.ShowDialog();
            ImageSavePath = _folderBrowserDialog.SelectedPath;
        }
    }
}

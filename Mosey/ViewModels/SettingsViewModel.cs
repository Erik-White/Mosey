using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mosey.Models;

namespace Mosey.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private ILogger<SettingsViewModel> _log;
        private IFolderBrowserDialog _folderBrowserDialog;

        #region Properties
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
        #endregion Properties

        public SettingsViewModel(ILogger<SettingsViewModel> logger, IFolderBrowserDialog folderBrowserDialog)
        {
            _log = logger;
            _folderBrowserDialog = folderBrowserDialog;
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
        #endregion Commands

        /// <summary>
        /// Display a dialog window that allows the user to select a default image save location
        /// </summary>
        private void OpenFolderDialog()
        {
            _folderBrowserDialog.Title = "Choose the default image file save location";
            if (ImageSavePath != Environment.GetFolderPath(Environment.SpecialFolder.MyPictures).ToString())
            {
                // Go up one directory level, otherwise the dialog starts inside the selected directory
                _folderBrowserDialog.InitialDirectory = Directory.GetParent(ImageSavePath).FullName;
            }
            _folderBrowserDialog.ShowDialog();

            RaisePropertyChanged("ImageSavePath");
        }
    }
}

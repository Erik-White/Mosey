using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mosey.Models;

namespace Mosey.Services
{
    /// <summary>
    /// A dialog window that allows selection of a single directory
    /// </summary>
    internal class FolderBrowserDialog : IFolderBrowserDialog
    {
        private string _title;
        private string _selectedPath;
        private string _initialDirectory;

        public string Title
        {
            get { return _title ?? string.Empty; }
            set { _title = value; }
        }

        public string InitialDirectory
        {
            get
            {
                return _initialDirectory ?? Environment.GetFolderPath(RootDirectory).ToString();
            }
            set
            {
                _initialDirectory = value;
            }
        }

        public Environment.SpecialFolder RootDirectory { get; set; }

        public string SelectedPath
        {
            get
            {
                return _selectedPath ?? string.Empty;
            }
            set
            {
                _selectedPath = value;
            }
        }

        public FolderBrowserDialog()
        {
            RootDirectory = Environment.SpecialFolder.MyComputer;
        }

        public FolderBrowserDialog(Environment.SpecialFolder rootDirectory)
        {
            RootDirectory = rootDirectory;
        }

        /// <summary>
        /// Display the dialog window
        /// </summary>
        /// <returns>A <c>bool</c> that indicates whether a path was selected</returns>
        public bool? ShowDialog()
        {
            using (CommonOpenFileDialog dialog = CreateDialog(RootDirectory, InitialDirectory))
            {
                var result = dialog.ShowDialog() == CommonFileDialogResult.Ok;
                if (result) SelectedPath = dialog.FileName;

                return result;
            }
        }

        /// <summary>
        /// Initialise a new instance of a <c>CommonOpenFileDialog</c>
        /// </summary>
        /// <param name="rootDirectory">The top directory available to the dialog</param>
        /// <param name="initialDirectory">The directory location to display when opening the dialog. Must be a child of <paramref name="rootDirectory"/>rootDirectory</param>
        /// <returns>A new instance of a <c>CommonOpenFileDialog</c></returns>
        private CommonOpenFileDialog CreateDialog(Environment.SpecialFolder rootDirectory, string initialDirectory)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            dialog.IsFolderPicker = true;
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.EnsureReadOnly = false;
            dialog.EnsureValidNames = true;
            dialog.Multiselect = false;
            dialog.ShowPlacesList = true;

            dialog.DefaultDirectory = Environment.GetFolderPath(rootDirectory).ToString();
            dialog.InitialDirectory = initialDirectory;
            dialog.Title = Title;

            return dialog;
        }
    }
}

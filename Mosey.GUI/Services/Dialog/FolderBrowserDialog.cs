using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mosey.GUI.Models.Dialog;

namespace Mosey.GUI.Services.Dialog
{
    /// <summary>
    /// A dialog window that allows selection of a single directory
    /// </summary>
    internal class FolderBrowserDialog : IFolderBrowserDialog
    {
        private string _title;
        private string _selectedPath;
        private string _initialDirectory;

        /// <summary>
        /// Text displayed in the dialog window title bar
        /// </summary>
        public string Title
        {
            get => _title ?? string.Empty;
            set => _title = value;
        }

        /// <summary>
        /// Initial file browsing directory
        /// </summary>
        public string InitialDirectory
        {
            get => _initialDirectory ?? Environment.GetFolderPath(RootDirectory).ToString();
            set => _initialDirectory = value;
        }

        public Environment.SpecialFolder RootDirectory { get; set; }

        public string SelectedPath
        {
            get => _selectedPath ?? string.Empty;
            set => _selectedPath = value;
        }

        public FolderBrowserDialog()
            => RootDirectory = Environment.SpecialFolder.MyComputer;

        public FolderBrowserDialog(Environment.SpecialFolder rootDirectory)
            => RootDirectory = rootDirectory;

        /// <summary>
        /// Display the dialog window
        /// </summary>
        /// <returns>A <c>bool</c> that indicates whether a path was selected</returns>
        public bool? ShowDialog()
        {
            using (var dialog = CreateDialog(RootDirectory, InitialDirectory))
            {
                var result = dialog.ShowDialog() == CommonFileDialogResult.Ok;
                if (result)
                {
                    SelectedPath = dialog.FileName;
                }

                return result;
            }
        }

        /// <summary>
        /// Initialise a new instance of a <c>CommonOpenFileDialog</c>
        /// </summary>
        /// <param name="rootDirectory">The top directory available to the dialog</param>
        /// <param name="initialDirectory">The directory location to display when opening the dialog.
        /// Must be a child of <paramref name="rootDirectory"/>rootDirectory</param>
        /// <returns>A new instance of a <c>CommonOpenFileDialog</c></returns>
        private CommonOpenFileDialog CreateDialog(Environment.SpecialFolder rootDirectory, string initialDirectory)
            => new()
            {
                IsFolderPicker = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true,

                DefaultDirectory = Environment.GetFolderPath(rootDirectory).ToString(),
                InitialDirectory = initialDirectory,
                Title = Title
            };
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mosey.Gui.Models;
using Mosey.Gui.Models.Dialog;
using Mosey.Gui.Services;
using Mosey.Gui.Services.Dialog;
using Mosey.Gui.ViewModels.Extensions;

namespace Mosey.Gui.ViewModels
{
    /// <summary>
    /// Common ViewModel dialog.
    /// </summary>
    public class DialogViewModel : ViewModelBase
    {
        private readonly IViewModel _context;
        private readonly IDialogManager _dialogManager;
        private readonly IFolderBrowserDialog _folderBrowserDialog;
        private readonly ILogger _log;

        public DialogViewModel(IViewModel dialogContext, UIServices uiServices, ILogger logger)
        {
            _context = dialogContext;
            _dialogManager = uiServices.DialogManager;
            _folderBrowserDialog = uiServices.FolderBrowserDialog;
            _log = logger;
        }

        public override IViewModel Create()
            => new DialogViewModel(
                dialogContext: _context,
                uiServices: new UIServices(_dialogManager, _folderBrowserDialog),
                logger: _log);

        /// <summary>
        /// Confirm if the user wants to start scanning despite low interval time.
        /// </summary>
        /// <param name="intervalTime">The time available between scanning triggered by an interval timer</param>
        /// <param name="scanningTime">The time required for a all scans to complete</param>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><see langword="true"/> if the user confirms to start scanning</returns>
        public async Task<bool> ImagingTimeDialog(TimeSpan intervalTime, TimeSpan scanningTime, int timeout = 10000, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"{nameof(ImagingTimeDialog)} initiated.");

            IDialogSettings dialogSettings = new DialogSettings
            {
                AffirmativeButtonText = "Start scanning",
                NegativeButtonText = "Cancel",
                CancellationToken = cancellationToken
            };

            // Show the dialogue until user input is recieved, or scanning is otherwise stopped
            var dialogResult = await _dialogManager.ShowMessageWithTimeoutAsync(
                _context,
                "Low interval time",
                $"Scanning at the selected resolution will take approximately {scanningTime.TotalMinutes} minutes. The selected interval time, {intervalTime.TotalMinutes} minutes, may not be enough time for all scans to complete. Are you sure you want to continue?",
                DialogStyle.AffirmativeAndNegative,
                dialogSettings,
                timeout);

            if (dialogResult == DialogResult.Canceled)
            {
                _log.LogDebug($"{nameof(ImagingTimeDialog)} closed by CancellationToken before user input recieved");
            }

            _log.LogDebug($"User input return from {nameof(ImagingTimeDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }

        /// <summary>
        /// Confirm if the user wants to start scanning despite low disk space.
        /// </summary>
        /// <param name="requiredSpace">The disk space required for storing images, in bytes</param>
        /// <param name="availableSpace">The disk space available on the selected logical drive, in bytes</param>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><see langword="true"/> if the user confirms to start scanning</returns>
        public async Task<bool> DiskSpaceDialog(long requiredSpace, long availableSpace, int timeout = 10000, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"{nameof(DiskSpaceDialog)} initiated.");

            IDialogSettings dialogSettings = new DialogSettings
            {
                AffirmativeButtonText = "Start scanning",
                NegativeButtonText = "Cancel",
                CancellationToken = cancellationToken
            };

            var dialogResult = await _dialogManager.ShowMessageWithTimeoutAsync(
                _context,
                "Disk space low",
                $"The images generated from scanning will require approximately {StringFormat.ByteSize(requiredSpace)} of disk space." +
                $"Only {StringFormat.ByteSize(availableSpace)} is available, are you sure you want to continue?",
                DialogStyle.AffirmativeAndNegative,
                dialogSettings,
                timeout);

            if (dialogResult == DialogResult.Canceled)
            {
                _log.LogDebug($"{nameof(DiskSpaceDialog)} closed by CancellationToken before user input recieved");
            }

            _log.LogDebug($"User input return from {nameof(DiskSpaceDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }

        /// <summary>
        /// Confirm if the user wants to stop scanning
        /// </summary>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><see langword="true"/> if the user confirms to stop scanning</returns>
        public async Task<bool> StopScanDialog(int timeout = 5000, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"{nameof(StopScanDialog)} initiated.");

            IDialogSettings dialogSettings = new DialogSettings
            {
                AffirmativeButtonText = "Stop scanning",
                NegativeButtonText = "Continue scanning",
                CancellationToken = cancellationToken
            };

            // Show the dialogue until user input is recieved, or scanning is otherwise stopped
            var dialogResult = await _dialogManager.ShowMessageWithTimeoutAsync(
                _context,
                "Stop scanning",
                "Are you sure you want to cancel scanning?",
                DialogStyle.AffirmativeAndNegative,
                dialogSettings,
                timeout);

            if (dialogResult == DialogResult.Canceled)
            {
                _log.LogDebug($"{nameof(StopScanDialog)} closed by CancellationToken before user input recieved");
            }

            _log.LogDebug($"User input return from {nameof(StopScanDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }

        /// <summary>
        /// Notify the user than an except has occurred and the exception message.
        /// </summary>
        /// <param name="ex">An exception to relay to the user</param>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><see langword="true"/> when the user has acknowledged the message</returns>
        public async Task<bool> ExceptionDialog(Exception ex, int timeout = 5000, CancellationToken cancellationToken = default)
        {
            _log.LogDebug($"{nameof(ExceptionDialog)} initiated.");

            var errorTitle = "An error occurred";
            var errorMessage = "";

            if (ex is COMException)
            {
                errorTitle = "Scanner communication error";
                errorMessage = "A error occurred when attempting to communicate with a device";
            }

            IDialogSettings dialogSettings = new DialogSettings
            {
                AffirmativeButtonText = "OK",
                CancellationToken = cancellationToken
            };

            // Show the dialogue until user input is recieved, or scanning is otherwise stopped
            var dialogResult = await _dialogManager.ShowMessageWithTimeoutAsync(
                _context,
                errorTitle,
                $"{errorMessage}{Environment.NewLine}{ex.GetType()}{Environment.NewLine}{ex.Message}",
                DialogStyle.Affirmative,
                dialogSettings,
                timeout);

            if (dialogResult == DialogResult.Canceled)
            {
                _log.LogDebug($"{nameof(ExceptionDialog)} closed by CancellationToken before user input recieved");
            }

            _log.LogDebug($"User input return from {nameof(ExceptionDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }

        /// <summary>
        /// Display a <see cref="IFolderBrowserDialog"/> and return the selected path.
        /// </summary>
        /// <param name="initialDirectory">The directory location to start browsing</param>
        /// <param name="title">The dialog title</param>
        /// <returns>The user selected path, or an empty string if no directory was selected</returns>
        public string FolderBrowserDialog(string initialDirectory, string title = "Select folder")
        {
            _folderBrowserDialog.Title = title;
            _folderBrowserDialog.InitialDirectory = initialDirectory;
            _folderBrowserDialog.ShowDialog();

            return _folderBrowserDialog.SelectedPath;
        }
    }
}

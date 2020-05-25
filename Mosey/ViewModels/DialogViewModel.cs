using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Mosey.Models;
using Mosey.Models.Dialog;
using Mosey.Services.Dialog;

namespace Mosey.ViewModels
{
    /// <summary>
    /// Common ViewModel dialog.
    /// </summary>
    public class DialogViewModel : ViewModelBase
    {
        private readonly IViewModel _context;
        private readonly IDialogManager _dialogManager;
        private readonly ILogger _log;

        public DialogViewModel(IViewModel viewModel, IDialogManager dialogManager, ILogger logger)
        {
            _context = viewModel;
            _dialogManager = dialogManager;
            _log = logger;
        }

        public override IViewModel Create()
        {
            return new DialogViewModel(
                viewModel: _context,
                dialogManager: _dialogManager,
                logger: _log
                );
        }

        /// <summary>
        /// Triggers a dialogue to confirm if the user wants to stop scanning
        /// </summary>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><c>true</c> if the user confirms to stop scanning</returns>
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
                $"The images generated from scanning will require approximately {Format.ByteSize(requiredSpace)} of disk space. Only {Format.ByteSize(availableSpace)} is available, are you sure you want to continue?",
                DialogStyle.AffirmativeAndNegative,
                dialogSettings,
                timeout
                );

            if (dialogResult == DialogResult.Canceled) _log.LogDebug("Stop scanning dialog closed by CancellationToken before user input recieved");

            _log.LogDebug($"User input return from {nameof(DiskSpaceDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }

        /// <summary>
        /// Triggers a dialogue to confirm if the user wants to stop scanning
        /// </summary>
        /// <param name="timeout">Removes the dialogue if it is still running after this amount of milliseconds</param>
        /// <param name="cancellationToken">Removes the dialogue if it is still running</param>
        /// <returns><c>true</c> if the user confirms to stop scanning</returns>
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
                timeout
                );

            if (dialogResult == DialogResult.Canceled) _log.LogDebug("Stop scanning dialog closed by CancellationToken before user input recieved");

            _log.LogDebug($"User input return from {nameof(StopScanDialog)}: {dialogResult}");

            return dialogResult == DialogResult.Affirmative;
        }
    }
}

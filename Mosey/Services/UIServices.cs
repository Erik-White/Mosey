using System;
using System.Collections.Generic;
using System.Text;
using Mosey.Models.Dialog;

namespace Mosey.Services
{
    /// <summary>
    /// Aggregate of common UI services
    /// </summary>
    public class UIServices
    {
        public IDialogManager DialogManager { get; }
        public IFolderBrowserDialog FolderBrowserDialog { get; }

        public UIServices(
            IDialogManager dialogManager,
            IFolderBrowserDialog folderBrowserDialog
            )
        {
            DialogManager = dialogManager;
            FolderBrowserDialog = folderBrowserDialog;
        }
    }
}

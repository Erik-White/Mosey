﻿using Mosey.Gui.Models.Dialog;

namespace Mosey.Gui.Services
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
            IFolderBrowserDialog folderBrowserDialog)
        {
            DialogManager = dialogManager;
            FolderBrowserDialog = folderBrowserDialog;
        }
    }
}

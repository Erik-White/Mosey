﻿using System;
using System.Collections.Generic;
using System.Text;
using Mosey.Models.Dialog;

namespace Mosey.Services.Dialog
{
    public class DialogInstance : IDialogInstance
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public IDialogSettings DialogSettings { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="DialogInstance"/>.
        /// </summary>
        /// <param name="settings">The settings for the message dialog.</param>
        public DialogInstance(IDialogSettings settings)
        {
            DialogSettings = settings;
        }
    }
}
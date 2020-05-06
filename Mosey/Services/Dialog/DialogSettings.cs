using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Mosey.Models.Dialog;

namespace Mosey.Services.Dialog
{
    /// <summary>
    /// A class that represents the settings used by DialogInstances.
    /// </summary>
    public class DialogSettings : IDialogSettings
    {
        public DialogSettings()
        {
            OwnerCanCloseWithDialog = false;

            AffirmativeButtonText = "OK";
            NegativeButtonText = "Cancel";

            AnimateShow = AnimateHide = true;

            DefaultText = "";
            DefaultButtonFocus = DialogResult.Negative;
            CancellationToken = CancellationToken.None;
            DialogTitleFontSize = Double.NaN;
            DialogMessageFontSize = Double.NaN;
            DialogButtonFontSize = Double.NaN;
            DialogResultOnCancel = null;
            MaximumBodyHeight = Double.NaN;
        }

        /// <summary>
        /// Gets or sets wheater the owner of the dialog can be closed.
        /// </summary>
        public bool OwnerCanCloseWithDialog { get; set; }

        /// <summary>
        /// Gets or sets the text used for the Affirmative button. For example: "OK" or "Yes".
        /// </summary>
        public string AffirmativeButtonText { get; set; }

        /// <summary>
        /// Enable or disable dialog hiding animation
        /// "True" - play hiding animation.
        /// "False" - skip hiding animation.
        /// </summary>
        public bool AnimateHide { get; set; }

        /// <summary>
        /// Enable or disable dialog showing animation.
        /// "True" - play showing animation.
        /// "False" - skip showing animation.
        /// </summary>
        public bool AnimateShow { get; set; }

        /// <summary>
        /// Gets or sets a token to cancel the dialog.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets which button should be focused by default
        /// </summary>
        public DialogResult DefaultButtonFocus { get; set; }

        /// <summary>
        /// Gets or sets the default text (just the inputdialog needed)
        /// </summary>
        public string DefaultText { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog message font.
        /// </summary>
        /// <value>
        /// The size of the dialog message font.
        /// </value>
        public double DialogMessageFontSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog button font.
        /// </summary>
        /// <value>
        /// The size of the dialog button font.
        /// </value>
        public double DialogButtonFontSize { get; set; }

        /// <summary>
        /// Gets or sets the dialog result when the user cancelled the dialog with 'ESC' key
        /// </summary>
        /// <remarks>If the value is <see langword="null"/> the default behavior is determined 
        /// by the <see cref="DialogStyle"/>.
        /// <table>
        /// <tr><td><see cref="DialogStyle"/></td><td><see cref="DialogResult"/></td></tr>
        /// <tr><td><see cref="DialogStyle.Affirmative"/></td><td><see cref="DialogResult.Affirmative"/></td></tr>
        /// <tr><td>
        /// <list type="bullet">
        /// <item><see cref="DialogStyle.AffirmativeAndNegative"/></item>
        /// <item><see cref="DialogStyle.AffirmativeAndNegativeAndSingleAuxiliary"/></item>
        /// <item><see cref="DialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary"/></item>
        /// </list></td>
        /// <td><see cref="DialogResult.Negative"/></td></tr></table></remarks>
        public DialogResult? DialogResultOnCancel { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog title font.
        /// </summary>
        /// <value>
        /// The size of the dialog title font.
        /// </value>
        public double DialogTitleFontSize { get; set; }

        /// <summary>
        /// Gets or sets the text used for the first auxiliary button.
        /// </summary>
        public string FirstAuxiliaryButtonText { get; set; }

        /// <summary>
        /// Gets or sets the maximum height. (Default is unlimited height, <a href="http://msdn.microsoft.com/de-de/library/system.double.nan">Double.NaN</a>)
        /// </summary>
        public double MaximumBodyHeight { get; set; }

        /// <summary>
        /// Gets or sets the text used for the Negative button. For example: "Cancel" or "No".
        /// </summary>
        public string NegativeButtonText { get; set; }

        /// <summary>
        /// Gets or sets the text used for the second auxiliary button.
        /// </summary>
        public string SecondAuxiliaryButtonText { get; set; }
    }
}

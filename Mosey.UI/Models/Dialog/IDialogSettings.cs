using System.Threading;

namespace Mosey.UI.Models.Dialog
{
    /// <summary>
    /// Represents the settings used by <see cref="IDialogInstance"/>s.
    /// </summary>
    public interface IDialogSettings
    {

        /// <summary>
        /// Gets or sets wheater the owner of the dialog can be closed.
        /// </summary>
        bool OwnerCanCloseWithDialog { get; set; }

        /// <summary>
        /// Gets or sets the text used for the Affirmative button. For example: "OK" or "Yes".
        /// </summary>
        string AffirmativeButtonText { get; set; }

        /// <summary>
        /// Enable or disable dialog hiding animation
        /// "True" - play hiding animation.
        /// "False" - skip hiding animation.
        /// </summary>
        bool AnimateHide { get; set; }

        /// <summary>
        /// Enable or disable dialog showing animation.
        /// "True" - play showing animation.
        /// "False" - skip showing animation.
        /// </summary>
        bool AnimateShow { get; set; }

        /// <summary>
        /// Gets or sets a token to cancel the dialog.
        /// </summary>
        CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets which button should be focused by default
        /// </summary>
        DialogResult DefaultButtonFocus { get; set; }

        /// <summary>
        /// Gets or sets the default text (just the inputdialog needed)
        /// </summary>
        string DefaultText { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog message font.
        /// </summary>
        /// <value>
        /// The size of the dialog message font.
        /// </value>
        double DialogMessageFontSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog button font.
        /// </summary>
        /// <value>
        /// The size of the dialog button font.
        /// </value>
        double DialogButtonFontSize { get; set; }

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
        DialogResult? DialogResultOnCancel { get; set; }

        /// <summary>
        /// Gets or sets the size of the dialog title font.
        /// </summary>
        /// <value>
        /// The size of the dialog title font.
        /// </value>
        double DialogTitleFontSize { get; set; }

        /// <summary>
        /// Gets or sets the text used for the first auxiliary button.
        /// </summary>
        string FirstAuxiliaryButtonText { get; set; }

        /// <summary>
        /// Gets or sets the maximum height. (Default is unlimited height, <a href="http://msdn.microsoft.com/de-de/library/system.double.nan">Double.NaN</a>)
        /// </summary>
        double MaximumBodyHeight { get; set; }

        /// <summary>
        /// Gets or sets the text used for the Negative button. For example: "Cancel" or "No".
        /// </summary>
        string NegativeButtonText { get; set; }

        /// <summary>
        /// Gets or sets the text used for the second auxiliary button.
        /// </summary>
        string SecondAuxiliaryButtonText { get; set; }
    }
}

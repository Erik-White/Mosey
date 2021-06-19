using System.Threading;
using System.Threading.Tasks;
using Mosey.GUI.Models.Dialog;

namespace Mosey.GUI.Services.Dialog
{
    /// <summary>
    /// Extension methods for <see cref="IDialogManager"/>.
    /// </summary>
    public static class DialogManagerExtensions
    {
        /// <inheritdoc cref="DialogManager.ShowMessageAsync(object, string, string, DialogStyle, IDialogSettings)"/>
        /// <param name="timeout">The time, in millseconds, until the dialog is automatically closed</param>
        /// <returns>The <see cref="DialogResult"/> selected by the user, or <see cref="DialogResult.Canceled"/> if the dialog is closed by <see cref="CancellationToken"/></returns>
        public static async Task<DialogResult> ShowMessageWithTimeoutAsync(this IDialogManager value, object context, string title, string message, DialogStyle style = DialogStyle.Affirmative, IDialogSettings settings = null, int timeout = 5000)
        {
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(settings.CancellationToken))
            {
                settings.CancellationToken = linkedSource.Token;
                linkedSource.CancelAfter(timeout);

                // Supplying a CancellationToken to DialogSettings currently results in threading access error
                // See: https://github.com/MahApps/MahApps.Metro/issues/3214
                // For the time being the token must be removed, it will therefore be unable to close the dialog
                settings.CancellationToken = default;

                return await value.ShowMessageAsync(context, title, message, style, settings);
            }
        }
    }
}

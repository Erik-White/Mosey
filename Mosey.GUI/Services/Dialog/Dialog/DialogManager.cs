using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Mosey.GUI.Models.Dialog;

namespace Mosey.GUI.Services.Dialog
{
    public class DialogManager : IDialogManager
    {
        /// <summary>
        /// Gets the default instance of the dialog coordinator, which can be injected into a view model.
        /// </summary>
        private static readonly IDialogCoordinator Instance = new DialogCoordinator();

        public Task<string> ShowInputAsync(object context, string title, string message, IDialogSettings settings = null)
        {
            return Instance.ShowInputAsync(context, title, message, settings?.ToMetroDialogSettings());
        }

        public string ShowModalInputExternal(object context, string title, string message, IDialogSettings settings = null)
        {
            return Instance.ShowModalInputExternal(context, title, message, settings?.ToMetroDialogSettings());
        }

        public async Task<DialogResult> ShowMessageAsync(object context, string title, string message, DialogStyle style = DialogStyle.Affirmative, IDialogSettings settings = null)
        {
            MessageDialogResult result = await Instance.ShowMessageAsync(context, title, message, style.ToMetroDialogStyle(), settings?.ToMetroDialogSettings());

            return result.ToDialogResult();
        }

        public DialogResult ShowModalMessageExternal(object context, string title, string message, DialogStyle style = DialogStyle.Affirmative, IDialogSettings settings = null)
        {
            return Instance.ShowModalMessageExternal(context, title, message, style.ToMetroDialogStyle(), settings?.ToMetroDialogSettings()).ToDialogResult();
        }

        public async Task ShowDialogAsync(object context, IDialogInstance dialog, IDialogSettings settings = null)
        {
            // Get the current dialog
            var metroDialog = await Instance.GetCurrentDialogAsync<BaseMetroDialog>(context);

            // Perform a very simply check to see if the correct dialog is returned, then show it
            if (metroDialog != null && metroDialog.Title == dialog.Title)
            {
                await Instance.ShowMetroDialogAsync(context, metroDialog, settings?.ToMetroDialogSettings());
            }
        }

        public async Task HideDialogAsync(object context, IDialogInstance dialog, IDialogSettings settings = null)
        {
            // Get the current dialog
            var metroDialog = await Instance.GetCurrentDialogAsync<BaseMetroDialog>(context);

            // Perform a very simply check to see if the correct dialog is returned, then hide it
            if (metroDialog != null && metroDialog.Title == dialog.Title)
            {
                await Instance.HideMetroDialogAsync(context, metroDialog, settings?.ToMetroDialogSettings());
            }
        }

        public async Task<TDialog> GetCurrentDialogAsync<TDialog>(object context) where TDialog : IDialogInstance
        {
            BaseMetroDialog dialog = await Instance.GetCurrentDialogAsync<BaseMetroDialog>(context);
            return (TDialog)dialog.ToDialogInstance();
        }
    }
}

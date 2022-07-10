using System;
using System.Threading.Tasks;

namespace Mosey.Gui.Models.Dialog
{
    /// <summary>
    /// Interact with dialog instances from a view model.
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>
        /// Shows the input dialog.
        /// </summary>
        /// <param name="context">Typically this should be the view model, which you register in XAML using <see cref="DialogParticipation.SetRegister"/>.</param>
        /// <param name="title">The title of the DialogInstance.</param>
        /// <param name="message">The message contained within the DialogInstance.</param>
        /// <param name="settings">Optional settings that override the global dialog settings.</param>
        /// <returns>The text that was entered or null (Nothing in Visual Basic) if the user cancelled the operation.</returns>
        Task<string> ShowInputAsync(object context, string title, string message, IDialogSettings settings = null);

        /// <summary>
        /// Shows the input dialog.
        /// </summary>
        /// <param name="context">Typically this should be the view model, which you register in XAML using <see cref="DialogParticipation.SetRegister"/>.</param>
        /// <param name="title">The title of the DialogInstance.</param>
        /// <param name="message">The message contained within the DialogInstance.</param>
        /// <param name="settings">Optional settings that override the global dialog settings.</param>
        /// <returns>The text that was entered or null (Nothing in Visual Basic) if the user cancelled the operation.</returns>
        string ShowModalInputExternal(object context, string title, string message, IDialogSettings settings = null);

        /// <summary>
        /// Creates a DialogInstance inside of the current window.
        /// </summary>
        /// <param name="context">Typically this should be the view model, which you register in XAML using <see cref="DialogParticipation.SetRegister"/>.</param>
        /// <param name="title">The title of the DialogInstance.</param>
        /// <param name="message">The message contained within the DialogInstance.</param>
        /// <param name="style">The type of buttons to use.</param>
        /// <param name="settings">Optional settings that override the global dialog settings.</param>
        /// <returns>A task promising the result of which button was pressed.</returns>
        Task<DialogResult> ShowMessageAsync(object context, string title, string message, DialogStyle style = DialogStyle.Affirmative, IDialogSettings settings = null);

        /// <summary>
        /// Adds a Dialog instance to the specified window and makes it visible asynchronously.        
        /// <para>You have to close the resulting dialog yourself with <see cref="HideDialogAsync"/>.</para>
        /// </summary>
        /// <param name="context">Typically this should be the view model, which you register in XAML using <see cref="DialogParticipation.SetRegister"/>.</param>
        /// <param name="dialog">The dialog instance itself.</param>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="InvalidOperationException">The <paramref name="dialog"/> is already visible in the window.</exception>
        Task ShowDialogAsync(object context, IDialogInstance dialog, IDialogSettings settings = null);

        /// <summary>
        /// Hides a visible Dialog instance.
        /// </summary>
        /// <param name="context">Typically this should be the view model.</param>
        /// <param name="dialog">The dialog instance to hide.</param>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// The <paramref name="dialog"/> is not visible in the window.
        /// This happens if <see cref="ShowDialogAsync"/> hasn't been called before.
        /// </exception>
        Task HideDialogAsync(object context, IDialogInstance dialog, IDialogSettings settings = null);

        /// <summary>
        /// Gets the current shown dialog.
        /// </summary>
        /// <param name="context">Typically this should be the view model.</param>
        Task<TDialog> GetCurrentDialogAsync<TDialog>(object context) where TDialog : IDialogInstance;
    }
}

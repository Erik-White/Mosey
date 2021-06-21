using Mosey.GUI.Models.Dialog;

namespace Mosey.GUI.Services.Dialog
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
            => DialogSettings = settings;
    }
}

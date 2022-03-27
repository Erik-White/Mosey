namespace Mosey.UI.Models.Dialog
{
    public interface IDialogInstance : IDialog
    {
        IDialogSettings DialogSettings { get; }
        string Message { get; set; }
    }
}

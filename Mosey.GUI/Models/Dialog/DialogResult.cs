namespace Mosey.GUI.Models.Dialog
{
    /// <summary>
    /// An enum representing the result of a DialogInstance.
    /// </summary>
    public enum DialogResult
    {
        Canceled = -1,
        Negative = 0,
        Affirmative = 1,
        FirstAuxiliary,
        SecondAuxiliary
    }
}

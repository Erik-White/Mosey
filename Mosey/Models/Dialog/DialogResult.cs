using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models.Dialog
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

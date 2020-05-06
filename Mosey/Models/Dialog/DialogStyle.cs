using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models.Dialog
{
    public enum DialogStyle
    {
        /// <summary>
        /// Just "OK"
        /// </summary>
        Affirmative = 0,
        /// <summary>
        /// "OK" and "Cancel"
        /// </summary>
        AffirmativeAndNegative = 1,
        AffirmativeAndNegativeAndSingleAuxiliary = 2,
        AffirmativeAndNegativeAndDoubleAuxiliary = 3
    }
}

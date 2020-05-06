using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models.Dialog
{
    public interface IDialogInstance : IDialog
    {
        IDialogSettings DialogSettings { get; }
        string Message { get; set; }
    }
}

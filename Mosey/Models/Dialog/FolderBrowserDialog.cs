using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models.Dialog
{
    /// <summary>
    /// Provide an interface to allow user to select file system objects
    /// </summary>
    public interface IFileBrowserDialog : IDialog
    {
        string InitialDirectory { get; set; }
        Environment.SpecialFolder RootDirectory { get; set; }
        bool? ShowDialog();
    }

    public interface IFolderBrowserDialog : IFileBrowserDialog
    {
        string SelectedPath { get; set; }
    }
}

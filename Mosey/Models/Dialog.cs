using System;

namespace Mosey.Models
{
    public interface IDialog
    {
        string Title { get; set; }
    }

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

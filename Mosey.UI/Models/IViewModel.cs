using System.Collections.Generic;
using Mosey.Models;

namespace Mosey.UI.Models
{
    public interface IViewModel : IFactory<IViewModel>
    {
    }

    public interface IViewModelParent<T> where T : IViewModel
    {
        ICollection<T> ViewModelChildren { get; }
        T AddChildViewModel(IFactory<T> viewModel);
    }

    public interface IClosing
    {
        /// <summary>
        /// Executes when window is closing
        /// </summary>
        /// <returns>Whether the windows should be closed by the caller</returns>
        bool OnClosing();

        /// <summary>
        /// Executes when window is closing
        /// </summary>
        void OnClosingAsync();
    }
}

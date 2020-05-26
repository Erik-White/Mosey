using System;
using System.Collections.Generic;
using System.Text;

namespace Mosey.Models
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

        /// <inheritdoc cref="OnClosing"/>
        void OnClosingAsync();
    }
}

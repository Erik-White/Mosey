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
}

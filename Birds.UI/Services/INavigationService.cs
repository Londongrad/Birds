using Birds.UI.Common;
using System.Windows.Input;

namespace Birds.UI.Services
{
    public interface INavigationService
    {
        ViewModelBase? Current { get; }

        void NavigateTo(ViewModelBase viewModel);

        void NavigateToType(Type type);
        void AddCreator(Type type, Func<ViewModelBase> creator);

        ICommand NavigateToCommand { get; }
        ICommand NavigateToTypeCommand { get; }
    }
}

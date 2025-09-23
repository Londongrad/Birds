using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace Birds.UI.Services
{
    public interface INavigationService
    {
        ObservableObject? Current { get; }

        void NavigateTo(ObservableObject viewModel);

        void NavigateToType(Type type);
        void AddCreator(Type type, Func<ObservableObject> creator);

        ICommand NavigateToCommand { get; }
        ICommand NavigateToTypeCommand { get; }
    }
}

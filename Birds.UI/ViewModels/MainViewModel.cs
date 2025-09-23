using Birds.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel(INavigationService navigation) : ObservableObject
    {
        private readonly INavigationService _navigation = navigation;

        [ObservableProperty]
        private object? current;

        [RelayCommand]
        private void ShowAddBird()
        {
            _navigation.NavigateTo(new AddBirdViewModel());
            Current = _navigation.Current;
        }

        [RelayCommand]
        private void ShowBirds()
        {
            _navigation.NavigateTo(new BirdListViewModel());
            Current = _navigation.Current;
        }
    }
}

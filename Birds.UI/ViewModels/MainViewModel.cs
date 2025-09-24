using Birds.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private ObservableObject? current;

        public MainViewModel(INavigationService navigation,
                             BirdListViewModel birdVM,
                             AddBirdViewModel addBirdVM)
        {
            _navigation = navigation;

            _navigation.NavigateTo(addBirdVM);

            _navigation.AddCreator(typeof(BirdListViewModel), () => birdVM);
            _navigation.AddCreator(typeof(AddBirdViewModel), () => addBirdVM);
        }

        public INavigationService Navigation => _navigation;
    }
}

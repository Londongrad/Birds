using Birds.UI.Services.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private ObservableObject? current;

        public MainViewModel(INavigationService navigation,
                             BirdListViewModel birdsVM,
                             AddBirdViewModel addBirdVM,
                             BirdStatisticsViewModel birdStatistics)
        {
            _navigation = navigation;

            _navigation.NavigateTo(addBirdVM);

            _navigation.AddCreator<BirdListViewModel>(() => birdsVM);
            _navigation.AddCreator<AddBirdViewModel>(() => addBirdVM);
            _navigation.AddCreator<BirdStatisticsViewModel>(() => birdStatistics);
        }

        public INavigationService Navigation => _navigation;
    }
}
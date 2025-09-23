using Birds.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;

namespace Birds.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigation;
        private readonly BirdListViewModel birdVM;
        private readonly AddBirdViewModel addBirdVM;

        [ObservableProperty]
        private ObservableObject? current;

        public MainViewModel(INavigationService navigation, IMediator mediator)
        {
            _navigation = navigation;

            birdVM = new();
            addBirdVM = new(mediator);

            _navigation.AddCreator(typeof(BirdListViewModel), () => birdVM);
            _navigation.AddCreator(typeof(AddBirdViewModel), () => addBirdVM);
        }

        public INavigationService Navigation => _navigation;
    }
}

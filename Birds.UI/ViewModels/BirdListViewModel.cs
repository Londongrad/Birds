using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        public BirdListViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [ObservableProperty]
        private ObservableCollection<BirdDTO> birds = new();

        [RelayCommand]
        private async Task LoadAsync()
        {
            var result = await _mediator.Send(new GetAllBirdsQuery());

            Birds = new ObservableCollection<BirdDTO>(result);
        }
    }
}

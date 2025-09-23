using Birds.Application.Commands.CreateBird;
using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels
{
    public partial class AddBirdViewModel(IMediator mediator) : ObservableObject
    {
        private readonly IMediator _mediator = mediator;

        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        [ObservableProperty] private BirdsName selectedBirdName;
        [ObservableProperty] private string? description;
        [ObservableProperty] private DateTime arrival = DateTime.UtcNow;

        [RelayCommand]
        private async Task SaveAsync()
        {
            var command = new CreateBirdCommand(
                SelectedBirdName,
                Description,
                DateOnly.FromDateTime(Arrival)
            );

            await _mediator.Send(command);
        }
    }
}

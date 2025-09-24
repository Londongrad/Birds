using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject, INotificationHandler<BirdCreatedNotification>
    {
        private readonly IMediator _mediator;
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        public BirdListViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [ObservableProperty]
        private ObservableCollection<BirdDTO> birds = new();

        [RelayCommand]
        private async Task LoadAsync()
        {
            Birds.Clear();
            var result = await _mediator.Send(new GetAllBirdsQuery());
            foreach (var bird in result)
                Birds.Add(bird);
        }

        public Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            Birds.Add(notification.Bird);
            return Task.CompletedTask;
        }
    }
}

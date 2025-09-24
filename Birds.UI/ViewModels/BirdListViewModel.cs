using Birds.Application.DTOs;
using Birds.Application.Notifications;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Enums;
using Birds.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Birds.UI.ViewModels
{
    public partial class BirdListViewModel : ObservableObject, 
                                             INotificationHandler<BirdCreatedNotification>,
                                             IAsyncNavigatedTo
    {
        private readonly IMediator _mediator;
        private bool _isLoaded;
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        public BirdListViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [ObservableProperty]
        private ObservableCollection<BirdDTO> birds = new();

        private async Task LoadAsync()
        {
            Debug.WriteLine("Loading birds...");
            Birds.Clear();
            var result = await _mediator.Send(new GetAllBirdsQuery());
            foreach (var bird in result)
                Birds.Add(bird);

            _isLoaded = true;
        }

        public Task Handle(BirdCreatedNotification notification, CancellationToken cancellationToken)
        {
            Birds.Add(notification.Bird);
            return Task.CompletedTask;
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isLoaded)
            {
                await LoadAsync();
            }
        }
    }
}

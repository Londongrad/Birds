using Birds.Application.Commands.DeleteBird;
using Birds.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels
{
    public partial class BirdViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        public BirdViewModel(BirdDTO dto, IMediator mediator)
        {
            Dto = dto;
            _mediator = mediator;
            _isAlive = dto.IsAlive;
        }

        public BirdDTO Dto { get; }

        public Guid Id => Dto.Id;

        public string Name => Dto.Name;
        public string? Description => Dto.Description;
        public DateOnly Arrival => Dto.Arrival;
        public DateOnly? Departure => Dto.Departure;

        [ObservableProperty]
        private bool _isAlive;

        // Пример derived-свойства
        //public int Age => (DateTime.Today - Dto.Arrival).Days;

        [RelayCommand]
        private async Task DeleteAsync()
        {
            await _mediator.Send(new DeleteBirdCommand(Id));
        }

        [RelayCommand]
        private void ToggleAlive()
        {
            IsAlive = !IsAlive;
        }
    }
}
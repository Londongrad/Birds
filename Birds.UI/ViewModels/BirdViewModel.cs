using Birds.Application.Commands.DeleteBird;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Diagnostics;

namespace Birds.UI.ViewModels
{
    public partial class BirdViewModel : ObservableValidator
    {
        private readonly IMediator _mediator;

        public BirdViewModel(BirdDTO dto, IMediator mediator)
        {
            Debug.WriteLine($"Item with id = {dto.Id} was created.");
            Dto = dto;
            _mediator = mediator;
            Name = dto.Name;
            Description = dto.Description;
            Arrival = dto.Arrival;
            Departure = dto.Departure;
            isAlive = dto.IsAlive;
        }

        #region [ Properties ]

        public BirdDTO Dto { get; }
        public Guid Id => Dto.Id;

        #endregion [ Properties ]

        #region [ ObservableProperties ]

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string? description;

        [ObservableProperty]
        private DateOnly arrival;

        [ObservableProperty]
        private DateOnly? departure;

        [ObservableProperty]
        private bool isAlive;

        [ObservableProperty]
        private bool isConfirmingDelete;

        [ObservableProperty]
        private bool isEditing;

        #endregion [ ObservableProperties ]

        #region [ Commands ]

        #region [ Commands/Delete ]

        [RelayCommand]
        private async Task DeleteAsync()
        {
            await _mediator.Send(new DeleteBirdCommand(Id));
            IsConfirmingDelete = false;
        }

        [RelayCommand]
        private void ToggleAlive() => IsAlive = !IsAlive;

        [RelayCommand]
        private void AskDelete() => IsConfirmingDelete = true;

        [RelayCommand]
        private void CancelDelete() => IsConfirmingDelete = false;

        #endregion [ Commands/Delete ]

        #region [ Commands/Edit ]

        [RelayCommand]
        private void Edit() => IsEditing = true;

        [RelayCommand]
        private void CancelEdit()
        {
            // тут можно откатить изменения, если хранить копию DTO
            IsEditing = false;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            ValidateAllProperties();
            if (HasErrors)
                return;

            await _mediator.Send(
                new UpdateBirdCommand(Id,
                    SelectedBirdName ?? default,
                    Description,
                    Arrival,
                    Departure,
                    IsAlive));

            // После сохранения пересчитываем производные поля
            UpdateCalculatedFields();
            IsEditing = false;
        }

        #endregion [ Commands/Edit ]

        #endregion [ Commands ]
    }
}
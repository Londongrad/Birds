using Birds.Application.Commands.CreateBird;
using Birds.Application.Common.Models;
using Birds.UI.Services.Notification;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the "Add Bird" view.
    /// Inherits common fields and validation rules from <see cref="BirdValidationBaseViewModel"/>.
    /// </summary>
    /// <remarks>
    /// Contains commands for saving a new bird and notifying the user about the result.
    /// </remarks>
    public partial class AddBirdViewModel : BirdValidationBaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly INotificationService _notification;

        /// <summary>
        /// Creates a new instance of <see cref="AddBirdViewModel"/>.
        /// </summary>
        /// <param name="mediator">The MediatR mediator used to send application commands.</param>
        /// <param name="notification">The user notification service.</param>
        public AddBirdViewModel(IMediator mediator, INotificationService notification)
        {
            _mediator = mediator;
            _notification = notification;

            // When validation errors change — update the Save command availability
            ErrorsChanged += (_, __) => SaveCommand.NotifyCanExecuteChanged();

            // Run initial validation so that the Save button is disabled by default
            ValidateAllProperties();
        }

        /// <summary>
        /// Determines whether the Save command can be executed.
        /// </summary>
        private bool CanSave() => !HasErrors;

        /// <summary>
        /// Command to save a new bird to the system.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            // Force validation of all properties before saving
            ValidateAllProperties();
            if (HasErrors)
               return;

            var command = new CreateBirdCommand(
                SelectedBirdName ?? default,
                Description,
                Arrival
            );

            Result result = await _mediator.Send(command);

            if (result.IsSuccess)
                _notification.ShowSuccess("Bird added successfully!");
            else
                _notification.ShowError("Unable to save bird");

            // Reset the description after successful save
            Description = string.Empty;
        }
    }
}
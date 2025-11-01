using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        #region [ Fields ]

        private readonly INotificationService _notification;
        private readonly IBirdManager _birdManager;

        #endregion [ Fields ]

        /// <summary>
        /// Creates a new instance of <see cref="AddBirdViewModel"/>.
        /// </summary>
        /// <param name="mediator">The MediatR mediator used to send application commands.</param>
        /// <param name="notification">The user notification service.</param>
        public AddBirdViewModel(INotificationService notification, IBirdManager birdManager)
        {
            _notification = notification;
            _birdManager = birdManager;

            // When validation errors change — update the Save command availability
            ErrorsChanged += (_, __) => SaveCommand.NotifyCanExecuteChanged();

            // Run initial validation so that the Save button is disabled by default
            ValidateAllProperties();
        }

        #region [ Observable Properties ]

        [ObservableProperty]
        private bool isBusy; // Indicates whether the add operation is in progress

        [ObservableProperty]
        private bool isOneTime;

        #endregion [ Observable Properties ]

        #region [ Methods ]

        /// <summary>
        /// Determines whether the Save command can be executed.
        /// </summary>
        private bool CanSave() => !HasErrors && !IsBusy;

        #endregion [ Methods ]

        #region [ Commands ]

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

            IsBusy = true; // Disable button
            SaveCommand.NotifyCanExecuteChanged();

            _notification.ShowInfo(InfoMessages.AddingBird);

            var dto = new BirdCreateDTO(
                SelectedBirdName ?? default,
                Description,
                Arrival,
                IsOneTime ? Arrival : null,
                !IsOneTime
            );

            Result<BirdDTO> result = await _birdManager.AddAsync(dto, CancellationToken.None);

            if (result.IsSuccess)
            {
                _notification.ShowSuccess(InfoMessages.BirdAdded);
                // Reset the description after successful save
                Description = string.Empty;
            }
            else
                _notification.ShowError("Unable to save bird");

            IsBusy = false; // Enable button again
            SaveCommand.NotifyCanExecuteChanged();
        }

        #endregion [ Commands ]
    }
}
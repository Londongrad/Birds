using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Birds.UI.ViewModels
{
    /// <summary>
    /// ViewModel representing a single bird, used for display and editing.
    /// Inherits common properties and validation rules from <see cref="BirdValidationBaseViewModel"/>.
    /// </summary>
    public partial class BirdViewModel : BirdValidationBaseViewModel
    {
        #region [ Fields ]

        private readonly IBirdManager _birdManager;
        private readonly INotificationService _notificationService;

        #endregion [ Fields ]

        public BirdViewModel(BirdDTO dto, IBirdManager birdManager, INotificationService notificationService)
        {
            Debug.WriteLine($"Item with id = {dto.Id} was created.");

            Dto = dto;

            _birdManager = birdManager;
            _notificationService = notificationService;

            Name = dto.Name;
            SelectedBirdName = BirdEnumHelper.ParseBirdName(dto.Name);  // property from base class
            Description = dto.Description;
            Arrival = dto.Arrival;
            Departure = dto.Departure;
            IsAlive = dto.IsAlive;

            UpdateCalculatedFields();
        }

        #region [ Properties ]

        /// <summary>
        /// The original bird DTO object.
        /// </summary>
        public BirdDTO Dto { get; }

        /// <summary>
        /// The unique identifier of the bird.
        /// </summary>
        public Guid Id => Dto.Id;

        #endregion [ Properties ]

        #region [ ObservableProperties ]

        /// <summary>
        /// The bird’s name.
        /// </summary>
        [ObservableProperty]
        private string name;

        /// <summary>
        /// The departure date of the bird (if it has left the record).
        /// </summary>
        [CustomValidation(typeof(BirdViewModel), nameof(ValidateDeparture))]
        [ObservableProperty]
        private DateOnly? departure;

        /// <summary>
        /// Indicates whether the bird is alive.
        /// </summary>
        [ObservableProperty]
        private bool isAlive;

        /// <summary>
        /// The number of days since arrival (or until departure).
        /// </summary>
        [ObservableProperty]
        private int daysInStock;

        /// <summary>
        /// A textual representation of the departure date (or “to this day” if none).
        /// </summary>
        [ObservableProperty]
        private string? departureDisplay;

        /// <summary>
        /// Determines whether delete confirmation buttons are visible.
        /// </summary>
        [ObservableProperty]
        private bool isConfirmingDelete;

        /// <summary>
        /// Indicates whether the item is in edit mode.
        /// </summary>
        [ObservableProperty]
        private bool isEditing;

        #endregion [ ObservableProperties ]

        #region [ Commands ]

        /// <summary>
        /// Command for deleting a bird.
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            Result result = await _birdManager.DeleteAsync(Id, CancellationToken.None);

            if (result.IsSuccess)
                _notificationService.ShowSuccess("Bird deleted successfully!");
            else
                _notificationService.ShowError("Unable to delete bird.");

            IsConfirmingDelete = false;
        }

        /// <summary>
        /// Command for displaying delete confirmation buttons.
        /// </summary>
        [RelayCommand]
        private void AskDelete() => IsConfirmingDelete = true;

        /// <summary>
        /// Command for canceling delete confirmation.
        /// </summary>
        [RelayCommand]
        private void CancelDelete() => IsConfirmingDelete = false;

        /// <summary>
        /// Command for switching to edit mode.
        /// </summary>
        [RelayCommand]
        private void Edit() => IsEditing = true;

        /// <summary>
        /// Command for canceling editing and reverting changes.
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            // Restore data from DTO
            SelectedBirdName = BirdEnumHelper.ParseBirdName(Dto.Name);
            Description = Dto.Description;
            Arrival = Dto.Arrival;
            Departure = Dto.Departure;
            IsAlive = Dto.IsAlive;
            IsEditing = false;
        }

        /// <summary>
        /// Command that validates user input and updates the bird through <see cref="IBirdManager"/>.
        /// </summary>
        /// <remarks>
        /// If validation passes, the method sends an update request via <see cref="IBirdManager"/>.  
        /// Upon completion, a success or error notification is displayed to the user using  
        /// <see cref="INotificationService"/>.  
        /// 
        /// After a successful update, calculated fields (e.g. days in stock and departure display)  
        /// are refreshed, and edit mode is turned off.
        /// </remarks>
        [RelayCommand]
        private async Task SaveAsync()
        {
            ValidateAllProperties();
            if (HasErrors)
                return;

            Result result = await _birdManager.UpdateAsync(
                new BirdDTO(
                    Id,
                    Name,
                    Description,
                    Arrival,
                    Departure,
                    IsAlive), CancellationToken.None);

            if (result.IsSuccess)
                _notificationService.ShowSuccess("Bird updated successfully!");
            else
                _notificationService.ShowError("Unable to update bird.");

            // Recalculate derived fields after saving
            UpdateCalculatedFields();
            IsEditing = false;
        }

        /// <summary>
        /// For the DatePicker to be able to clear the departure date.
        /// </summary>
        [RelayCommand]
        private void ClearDeparture()
        {
            Departure = null;
        }

        #endregion [ Commands ]

        #region [ Private helpers ]

        /// <summary>
        /// Updates calculated fields (<see cref="DepartureDisplay"/> and <see cref="DaysInStock"/>).
        /// </summary>
        private void UpdateCalculatedFields()
        {
            DepartureDisplay = Departure.HasValue
                ? Departure.Value.ToString("dd.MM.yyyy")
                : "to this day";

            var endDate = Departure?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            DaysInStock = (int)(endDate - Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays;
        }

        /// <summary>
        /// Automatically invoked when the departure date changes (partial method generated by the Toolkit).
        /// </summary>
        partial void OnDepartureChanged(DateOnly? value)
        {
            ValidateProperty(value, nameof(Departure));
            UpdateCalculatedFields();
        }

        /// <summary>
        /// Overrides logic executed when the arrival date changes.
        /// </summary>
        protected override void OnArrivalChangedCore(DateOnly value)
        {
            // When Arrival changes, revalidate Departure since its rule depends on Arrival
            ValidateProperty(Departure, nameof(Departure));
            UpdateCalculatedFields();
        }

        #endregion [ Private helpers ]

        #region [ Validation ]

        /// <summary>
        /// Validates the departure date.
        /// Allows null, disallows future dates and dates earlier than Arrival.
        /// 
        /// <para>
        /// Moved from <see cref="BirdValidationBaseViewModel"/> since it is only required in this ViewModel.
        /// </para>
        /// </summary>
        public static ValidationResult? ValidateDeparture(object? value, ValidationContext ctx)
        {
            if (ctx.ObjectInstance is BirdViewModel birdVM && value is null && birdVM.IsAlive == false)
                return new ValidationResult("Please specify the date first.");

            if (value is null)
                return ValidationResult.Success;

            if (value is not DateOnly d)
                return new ValidationResult("Please specify a valid date.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (d > today)
                return new ValidationResult($"The date cannot be in the future (no later than {today:dd-MM-yyyy}).");

            // Access Arrival via context (BirdViewModel inherits from the base class)
            if (ctx.ObjectInstance is BirdValidationBaseViewModel vm && d < vm.Arrival)
                return new ValidationResult($"Departure date cannot be earlier than arrival date ({vm.Arrival:dd-MM-yyyy}).");

            return ValidationResult.Success;
        }

        partial void OnIsAliveChanged(bool value)
        {
            // If the bird is marked as dead — validate the Departure date
            ValidateProperty(Departure, nameof(Departure));
        }

        #endregion [ Validation ]
    }
}

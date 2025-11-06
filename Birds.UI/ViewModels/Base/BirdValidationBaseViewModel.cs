using Birds.Domain.Enums;
using Birds.Shared.Constants;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Birds.UI.ViewModels.Base
{
    /// <summary>
    /// Base model for validating common bird properties (species, description, arrival date).
    /// Serves as the foundation for <see cref="AddBirdViewModel"/> and <see cref="BirdViewModel"/> (editing).
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="ObservableValidator"/> to automatically support
    /// property validation through <see cref="ValidationAttribute"/> annotations.
    /// </remarks>
    public abstract partial class BirdValidationBaseViewModel : ObservableValidator
    {
        #region [ Properties ]

        /// <summary>
        /// A list of available bird species obtained from the <see cref="BirdsName"/> enumeration.
        /// </summary>
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        /// <summary>
        /// The selected bird species.
        /// Required; validation will fail if not provided.
        /// </summary>
        [Required(ErrorMessage = ValidationMessages.UnselectedBird)]
        [ObservableProperty]
        private BirdsName? selectedBirdName;

        /// <summary>
        /// The bird description.
        /// Optional, but limited to 100 characters.
        /// </summary>
        [MaxLength(100, ErrorMessage = ValidationMessages.LongDescription)]
        [ObservableProperty]
        private string? description;

        /// <summary>
        /// The bird's arrival date.
        /// Required and validated by <see cref="ValidateArrival"/>.
        /// </summary>
        [CustomValidation(typeof(BirdValidationBaseViewModel), nameof(ValidateArrival))]
        [Required(ErrorMessage = ValidationMessages.DateIsNotSpecified)]
        [ObservableProperty]
        private DateOnly arrival = DateOnly.FromDateTime(DateTime.Now);

        #endregion [ Properties ]

        #region [ Validation ]

        /// <summary>
        /// Validates the correctness of the bird's arrival date.
        /// </summary>
        /// <param name="value">The date value to validate.</param>
        /// <param name="_">Validation context (not used).</param>
        /// <returns>
        /// <see cref="ValidationResult.Success"/> if the date is valid;
        /// otherwise, an error describing the acceptable range.
        /// </returns>
        public static ValidationResult? ValidateArrival(object? value, ValidationContext _)
        {
            if (value is not DateOnly d)
                return new ValidationResult(ValidationMessages.DateIsNotSpecified);

            var min = new DateOnly(2020, 1, 1);
            var max = DateOnly.FromDateTime(DateTime.Today);

            if (d < min || d > max)
                return new ValidationResult(
                    string.Format(CultureInfo.CurrentCulture, ValidationMessages.InvalidDateRange, min, max));

            return ValidationResult.Success;
        }

        /// <summary>
        /// Automatically triggers property validation when the selected bird species changes.
        /// </summary>
        /// <param name="value">The new value of the <see cref="SelectedBirdName"/> property.</param>
        partial void OnSelectedBirdNameChanged(BirdsName? value)
            => ValidateProperty(value, nameof(SelectedBirdName));

        /// <summary>
        /// Automatically triggers property validation when the description changes.
        /// </summary>
        /// <param name="value">The new value of the <see cref="Description"/> property.</param>
        partial void OnDescriptionChanged(string? value)
            => ValidateProperty(value, nameof(Description));

        /// <summary>
        /// Automatically triggers property validation when the arrival date changes.
        /// </summary>
        /// <param name="value">The new value of the <see cref="Arrival"/> property.</param>
        partial void OnArrivalChanged(DateOnly value)
        {
            ValidateProperty(value, nameof(Arrival));
            OnArrivalChangedCore(value);
        }

        /// <summary>
        /// Invoked when the arrival date changes (can be overridden in derived classes).
        /// </summary>
        protected virtual void OnArrivalChangedCore(DateOnly value)
        { }

        #endregion [ Validation ]
    }
}
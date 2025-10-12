using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Select a bird species")]
        [ObservableProperty]
        private BirdsName? selectedBirdName;

        /// <summary>
        /// The bird description.
        /// Optional, but limited to 100 characters.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Description is too long")]
        [ObservableProperty]
        private string? description;

        /// <summary>
        /// The bird's arrival date.
        /// Required and validated by <see cref="ValidateArrival"/>.
        /// </summary>
        [CustomValidation(typeof(BirdValidationBaseViewModel), nameof(ValidateArrival))]
        [Required(ErrorMessage = "Specify the date")]
        [ObservableProperty]
        private DateOnly arrival = DateOnly.FromDateTime(DateTime.Now);

        #endregion

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
                return new ValidationResult("Specify the date");

            var min = new DateOnly(2020, 1, 1);
            var max = DateOnly.FromDateTime(DateTime.Today);

            if (d < min || d > max)
                return new ValidationResult($"Date must be between {min:dd-MM-yyyy} and {max:dd-MM-yyyy}");

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
        protected virtual void OnArrivalChangedCore(DateOnly value) { }

        #endregion
    }
}

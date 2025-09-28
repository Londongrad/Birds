using Birds.Application.Commands.CreateBird;
using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Birds.UI.ViewModels
{
    public partial class AddBirdViewModel : ObservableValidator
    {
        public AddBirdViewModel(IMediator mediator)
        {
            _mediator = mediator;

            // При любом изменении ошибок обновляем доступность кнопки
            ErrorsChanged += (_, __) => SaveCommand.NotifyCanExecuteChanged();

            // Чтобы при старте кнопка была выключена
            ValidateAllProperties();
        }

        #region [ Fields ]

        private readonly IMediator _mediator;

        #endregion [ Fields ]

        #region [ Properties ]

        private bool CanSave() => !HasErrors;

        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        [Required(ErrorMessage = "Выберите вид птицы")]
        [ObservableProperty]
        private BirdsName? selectedBirdName;

        [MaxLength(100, ErrorMessage = "Описание слишком длинное")]
        [ObservableProperty]
        private string? description;

        [CustomValidation(typeof(AddBirdViewModel), nameof(ValidateArrival))]
        [Required(ErrorMessage = "Укажите дату")]
        [ObservableProperty]
        private DateOnly arrival = DateOnly.FromDateTime(DateTime.Now);

        #endregion [ Properties ]

        #region [ Commands ]

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            var command = new CreateBirdCommand(
                SelectedBirdName ?? default,
                Description,
                Arrival
            );

            await _mediator.Send(command);
        }

        #endregion [ Commands ]

        #region [ Validation ]

        // кастомная валидация даты
        public static ValidationResult? ValidateArrival(object? value, ValidationContext _)
        {
            if (value is not DateOnly d)
                return new ValidationResult("Укажите дату");

            var min = new DateOnly(2020, 1, 1);
            var max = DateOnly.FromDateTime(DateTime.Today);

            if (d < min || d > max)
                return new ValidationResult($"Дата должна быть в диапазоне {min:dd-MM-yyyy} – {max:dd-MM-yyyy}");

            return ValidationResult.Success;
        }

        // Автоматическая проверка при изменении свойств
        partial void OnSelectedBirdNameChanged(BirdsName? value)
            => ValidateProperty(value, nameof(SelectedBirdName));

        partial void OnDescriptionChanged(string? value)
            => ValidateProperty(value, nameof(Description));

        partial void OnArrivalChanged(DateOnly value)
            => ValidateProperty(value, nameof(Arrival));

        #endregion [ Validation ]
    }
}
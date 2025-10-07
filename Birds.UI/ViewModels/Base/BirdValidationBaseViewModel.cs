using Birds.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Birds.UI.ViewModels.Base
{
    /// <summary>
    /// Базовая модель для валидации общих свойств птиц (вид, описание, дата прибытия).
    /// Используется как основа для AddBirdViewModel и BirdViewModel (редактирование).
    /// </summary>
    /// <remarks>
    /// Наследуется от <see cref="ObservableValidator"/> для автоматической поддержки
    /// проверки свойств через аннотации <see cref="ValidationAttribute"/>.
    /// </remarks>
    public abstract partial class BirdValidationBaseViewModel : ObservableValidator
    {
        #region [ Properties ]

        /// <summary>
        /// Список доступных видов птиц, получаемый из перечисления <see cref="BirdsName"/>.
        /// </summary>
        public static Array BirdNames => Enum.GetValues(typeof(BirdsName));

        /// <summary>
        /// Выбранный вид птицы.
        /// Обязательно для заполнения, иначе валидация выдаст ошибку.
        /// </summary>
        [Required(ErrorMessage = "Выберите вид птицы")]
        [ObservableProperty]
        private BirdsName? selectedBirdName;

        /// <summary>
        /// Описание птицы.
        /// Необязательно, но ограничено длиной до 100 символов.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Описание слишком длинное")]
        [ObservableProperty]
        private string? description;

        /// <summary>
        /// Дата прибытия птицы.
        /// Обязательно для заполнения, проверяется методом <see cref="ValidateArrival"/>.
        /// </summary>
        [CustomValidation(typeof(BirdValidationBaseViewModel), nameof(ValidateArrival))]
        [Required(ErrorMessage = "Укажите дату")]
        [ObservableProperty]
        private DateOnly arrival = DateOnly.FromDateTime(DateTime.Now);

        #endregion

        #region [ Validation ]

        /// <summary>
        /// Проверяет корректность даты прибытия птицы.
        /// </summary>
        /// <param name="value">Проверяемое значение даты.</param>
        /// <param name="_">Контекст проверки (не используется).</param>
        /// <returns>
        /// <see cref="ValidationResult.Success"/>, если дата корректна;
        /// иначе — ошибка с текстом, описывающим допустимый диапазон.
        /// </returns>
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

        /// <summary>
        /// Автоматически вызывает проверку свойства при изменении выбранного вида птицы.
        /// </summary>
        /// <param name="value">Новое значение свойства <see cref="SelectedBirdName"/>.</param>
        partial void OnSelectedBirdNameChanged(BirdsName? value)
            => ValidateProperty(value, nameof(SelectedBirdName));

        /// <summary>
        /// Автоматически вызывает проверку свойства при изменении описания.
        /// </summary>
        /// <param name="value">Новое значение свойства <see cref="Description"/>.</param>
        partial void OnDescriptionChanged(string? value)
            => ValidateProperty(value, nameof(Description));

        /// <summary>
        /// Автоматически вызывает проверку свойства при изменении даты прибытия.
        /// </summary>
        /// <param name="value">Новое значение свойства <see cref="Arrival"/>.</param>
        partial void OnArrivalChanged(DateOnly value)
        {
            ValidateProperty(value, nameof(Arrival));
            OnArrivalChangedCore(value);
        }

        /// <summary>
        /// Вызывается при изменении даты прибытия (для переопределения в потомках).
        /// </summary>
        protected virtual void OnArrivalChangedCore(DateOnly value) { }

        #endregion
    }
}

using Birds.Application.Commands.DeleteBird;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Birds.UI.ViewModels
{
    /// <summary>
    /// ViewModel для отдельной птицы, используется для отображения и редактирования.
    /// Наследует общие свойства и правила валидации из <see cref="BirdValidationBaseViewModel"/>.
    /// </summary>
    public partial class BirdViewModel : BirdValidationBaseViewModel
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Создаёт экземпляр <see cref="BirdViewModel"/> и инициализирует его данными из <see cref="BirdDTO"/>.
        /// </summary>
        /// <param name="dto">DTO-модель птицы, полученная из слоя приложения.</param>
        /// <param name="mediator">MediatR-медиатор для отправки команд (удаление, обновление).</param>
        public BirdViewModel(BirdDTO dto, IMediator mediator)
        {
            Debug.WriteLine($"Item with id = {dto.Id} was created.");

            Dto = dto;
            _mediator = mediator;

            Name = dto.Name;
            SelectedBirdName = Enum.TryParse<BirdsName>(dto.Name, out var bird) ? bird : null;  // свойство из базового класса
            Description = dto.Description;
            Arrival = dto.Arrival;
            Departure = dto.Departure;
            IsAlive = dto.IsAlive;

            UpdateCalculatedFields();
        }

        #region [ Properties ]

        /// <summary>
        /// Исходный DTO-объект птицы.
        /// </summary>
        public BirdDTO Dto { get; }

        /// <summary>
        /// Уникальный идентификатор птицы.
        /// </summary>
        public Guid Id => Dto.Id;

        #endregion

        #region [ ObservableProperties ]

        /// <summary>
        /// Имя птицы.
        /// </summary>
        [ObservableProperty]
        private string name;

        /// <summary>
        /// Дата убытия птицы (если она уже покинула учёт).
        /// </summary>
        [CustomValidation(typeof(BirdViewModel), nameof(ValidateDeparture))]
        [ObservableProperty]
        private DateOnly? departure;

        /// <summary>
        /// Признак того, что птица жива.
        /// </summary>
        [ObservableProperty]
        private bool isAlive;

        /// <summary>
        /// Количество дней, прошедших с момента прибытия (или до убытия).
        /// </summary>
        [ObservableProperty]
        private int daysInStock;

        /// <summary>
        /// Строковое представление даты убытия (или текста "по сей день").
        /// </summary>
        [ObservableProperty]
        private string? departureDisplay;

        /// <summary>
        /// Определяет, отображаются ли кнопки подтверждения удаления.
        /// </summary>
        [ObservableProperty]
        private bool isConfirmingDelete;

        /// <summary>
        /// Определяет, находится ли элемент в режиме редактирования.
        /// </summary>
        [ObservableProperty]
        private bool isEditing;

        #endregion

        #region [ Commands ]

        /// <summary>
        /// Команда удаления птицы.
        /// </summary>
        [RelayCommand]
        private async Task DeleteAsync()
        {
            await _mediator.Send(new DeleteBirdCommand(Id));
            IsConfirmingDelete = false;
        }

        /// <summary>
        /// Команда переключения состояния "жива/не жива".
        /// </summary>
        [RelayCommand]
        private void ToggleAlive() => IsAlive = !IsAlive;

        /// <summary>
        /// Команда отображения кнопок подтверждения удаления.
        /// </summary>
        [RelayCommand]
        private void AskDelete() => IsConfirmingDelete = true;

        /// <summary>
        /// Команда отмены подтверждения удаления.
        /// </summary>
        [RelayCommand]
        private void CancelDelete() => IsConfirmingDelete = false;

        /// <summary>
        /// Команда перехода в режим редактирования.
        /// </summary>
        [RelayCommand]
        private void Edit() => IsEditing = true;

        /// <summary>
        /// Команда отмены редактирования и отката изменений.
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            // при желании можно восстановить данные из Dto
            Description = Dto.Description;
            Arrival = Dto.Arrival;
            Departure = Dto.Departure;
            IsAlive = Dto.IsAlive;
            IsEditing = false;
        }

        /// <summary>
        /// Команда сохранения изменений после редактирования.
        /// Выполняет валидацию и обновление через MediatR.
        /// </summary>
        [RelayCommand]
        private async Task SaveAsync()
        {
            ValidateAllProperties();
            if (HasErrors)
                return;

            await _mediator.Send(
                new UpdateBirdCommand(
                    Id,
                    SelectedBirdName ?? default,
                    Description,
                    Arrival,
                    Departure,
                    IsAlive));

            // После сохранения пересчитываем производные поля
            UpdateCalculatedFields();
            IsEditing = false;
        }

        #endregion

        #region [ Private helpers ]

        /// <summary>
        /// Обновляет вычисляемые поля (DepartureDisplay и DaysInStock).
        /// </summary>
        private void UpdateCalculatedFields()
        {
            DepartureDisplay = Departure.HasValue
                ? Departure.Value.ToString("dd.MM.yyyy")
                : "по сей день";

            var endDate = Departure?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            DaysInStock = (int)(endDate - Arrival.ToDateTime(TimeOnly.MinValue)).TotalDays;
        }

        /// <summary>
        /// Вызывается автоматически при изменении даты отправления (partial-метод от Toolkit).
        /// </summary>
        partial void OnDepartureChanged(DateOnly? value)
        {
            ValidateProperty(value, nameof(Departure));
            UpdateCalculatedFields();
        }

        /// <summary>
        /// Переопределение логики, вызываемой при изменении даты прибытия.
        /// </summary>
        protected override void OnArrivalChangedCore(DateOnly value)
        {
            // если Arrival меняют — перепроверяем Departure, т.к. правило зависит от Arrival
            ValidateProperty(Departure, nameof(Departure));
            UpdateCalculatedFields();
        }

        #endregion

        #region [ Validation ]

        /// <summary>
        /// Валидация даты убытия.
        /// Разрешает null, запрещает будущее и дату раньше Arrival.
        /// 
        /// <para>
        /// Вынесен из базовой <see cref="BirdValidationBaseViewModel"/>, так как нужен только в этой ViewModel
        /// </para>
        /// </summary>
        public static ValidationResult? ValidateDeparture(object? value, ValidationContext ctx)
        {
            if (value is null)
                return ValidationResult.Success;

            if (value is not DateOnly d)
                return new ValidationResult("Укажите корректную дату");

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (d > today)
                return new ValidationResult($"Дата не может быть в будущем (не позже {today:dd-MM-yyyy})");

            // доступ к Arrival через контекст (BirdViewModel наследуется от базового класса)
            if (ctx.ObjectInstance is BirdValidationBaseViewModel vm && d < vm.Arrival)
                return new ValidationResult($"Дата убытия не может быть раньше даты прибытия ({vm.Arrival:dd-MM-yyyy})");

            return ValidationResult.Success;
        }

        #endregion [ Validation ]
    }
}

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
    /// <summary>
    /// ViewModel для отдельной птицы, используется для отображения и редактирования.
    /// Наследует общие свойства и правила валидации из <see cref="BirdValidationBaseViewModel"/>.
    /// </summary>
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
            Description = dto.Description;
            Arrival = dto.Arrival;
            Departure = dto.Departure;
            isAlive = dto.IsAlive;
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
        [ObservableProperty]
        private string? description;

        /// <summary>
        /// Признак того, что птица жива.
        /// </summary>
        [ObservableProperty]
        private DateOnly arrival;

        /// <summary>
        /// Количество дней, прошедших с момента прибытия (или до убытия).
        /// </summary>
        [ObservableProperty]
        private DateOnly? departure;

        /// <summary>
        /// Строковое представление даты убытия (или текста "по сей день").
        /// </summary>
        [ObservableProperty]
        private bool isAlive;

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

        #endregion [ ObservableProperties ]

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
            // тут можно откатить изменения, если хранить копию DTO
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
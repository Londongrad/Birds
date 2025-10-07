using Birds.Application.Commands.CreateBird;
using Birds.UI.Services.Notification;
using Birds.UI.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Birds.UI.ViewModels
{
    /// <summary>
    /// ViewModel для представления "Добавить птицу".
    /// Наследует общие поля и правила валидации из <see cref="BirdValidationBaseViewModel"/>.
    /// </summary>
    /// <remarks>
    /// Содержит команды для сохранения новой птицы и уведомления пользователя о результате.
    /// </remarks>
    public partial class AddBirdViewModel : BirdValidationBaseViewModel
    {
        private readonly IMediator _mediator;
        private readonly INotificationService _notification;

        /// <summary>
        /// Создаёт новый экземпляр <see cref="AddBirdViewModel"/>.
        /// </summary>
        /// <param name="mediator">MediatR-медиатор для отправки команд приложения.</param>
        /// <param name="notification">Сервис уведомлений пользователя.</param>
        public AddBirdViewModel(IMediator mediator, INotificationService notification)
        {
            _mediator = mediator;
            _notification = notification;

            // При изменении ошибок — обновляем доступность команды сохранения
            ErrorsChanged += (_, __) => SaveCommand.NotifyCanExecuteChanged();

            // Прогоняем первичную валидацию, чтобы кнопка Save сразу была недоступна
            ValidateAllProperties();
        }

        /// <summary>
        /// Определяет, можно ли выполнить команду сохранения.
        /// </summary>
        private bool CanSave() => !HasErrors;

        /// <summary>
        /// Команда сохранения новой птицы в систему.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            // Принудительная проверка всех свойств перед сохранением
            ValidateAllProperties();
            if (HasErrors)
                return;

            var command = new CreateBirdCommand(
                SelectedBirdName ?? default,
                Description,
                Arrival
            );

            await _mediator.Send(command);
            _notification.ShowSuccess("Bird added successfully!");

            // Сброс описания после успешного сохранения
            Description = string.Empty;
        }
    }
}
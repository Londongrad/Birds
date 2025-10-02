using Birds.UI.Services.Navigation;
using Birds.UI.Views.Windows;
using MediatR;
using System.Windows;

namespace Birds.UI.Services.Notification
{
    public class NotificationService : INotificationService, INotificationHandler<NavigatedEvent>
    {
        private Window? _parent;

        /// <summary>
        /// Подключает сервис нотификаций к указанному окну.
        /// После вызова уведомления будут позиционироваться относительно этого окна.
        /// </summary>
        /// <param name="window">Окно, к которому нужно привязаться.</param>
        private void AttachWindow(Window window)
        {
            _parent = window ?? throw new ArgumentNullException(nameof(window));
        }

        public Task Handle(NavigatedEvent notification, CancellationToken ct)
        {
            // Когда происходит навигация — переподключаемся к новому окну
            AttachWindow(notification.Window);
            return Task.CompletedTask;
        }

        public void Show(string message, NotificationOptions? options)
        {
            if (_parent == null)
                throw new InvalidOperationException("Сервис нотификаций не привязан ни к одному окну.");

            var toast = new NotificationWindow(message, options ?? new NotificationOptions())
            {
                Owner = _parent,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // позиционирование в правом верхнем углу окна
            toast.Left = _parent.Left + _parent.Width - toast.Width - 10;
            toast.Top = _parent.Top + 10;

            toast.Show();
        }

        public void ShowError(string message) =>
            Show(message, new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(2)));

        public void ShowInfo(string message) =>
            Show(message, new NotificationOptions(NotificationType.Info, TimeSpan.FromSeconds(2)));

        public void ShowSuccess(string message) =>
            Show(message, new NotificationOptions(NotificationType.Success, TimeSpan.FromSeconds(2)));

        public void ShowWarning(string message) =>
            Show(message, new NotificationOptions(NotificationType.Warning, TimeSpan.FromSeconds(2)));
    }
}

using Birds.UI.Services.Navigation;
using Birds.UI.Services.Notification.Interfaces;
using MediatR;
using System.Windows;

namespace Birds.UI.Services.Notification
{
    public class NotificationService(INotificationManager notificationManager) : INotificationService, INotificationHandler<NavigatedEvent>
    {
        private Window? _parent;
        private readonly INotificationManager _notificationManager = notificationManager;

        /// <summary>
        /// Attaches the notification service to the specified window.
        /// After attachment, notifications will be positioned relative to this window.
        /// </summary>
        /// <param name="window">The window to attach to.</param>
        private void AttachWindow(Window window)
        {
            _parent = window ?? throw new ArgumentNullException(nameof(window));
        }

        public Task Handle(NavigatedEvent notification, CancellationToken ct)
        {
            // When navigation occurs — reattach to the newly opened window
            AttachWindow(notification.Window);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Show(string message, NotificationOptions? options)
        {
            if (_parent == null)
                throw new InvalidOperationException("The notification service is not attached to any window.");

            _notificationManager.ShowNotification(message, options ?? new NotificationOptions(), _parent);
        }

        /// <inheritdoc/>
        public void ShowError(string message) =>
            Show(message, new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowInfo(string message) =>
            Show(message, new NotificationOptions(NotificationType.Info, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowSuccess(string message) =>
            Show(message, new NotificationOptions(NotificationType.Success, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowWarning(string message) =>
            Show(message, new NotificationOptions(NotificationType.Warning, TimeSpan.FromSeconds(3)));
    }
}
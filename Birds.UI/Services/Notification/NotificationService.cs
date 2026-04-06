using Birds.UI.Services.Notification.Interfaces;

namespace Birds.UI.Services.Notification
{
    public class NotificationService(INotificationManager notificationManager) : INotificationService
    {
        private readonly INotificationManager _notificationManager = notificationManager;

        /// <inheritdoc/>
        public void Show(string message, NotificationOptions? options)
        {
            _notificationManager.ShowNotification(message, options ?? new NotificationOptions());
        }

        /// <inheritdoc/>
        public void ShowError(string message) =>
            Show(message, new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(6)));

        /// <inheritdoc/>
        public void ShowInfo(string message) =>
            Show(message, new NotificationOptions(NotificationType.Info, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowSuccess(string message) =>
            Show(message, new NotificationOptions(NotificationType.Success, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowWarning(string message) =>
            Show(message, new NotificationOptions(NotificationType.Warning, TimeSpan.FromSeconds(5)));
    }
}

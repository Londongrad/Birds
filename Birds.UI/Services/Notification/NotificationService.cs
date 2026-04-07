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
        public void ShowLocalized(string messageKey, NotificationOptions? options = null, params object[] args)
        {
            _notificationManager.ShowLocalizedNotification(messageKey, options ?? new NotificationOptions(), args);
        }

        /// <inheritdoc/>
        public void ShowError(string message) =>
            Show(message, new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(6)));

        /// <inheritdoc/>
        public void ShowErrorLocalized(string messageKey, params object[] args) =>
            ShowLocalized(messageKey, new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(6)), args);

        /// <inheritdoc/>
        public void ShowInfo(string message) =>
            Show(message, new NotificationOptions(NotificationType.Info, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowInfoLocalized(string messageKey, params object[] args) =>
            ShowLocalized(messageKey, new NotificationOptions(NotificationType.Info, TimeSpan.FromSeconds(3)), args);

        /// <inheritdoc/>
        public void ShowSuccess(string message) =>
            Show(message, new NotificationOptions(NotificationType.Success, TimeSpan.FromSeconds(3)));

        /// <inheritdoc/>
        public void ShowSuccessLocalized(string messageKey, params object[] args) =>
            ShowLocalized(messageKey, new NotificationOptions(NotificationType.Success, TimeSpan.FromSeconds(3)), args);

        /// <inheritdoc/>
        public void ShowWarning(string message) =>
            Show(message, new NotificationOptions(NotificationType.Warning, TimeSpan.FromSeconds(5)));

        /// <inheritdoc/>
        public void ShowWarningLocalized(string messageKey, params object[] args) =>
            ShowLocalized(messageKey, new NotificationOptions(NotificationType.Warning, TimeSpan.FromSeconds(5)), args);
    }
}

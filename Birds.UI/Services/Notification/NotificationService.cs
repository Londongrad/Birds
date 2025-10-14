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

            var toast = new NotificationWindow(message, options ?? new NotificationOptions())
            {
                Owner = _parent,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Position the toast in the top-right corner of the window
            toast.Left = _parent.Left + _parent.Width - toast.Width - 10;
            toast.Top = _parent.Top + 30;

            toast.Show();
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

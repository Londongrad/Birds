namespace Birds.UI.Services.Notification.Interfaces
{
    /// <summary>
    /// Service for displaying popup notifications (toasts) in the user interface.
    /// Provides a simple API for showing messages of different types with default parameters,
    /// as well as a universal <see cref="Show"/> method for custom configuration.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Displays a notification with the specified message and options.
        /// If <paramref name="options"/> is not provided, default values are used.
        /// </summary>
        /// <param name="message">The message text to display.</param>
        /// <param name="options">Additional notification options (type and duration).</param>
        void Show(string message, NotificationOptions? options = null);

        /// <summary>
        /// Displays a success notification.
        /// Uses predefined parameters (green theme, auto-close after 3 seconds).
        /// </summary>
        /// <param name="message">The message text to display.</param>
        void ShowSuccess(string message);

        /// <summary>
        /// Displays an error notification.
        /// Uses predefined parameters (red theme, auto-close after 3 seconds).
        /// </summary>
        /// <param name="message">The message text to display.</param>
        void ShowError(string message);

        /// <summary>
        /// Displays an informational notification.
        /// Uses predefined parameters (blue theme, auto-close after 3 seconds).
        /// </summary>
        /// <param name="message">The message text to display.</param>
        void ShowInfo(string message);

        /// <summary>
        /// Displays a warning notification.
        /// Uses predefined parameters (orange theme, auto-close after 3 seconds).
        /// </summary>
        /// <param name="message">The message text to display.</param>
        void ShowWarning(string message);
    }
}

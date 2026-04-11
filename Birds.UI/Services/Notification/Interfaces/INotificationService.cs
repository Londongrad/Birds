namespace Birds.UI.Services.Notification.Interfaces;

/// <summary>
///     Service for displaying notifications in the user interface.
/// </summary>
public interface INotificationService
{
    void Show(string message, NotificationOptions? options = null);

    void ShowLocalized(string messageKey, NotificationOptions? options = null, params object[] args);

    void ShowSuccess(string message);

    void ShowSuccessLocalized(string messageKey, params object[] args);

    void ShowError(string message);

    void ShowErrorLocalized(string messageKey, params object[] args);

    void ShowInfo(string message);

    void ShowInfoLocalized(string messageKey, params object[] args);

    void ShowWarning(string message);

    void ShowWarningLocalized(string messageKey, params object[] args);
}
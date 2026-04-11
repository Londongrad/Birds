using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birds.UI.Services.Notification.Interfaces;

public interface INotificationManager : INotifyPropertyChanged
{
    ReadOnlyObservableCollection<NotificationToast> ActiveNotifications { get; }

    int UnreadCount { get; }

    bool HasNotifications { get; }

    bool HasRecentOperationStatus { get; }

    NotificationType? RecentOperationStatusType { get; }

    void ShowNotification(string message, NotificationOptions options);

    void ShowLocalizedNotification(string messageKey, NotificationOptions options, params object[] args);

    void DismissNotification(NotificationToast notification);

    void ClearNotifications();

    void MarkAllAsRead();
}
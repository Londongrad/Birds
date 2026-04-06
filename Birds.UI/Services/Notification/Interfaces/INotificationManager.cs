using System.Collections.ObjectModel;
using System.ComponentModel;
using Birds.UI.Services.Notification;

namespace Birds.UI.Services.Notification.Interfaces
{
    public interface INotificationManager : INotifyPropertyChanged
    {
        ReadOnlyObservableCollection<NotificationToast> ActiveNotifications { get; }

        int UnreadCount { get; }

        bool HasNotifications { get; }

        void ShowNotification(string message, NotificationOptions options);

        void DismissNotification(NotificationToast notification);

        void ClearNotifications();

        void MarkAllAsRead();
    }
}

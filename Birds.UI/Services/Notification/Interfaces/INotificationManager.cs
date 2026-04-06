using System.Collections.ObjectModel;
using Birds.UI.Services.Notification;

namespace Birds.UI.Services.Notification.Interfaces
{
    public interface INotificationManager
    {
        ReadOnlyObservableCollection<NotificationToast> ActiveNotifications { get; }

        void ShowNotification(string message, NotificationOptions options);

        void DismissNotification(NotificationToast notification);
    }
}

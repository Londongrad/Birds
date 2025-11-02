using System.Windows;

namespace Birds.UI.Services.Notification.Interfaces
{
    public interface INotificationManager
    {
        void ShowNotification(string message, NotificationOptions options, Window parent);
    }
}
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Threading.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Notification
{
    public partial class NotificationManager : ObservableObject, INotificationManager
    {
        private const int MaxHistoryNotifications = 80;

        private readonly IUiDispatcher _uiDispatcher;
        private readonly ObservableCollection<NotificationToast> _activeNotifications = [];
        private readonly ReadOnlyObservableCollection<NotificationToast> _activeNotificationsView;

        [ObservableProperty]
        private int unreadCount;

        [ObservableProperty]
        private bool hasNotifications;

        public NotificationManager(IUiDispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            _activeNotificationsView = new ReadOnlyObservableCollection<NotificationToast>(_activeNotifications);
        }

        public ReadOnlyObservableCollection<NotificationToast> ActiveNotifications => _activeNotificationsView;

        public void ShowNotification(string message, NotificationOptions options)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _ = ShowInternalAsync(message.Trim(), options);
        }

        public void DismissNotification(NotificationToast notification)
        {
            if (notification is null)
                return;

            _ = _uiDispatcher.InvokeAsync(() => RemoveNotification(notification.Id));
        }

        public void ClearNotifications()
        {
            _ = _uiDispatcher.InvokeAsync(() =>
            {
                _activeNotifications.Clear();
                RefreshCounters();
            });
        }

        public void MarkAllAsRead()
        {
            _ = _uiDispatcher.InvokeAsync(() =>
            {
                foreach (var item in _activeNotifications.Where(x => !x.IsRead))
                    item.IsRead = true;

                RefreshCounters();
            });
        }

        private async Task ShowInternalAsync(string message, NotificationOptions options)
        {
            await _uiDispatcher.InvokeAsync(() =>
            {
                var title = NotificationToast.ResolveTitle(options.Title, options.Type);
                var existing = _activeNotifications.FirstOrDefault(x =>
                    x.Type == options.Type
                    && x.Title == title
                    && x.Message == message);

                if (existing is not null)
                    _activeNotifications.Remove(existing);

                _activeNotifications.Insert(0, NotificationToast.Create(message, options));

                while (_activeNotifications.Count > MaxHistoryNotifications)
                    _activeNotifications.RemoveAt(_activeNotifications.Count - 1);

                RefreshCounters();
            });
        }

        private void RemoveNotification(Guid id)
        {
            var notification = _activeNotifications.FirstOrDefault(x => x.Id == id);
            if (notification is null)
                return;

            _activeNotifications.Remove(notification);
            RefreshCounters();
        }

        private void RefreshCounters()
        {
            HasNotifications = _activeNotifications.Count > 0;
            UnreadCount = _activeNotifications.Count(x => !x.IsRead);
        }
    }
}

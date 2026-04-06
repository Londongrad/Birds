using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Threading.Abstractions;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Notification
{
    public sealed class NotificationManager : INotificationManager
    {
        private const int MaxVisibleNotifications = 4;

        private readonly IUiDispatcher _uiDispatcher;
        private readonly ObservableCollection<NotificationToast> _activeNotifications = [];
        private readonly ReadOnlyObservableCollection<NotificationToast> _activeNotificationsView;
        private readonly Dictionary<Guid, CancellationTokenSource> _lifetimes = [];

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

            _ = RemoveNotificationAsync(notification.Id);
        }

        private async Task ShowInternalAsync(string message, NotificationOptions options)
        {
            NotificationToast? toast = null;
            var effectiveTitle = NotificationToast.ResolveTitle(options.Title, options.Type);

            await _uiDispatcher.InvokeAsync(() =>
            {
                var existing = _activeNotifications.LastOrDefault(x =>
                    x.Type == options.Type
                    && x.Title == effectiveTitle
                    && x.Message == message);

                if (existing is not null)
                {
                    CancelLifetime(existing.Id);
                    _activeNotifications.Remove(existing);
                    _activeNotifications.Add(existing);
                    toast = existing;
                    return;
                }

                while (_activeNotifications.Count >= MaxVisibleNotifications)
                    RemoveNotificationCore(_activeNotifications[0].Id);

                toast = NotificationToast.Create(message, options);
                _activeNotifications.Add(toast);
            });

            if (toast is null)
                return;

            var duration = options.EffectiveDuration;
            if (duration == Timeout.InfiniteTimeSpan)
                return;

            var cts = new CancellationTokenSource();
            RegisterLifetime(toast.Id, cts);

            try
            {
                await Task.Delay(duration, cts.Token);
                await RemoveNotificationAsync(toast.Id);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task RemoveNotificationAsync(Guid id)
        {
            await _uiDispatcher.InvokeAsync(() => RemoveNotificationCore(id));
        }

        private void RemoveNotificationCore(Guid id)
        {
            CancelLifetime(id);

            var notification = _activeNotifications.FirstOrDefault(x => x.Id == id);
            if (notification is not null)
                _activeNotifications.Remove(notification);
        }

        private void RegisterLifetime(Guid id, CancellationTokenSource cts)
        {
            CancelLifetime(id);
            _lifetimes[id] = cts;
        }

        private void CancelLifetime(Guid id)
        {
            if (!_lifetimes.Remove(id, out var cts))
                return;

            cts.Cancel();
            cts.Dispose();
        }
    }
}

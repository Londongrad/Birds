using System.Collections.ObjectModel;
using Birds.UI.Services.Background;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Threading.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Notification;

public partial class NotificationManager : ObservableObject, INotificationManager
{
    private const int MaxHistoryNotifications = 80;
    private readonly ObservableCollection<NotificationToast> _activeNotifications = [];
    private readonly IBackgroundTaskRunner _backgroundTaskRunner;
    private readonly TimeSpan _recentOperationStatusDuration;

    private readonly IUiDispatcher _uiDispatcher;
    private CancellationTokenSource? _recentOperationStatusCts;

    [ObservableProperty] private bool hasNotifications;

    [ObservableProperty] private bool hasRecentOperationStatus;

    [ObservableProperty] private NotificationType? recentOperationStatusType;

    [ObservableProperty] private int unreadCount;

    public NotificationManager(
        IUiDispatcher uiDispatcher,
        IBackgroundTaskRunner backgroundTaskRunner,
        TimeSpan? recentOperationStatusDuration = null)
    {
        _uiDispatcher = uiDispatcher;
        _backgroundTaskRunner = backgroundTaskRunner;
        _recentOperationStatusDuration = recentOperationStatusDuration ?? TimeSpan.FromSeconds(3);
        ActiveNotifications = new ReadOnlyObservableCollection<NotificationToast>(_activeNotifications);
    }

    public ReadOnlyObservableCollection<NotificationToast> ActiveNotifications { get; }

    public void ShowNotification(string message, NotificationOptions options)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var normalizedMessage = message.Trim();
        _backgroundTaskRunner.Run(
            _ => ShowInternalAsync(
                options,
                toast => toast.Matches(normalizedMessage, options),
                () => NotificationToast.Create(normalizedMessage, options)),
            new BackgroundTaskOptions("Show notification"));
    }

    public void ShowLocalizedNotification(string messageKey, NotificationOptions options, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(messageKey))
            return;

        var normalizedKey = messageKey.Trim();
        var normalizedArgs = args?.ToArray() ?? Array.Empty<object>();

        _backgroundTaskRunner.Run(
            _ => ShowInternalAsync(
                options,
                toast => toast.MatchesLocalized(normalizedKey, options, normalizedArgs),
                () => NotificationToast.CreateLocalized(normalizedKey, options, normalizedArgs)),
            new BackgroundTaskOptions("Show localized notification"));
    }

    public void DismissNotification(NotificationToast notification)
    {
        if (notification is null)
            return;

        _backgroundTaskRunner.Run(
            _ => _uiDispatcher.InvokeAsync(() => RemoveNotification(notification.Id)),
            new BackgroundTaskOptions("Dismiss notification"));
    }

    public void ClearNotifications()
    {
        _backgroundTaskRunner.Run(
            _ => _uiDispatcher.InvokeAsync(() =>
            {
                _activeNotifications.Clear();
                RefreshCounters();
            }),
            new BackgroundTaskOptions("Clear notifications"));
    }

    public void MarkAllAsRead()
    {
        _backgroundTaskRunner.Run(
            _ => _uiDispatcher.InvokeAsync(() =>
            {
                foreach (var item in _activeNotifications.Where(x => !x.IsRead))
                    item.IsRead = true;

                RefreshCounters();
            }),
            new BackgroundTaskOptions("Mark notifications as read"));
    }

    private async Task ShowInternalAsync(
        NotificationOptions options,
        Func<NotificationToast, bool> matchPredicate,
        Func<NotificationToast> notificationFactory)
    {
        await _uiDispatcher.InvokeAsync(() =>
        {
            if (ShouldCoalesce(options))
            {
                var existing = _activeNotifications.FirstOrDefault(matchPredicate);
                if (existing is not null)
                    _activeNotifications.Remove(existing);
            }

            _activeNotifications.Insert(0, notificationFactory());

            while (_activeNotifications.Count > MaxHistoryNotifications)
                _activeNotifications.RemoveAt(_activeNotifications.Count - 1);

            RefreshCounters();
            ShowRecentOperationStatus(options.Type);
        });
    }

    private static bool ShouldCoalesce(NotificationOptions options)
    {
        return options.Type == NotificationType.Info;
    }

    private void ShowRecentOperationStatus(NotificationType type)
    {
        if (type is not NotificationType.Success and not NotificationType.Error)
            return;

        _recentOperationStatusCts?.Cancel();
        _recentOperationStatusCts?.Dispose();

        var cts = new CancellationTokenSource();
        _recentOperationStatusCts = cts;

        HasRecentOperationStatus = true;
        RecentOperationStatusType = type;

        _backgroundTaskRunner.Run(
            _ => HideRecentOperationStatusAsync(cts.Token),
            new BackgroundTaskOptions("Hide recent operation status"),
            cts.Token);
    }

    private async Task HideRecentOperationStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_recentOperationStatusDuration, cancellationToken);
            await _uiDispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                HasRecentOperationStatus = false;
                RecentOperationStatusType = null;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A new operation status replaced the current one.
        }
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

using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Notification;

public partial class NotificationToast : ObservableObject
{
    private readonly string? _customTitle;
    private readonly string? _message;
    private readonly object[] _messageArguments;
    private readonly string? _messageKey;

    [ObservableProperty] private bool isRead;

    private NotificationToast(
        Guid id,
        string? customTitle,
        string? message,
        string? messageKey,
        object[] messageArguments,
        NotificationType type,
        DateTimeOffset createdAt,
        bool isRead)
    {
        Id = id;
        _customTitle = customTitle;
        _message = message;
        _messageKey = messageKey;
        _messageArguments = messageArguments;
        Type = type;
        CreatedAt = createdAt;
        IsRead = isRead;

        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    public Guid Id { get; }

    public string Title => ResolveTitle(_customTitle, Type);

    public string Message =>
        _messageKey is not null
            ? AppText.Format(LocalizationService.Instance.CurrentCulture, _messageKey, _messageArguments)
            : _message ?? string.Empty;

    public NotificationType Type { get; }

    public DateTimeOffset CreatedAt { get; }

    public string TypeLabel => ResolveTypeLabel(Type);

    public static NotificationToast Create(string message, NotificationOptions options)
    {
        return new NotificationToast(
            Guid.NewGuid(),
            options.Title,
            message,
            null,
            Array.Empty<object>(),
            options.Type,
            DateTimeOffset.Now,
            false);
    }

    public static NotificationToast CreateLocalized(string messageKey, NotificationOptions options,
        params object[] args)
    {
        return new NotificationToast(
            Guid.NewGuid(),
            options.Title,
            null,
            messageKey,
            args?.ToArray() ?? Array.Empty<object>(),
            options.Type,
            DateTimeOffset.Now,
            false);
    }

    public bool Matches(string message, NotificationOptions options)
    {
        return _messageKey is null
               && Type == options.Type
               && Title == ResolveTitle(options.Title, options.Type)
               && string.Equals(_message, message, StringComparison.Ordinal);
    }

    public bool MatchesLocalized(string messageKey, NotificationOptions options, params object[] args)
    {
        return string.Equals(_messageKey, messageKey, StringComparison.Ordinal)
               && Type == options.Type
               && Title == ResolveTitle(options.Title, options.Type)
               && _messageArguments.SequenceEqual(args ?? Array.Empty<object>());
    }

    public static string ResolveTitle(string? title, NotificationType type)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        return type switch
        {
            NotificationType.Success => AppText.Get("Notification.Title.Success"),
            NotificationType.Error => AppText.Get("Notification.Title.Error"),
            NotificationType.Warning => AppText.Get("Notification.Title.Warning"),
            _ => AppText.Get("Notification.Title.Info")
        };
    }

    public static string ResolveTypeLabel(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => AppText.Get("Notification.Type.Success"),
            NotificationType.Error => AppText.Get("Notification.Type.Error"),
            NotificationType.Warning => AppText.Get("Notification.Type.Warning"),
            _ => AppText.Get("Notification.Type.Info")
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(TypeLabel));
    }
}
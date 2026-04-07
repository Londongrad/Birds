using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Notification
{
    public partial class NotificationToast : ObservableObject
    {
        private readonly string? _customTitle;

        public NotificationToast(Guid id,
                                 string? customTitle,
                                 string message,
                                 NotificationType type,
                                 DateTimeOffset createdAt,
                                 bool isRead)
        {
            Id = id;
            _customTitle = customTitle;
            Message = message;
            Type = type;
            CreatedAt = createdAt;
            IsRead = isRead;

            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        public Guid Id { get; }

        public string Title => ResolveTitle(_customTitle, Type);

        public string Message { get; }

        public NotificationType Type { get; }

        public DateTimeOffset CreatedAt { get; }

        public string TypeLabel => ResolveTypeLabel(Type);

        [ObservableProperty]
        private bool isRead;

        public static NotificationToast Create(string message, NotificationOptions options)
        {
            return new NotificationToast(
                Guid.NewGuid(),
                options.Title,
                message,
                options.Type,
                DateTimeOffset.Now,
                isRead: false);
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
            => type switch
            {
                NotificationType.Success => AppText.Get("Notification.Type.Success"),
                NotificationType.Error => AppText.Get("Notification.Type.Error"),
                NotificationType.Warning => AppText.Get("Notification.Type.Warning"),
                _ => AppText.Get("Notification.Type.Info")
            };

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(TypeLabel));
        }
    }
}

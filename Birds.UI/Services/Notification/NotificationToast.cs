using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Notification
{
    public partial class NotificationToast : ObservableObject
    {
        public NotificationToast(Guid id,
                                 string title,
                                 string message,
                                 NotificationType type,
                                 DateTimeOffset createdAt,
                                 bool isRead)
        {
            Id = id;
            Title = title;
            Message = message;
            Type = type;
            CreatedAt = createdAt;
            IsRead = isRead;
        }

        public Guid Id { get; }

        public string Title { get; }

        public string Message { get; }

        public NotificationType Type { get; }

        public DateTimeOffset CreatedAt { get; }

        [ObservableProperty]
        private bool isRead;

        public static NotificationToast Create(string message, NotificationOptions options)
        {
            return new NotificationToast(
                Guid.NewGuid(),
                ResolveTitle(options.Title, options.Type),
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
                NotificationType.Success => "Успешно",
                NotificationType.Error => "Ошибка",
                NotificationType.Warning => "Внимание",
                _ => "Информация"
            };
        }
    }
}

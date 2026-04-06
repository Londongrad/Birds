namespace Birds.UI.Services.Notification
{
    public sealed class NotificationToast(Guid id, string title, string message, NotificationType type)
    {
        public Guid Id { get; } = id;

        public string Title { get; } = title;

        public string Message { get; } = message;

        public NotificationType Type { get; } = type;

        public static NotificationToast Create(string message, NotificationOptions options)
        {
            return new NotificationToast(
                Guid.NewGuid(),
                ResolveTitle(options.Title, options.Type),
                message,
                options.Type);
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

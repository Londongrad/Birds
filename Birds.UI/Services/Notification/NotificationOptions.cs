namespace Birds.UI.Services.Notification
{
    /// <summary>
    /// Опции отображения уведомления.
    /// Используются для настройки длительности, наличия кнопки закрытия, заголовка и типа уведомления.
    /// </summary>
    /// <param name="Type">Тип уведомления (успех, ошибка, предупреждение, информация).</param>
    /// <param name="Duration">Длительность отображения уведомления.</param>
    /// <param name="ShowCloseButton">Признак, показывать ли кнопку закрытия.</param>
    /// <param name="Title">Необязательный заголовок уведомления.</param>
    public record NotificationOptions(
        NotificationType Type = NotificationType.Info,
        TimeSpan Duration = default,
        bool ShowCloseButton = true,
        string? Title = null
    )
    {
        /// <summary>
        /// Длительность по умолчанию (2 секунды), если в параметре <see cref="Duration"/> передано значение по умолчанию.
        /// </summary>
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Возвращает реальную длительность (если не задана, используется <see cref="DefaultDuration"/>).
        /// </summary>
        public TimeSpan EffectiveDuration => Duration == default ? DefaultDuration : Duration;
    }
}

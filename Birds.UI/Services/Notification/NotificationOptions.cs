namespace Birds.UI.Services.Notification
{
    /// <summary>
    /// Notification display options.
    /// Used to configure the duration, presence of a close button, title, and notification type.
    /// </summary>
    /// <param name="Type">The type of notification (success, error, warning, information).</param>
    /// <param name="Duration">The duration of the notification display.</param>
    /// <param name="Title">An optional notification title.</param>
    public record NotificationOptions(
        NotificationType Type = NotificationType.Info,
        TimeSpan Duration = default,
        string? Title = null
    )
    {
        /// <summary>
        /// The default duration (2 seconds), used if <see cref="Duration"/> is set to its default value.
        /// </summary>
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Returns the effective duration (uses <see cref="DefaultDuration"/> if not explicitly set).
        /// </summary>
        public TimeSpan EffectiveDuration => Duration == default ? DefaultDuration : Duration;
    }
}
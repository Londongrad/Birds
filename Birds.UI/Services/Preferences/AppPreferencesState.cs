namespace Birds.UI.Services.Preferences
{
    public sealed record AppPreferencesState
    {
        public const string DefaultLanguage = "Русский";

        public string SelectedLanguage { get; init; } = DefaultLanguage;

        public bool ShowNotificationBadge { get; init; } = true;

        public bool ReduceMotion { get; init; }
    }
}

namespace Birds.UI.Services.Preferences
{
    public sealed record AppPreferencesState
    {
        public const string DefaultLanguage = "Русский";
        public const string DefaultTheme = "Графит";

        public string SelectedLanguage { get; init; } = DefaultLanguage;

        public string SelectedTheme { get; init; } = DefaultTheme;

        public bool ShowNotificationBadge { get; init; } = true;

        public bool ReduceMotion { get; init; }
    }
}

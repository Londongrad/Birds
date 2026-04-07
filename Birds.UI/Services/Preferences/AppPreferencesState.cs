using Birds.Shared.Localization;
using Birds.UI.Services.Theming;

namespace Birds.UI.Services.Preferences
{
    public sealed record AppPreferencesState
    {
        public const string DefaultLanguage = AppLanguages.Russian;
        public const string DefaultTheme = ThemeKeys.Graphite;

        public string SelectedLanguage { get; init; } = DefaultLanguage;

        public string SelectedTheme { get; init; } = DefaultTheme;

        public bool ShowNotificationBadge { get; init; } = true;

        public bool ReduceMotion { get; init; }
    }
}

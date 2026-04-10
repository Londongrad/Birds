using Birds.Shared.Localization;
using Birds.UI.Services.Theming;

namespace Birds.UI.Services.Preferences
{
    public sealed record AppPreferencesState
    {
        public const string DefaultLanguage = AppLanguages.Russian;
        public const string DefaultTheme = ThemeKeys.Graphite;
        public const string DefaultDateFormat = Localization.DateDisplayFormats.Default;
        public const string DefaultImportMode = Import.BirdImportModes.Merge;

        public string SelectedLanguage { get; init; } = DefaultLanguage;

        public string SelectedTheme { get; init; } = DefaultTheme;

        public string SelectedDateFormat { get; init; } = DefaultDateFormat;

        public string SelectedImportMode { get; init; } = DefaultImportMode;

        public string CustomExportPath { get; init; } = string.Empty;

        public bool ShowNotificationBadge { get; init; } = true;

        public bool ReduceMotion { get; init; }
    }
}

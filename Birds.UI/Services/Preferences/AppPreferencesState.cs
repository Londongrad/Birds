using Birds.Shared.Localization;
using Birds.Shared.Sync;
using Birds.UI.Services.Import;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Theming;

namespace Birds.UI.Services.Preferences;

public sealed record AppPreferencesState
{
    public const string DefaultLanguage = AppLanguages.Russian;
    public const string DefaultTheme = ThemeKeys.Graphite;
    public const string DefaultDateFormat = DateDisplayFormats.Default;
    public const string DefaultImportMode = BirdImportModes.Merge;
    public const string DefaultSyncInterval = RemoteSyncIntervalPresets.Default;
    public const bool DefaultAutoExportEnabled = true;
    public const bool DefaultShowSyncStatusIndicator = true;

    public string SelectedLanguage { get; init; } = DefaultLanguage;

    public string SelectedTheme { get; init; } = DefaultTheme;

    public string SelectedDateFormat { get; init; } = DefaultDateFormat;

    public string SelectedImportMode { get; init; } = DefaultImportMode;

    public string SelectedSyncInterval { get; init; } = DefaultSyncInterval;

    public string CustomExportPath { get; init; } = string.Empty;

    public bool AutoExportEnabled { get; init; } = DefaultAutoExportEnabled;

    public bool ShowNotificationBadge { get; init; } = true;

    public bool ShowSyncStatusIndicator { get; init; } = DefaultShowSyncStatusIndicator;
}

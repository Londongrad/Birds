using Birds.Shared.Localization;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Import;
using Birds.UI.Services.Theming;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Birds.UI.Services.Preferences
{
    public sealed partial class JsonAppPreferencesService : ObservableObject, IAppPreferencesService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IAppPreferencesPathProvider _pathProvider;

        [ObservableProperty]
        private string selectedLanguage = AppPreferencesState.DefaultLanguage;

        [ObservableProperty]
        private string selectedTheme = AppPreferencesState.DefaultTheme;

        [ObservableProperty]
        private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

        [ObservableProperty]
        private string selectedImportMode = AppPreferencesState.DefaultImportMode;

        [ObservableProperty]
        private string customExportPath = string.Empty;

        [ObservableProperty]
        private bool autoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;

        [ObservableProperty]
        private bool showNotificationBadge = true;

        [ObservableProperty]
        private bool showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

        public JsonAppPreferencesService(IAppPreferencesPathProvider pathProvider)
        {
            _pathProvider = pathProvider;

            var state = LoadState();
            selectedLanguage = AppLanguages.Normalize(state.SelectedLanguage);
            selectedTheme = ThemeKeys.Normalize(state.SelectedTheme);
            selectedDateFormat = DateDisplayFormats.Normalize(state.SelectedDateFormat);
            selectedImportMode = BirdImportModes.Normalize(state.SelectedImportMode);
            customExportPath = state.CustomExportPath?.Trim() ?? string.Empty;
            autoExportEnabled = state.AutoExportEnabled;
            showNotificationBadge = state.ShowNotificationBadge;
            showSyncStatusIndicator = state.ShowSyncStatusIndicator;
        }

        public void ResetToDefaults()
        {
            SelectedLanguage = AppPreferencesState.DefaultLanguage;
            SelectedTheme = AppPreferencesState.DefaultTheme;
            SelectedDateFormat = AppPreferencesState.DefaultDateFormat;
            SelectedImportMode = AppPreferencesState.DefaultImportMode;
            CustomExportPath = string.Empty;
            AutoExportEnabled = AppPreferencesState.DefaultAutoExportEnabled;
            ShowNotificationBadge = true;
            ShowSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;
        }

        partial void OnSelectedLanguageChanged(string value) => SaveState();

        partial void OnSelectedThemeChanged(string value)
        {
            selectedTheme = ThemeKeys.Normalize(value);
            SaveState();
        }

        partial void OnSelectedDateFormatChanged(string value)
        {
            selectedDateFormat = DateDisplayFormats.Normalize(value);
            SaveState();
        }

        partial void OnSelectedImportModeChanged(string value)
        {
            selectedImportMode = BirdImportModes.Normalize(value);
            SaveState();
        }

        partial void OnCustomExportPathChanged(string value)
        {
            customExportPath = value?.Trim() ?? string.Empty;
            SaveState();
        }

        partial void OnAutoExportEnabledChanged(bool value) => SaveState();

        partial void OnShowNotificationBadgeChanged(bool value) => SaveState();

        partial void OnShowSyncStatusIndicatorChanged(bool value) => SaveState();

        private AppPreferencesState LoadState()
        {
            try
            {
                var path = _pathProvider.GetPreferencesPath();
                if (!File.Exists(path))
                    return new AppPreferencesState();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppPreferencesState>(json, JsonOptions) ?? new AppPreferencesState();
            }
            catch
            {
                return new AppPreferencesState();
            }
        }

        private void SaveState()
        {
            try
            {
                var path = _pathProvider.GetPreferencesPath();
                var directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(
                    new AppPreferencesState
                    {
                        SelectedLanguage = SelectedLanguage,
                        SelectedTheme = ThemeKeys.Normalize(SelectedTheme),
                        SelectedDateFormat = DateDisplayFormats.Normalize(SelectedDateFormat),
                        SelectedImportMode = BirdImportModes.Normalize(SelectedImportMode),
                        CustomExportPath = CustomExportPath?.Trim() ?? string.Empty,
                        AutoExportEnabled = AutoExportEnabled,
                        ShowNotificationBadge = ShowNotificationBadge,
                        ShowSyncStatusIndicator = ShowSyncStatusIndicator
                    },
                    JsonOptions);

                File.WriteAllText(path, json);
            }
            catch
            {
                // User preferences should never crash the UI.
            }
        }
    }
}

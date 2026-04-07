using Birds.UI.Services.Preferences.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Birds.UI.Services.Preferences
{
    public sealed partial class JsonAppPreferencesService : ObservableObject, IAppPreferencesService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly IAppPreferencesPathProvider _pathProvider;

        [ObservableProperty]
        private string selectedLanguage = AppPreferencesState.DefaultLanguage;

        [ObservableProperty]
        private string selectedTheme = AppPreferencesState.DefaultTheme;

        [ObservableProperty]
        private bool showNotificationBadge = true;

        [ObservableProperty]
        private bool reduceMotion;

        public JsonAppPreferencesService(IAppPreferencesPathProvider pathProvider)
        {
            _pathProvider = pathProvider;

            var state = LoadState();
            selectedLanguage = state.SelectedLanguage;
            selectedTheme = state.SelectedTheme;
            showNotificationBadge = state.ShowNotificationBadge;
            reduceMotion = state.ReduceMotion;
        }

        public void ResetToDefaults()
        {
            SelectedLanguage = AppPreferencesState.DefaultLanguage;
            SelectedTheme = AppPreferencesState.DefaultTheme;
            ShowNotificationBadge = true;
            ReduceMotion = false;
        }

        partial void OnSelectedLanguageChanged(string value) => SaveState();

        partial void OnSelectedThemeChanged(string value) => SaveState();

        partial void OnShowNotificationBadgeChanged(bool value) => SaveState();

        partial void OnReduceMotionChanged(bool value) => SaveState();

        private AppPreferencesState LoadState()
        {
            try
            {
                var path = _pathProvider.GetPreferencesPath();
                if (!File.Exists(path))
                    return new AppPreferencesState();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppPreferencesState>(json) ?? new AppPreferencesState();
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
                        SelectedTheme = SelectedTheme,
                        ShowNotificationBadge = ShowNotificationBadge,
                        ReduceMotion = ReduceMotion
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

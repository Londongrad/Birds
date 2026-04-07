using Birds.Shared.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Preferences;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Birds.UI.Services.Localization
{
    public sealed class LocalizationService : ObservableObject, ILocalizationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static LocalizationService Instance { get; } = new();

        private LocalizationService()
        {
            SupportedLanguages = new ReadOnlyCollection<string>(AppLanguages.SupportedLanguages.ToArray());
            ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian));
        }

        public ReadOnlyCollection<string> SupportedLanguages { get; }

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

        public string CurrentLanguage => CurrentCulture.Name;

        public string this[string key] => AppText.Get(key, CurrentCulture);

        public event EventHandler? LanguageChanged;

        public string GetString(string key) => AppText.Get(key, CurrentCulture);

        public string GetString(string key, params object[] args)
            => AppText.Format(CurrentCulture, key, args);

        public bool ApplyLanguage(string? language)
        {
            var culture = AppLanguages.ToCulture(language);
            if (CurrentCulture.Name == culture.Name)
                return false;

            ApplyCulture(culture);
            return true;
        }

        public void LoadSavedLanguage()
        {
            try
            {
                var path = new LocalAppPreferencesPathProvider().GetPreferencesPath();
                if (!File.Exists(path))
                {
                    ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian));
                    return;
                }

                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<AppPreferencesState>(json, JsonOptions);
                ApplyCulture(AppLanguages.ToCulture(state?.SelectedLanguage));
            }
            catch
            {
                ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian));
            }
        }

        private void ApplyCulture(CultureInfo culture)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CurrentCulture = culture;

            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged("Item[]");
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

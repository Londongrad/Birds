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
            ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian), DateDisplayFormats.Default);
        }

        public ReadOnlyCollection<string> SupportedLanguages { get; }

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

        public string CurrentLanguage => CurrentCulture.Name;

        public string CurrentDateFormat { get; private set; } = DateDisplayFormats.Default;

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

            ApplyCulture(culture, CurrentDateFormat);
            return true;
        }

        public bool ApplyDateFormat(string? dateFormat)
        {
            var normalized = DateDisplayFormats.Normalize(dateFormat);
            if (CurrentDateFormat == normalized)
                return false;

            ApplyCulture(AppLanguages.ToCulture(CurrentLanguage), normalized);
            return true;
        }

        public string FormatDate(DateOnly value, DateDisplayStyle style = DateDisplayStyle.Short)
            => DateDisplayFormats.FormatDate(value, CurrentCulture, CurrentDateFormat, style);

        public string FormatDate(DateOnly? value, DateDisplayStyle style = DateDisplayStyle.Short, string? fallback = null)
            => value.HasValue
                ? FormatDate(value.Value, style)
                : fallback ?? "\u2014";

        public string FormatDateTime(DateTime value)
            => DateDisplayFormats.FormatDateTime(value, CurrentCulture, CurrentDateFormat);

        public string FormatDateTime(DateTime? value, string? fallback = null)
            => value.HasValue
                ? FormatDateTime(value.Value)
                : fallback ?? "\u2014";

        public void LoadSavedLanguage()
        {
            try
            {
                var path = new LocalAppPreferencesPathProvider().GetPreferencesPath();
                if (!File.Exists(path))
                {
                    ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian), DateDisplayFormats.Default);
                    return;
                }

                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<AppPreferencesState>(json, JsonOptions);
                ApplyCulture(
                    AppLanguages.ToCulture(state?.SelectedLanguage),
                    DateDisplayFormats.Normalize(state?.SelectedDateFormat));
            }
            catch
            {
                ApplyCulture(AppLanguages.ToCulture(AppLanguages.Russian), DateDisplayFormats.Default);
            }
        }

        private void ApplyCulture(CultureInfo culture, string dateFormat)
        {
            var customizedCulture = (CultureInfo)culture.Clone();
            DateDisplayFormats.ApplyToCulture(customizedCulture, dateFormat);

            CultureInfo.DefaultThreadCurrentCulture = customizedCulture;
            CultureInfo.DefaultThreadCurrentUICulture = customizedCulture;
            CultureInfo.CurrentCulture = customizedCulture;
            CultureInfo.CurrentUICulture = customizedCulture;
            CurrentCulture = customizedCulture;
            CurrentDateFormat = DateDisplayFormats.Normalize(dateFormat);

            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged(nameof(CurrentDateFormat));
            OnPropertyChanged("Item[]");
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

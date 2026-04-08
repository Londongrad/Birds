using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birds.UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IAppPreferencesService _preferences;
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localization;
        private readonly IBirdManager _birdManager;

        private ReadOnlyCollection<LanguageOption> _availableLanguages =
            new(new List<LanguageOption>());

        private ReadOnlyCollection<ThemeOption> _availableThemes =
            new(new List<ThemeOption>());

        private ReadOnlyCollection<DateFormatOption> _availableDateFormats =
            new(new List<DateFormatOption>());

        public SettingsViewModel(IAppPreferencesService preferences,
                                 IThemeService themeService,
                                 ILocalizationService localization,
                                 IBirdManager birdManager)
        {
            _preferences = preferences;
            _themeService = themeService;
            _localization = localization;
            _birdManager = birdManager;

            BuildAvailableLanguages();
            BuildAvailableThemes();
            BuildAvailableDateFormats();
            ReloadFromPreferences();

            _preferences.PropertyChanged += OnPreferencesChanged;
            _localization.LanguageChanged += OnLanguageChanged;
        }

        public ReadOnlyCollection<LanguageOption> AvailableLanguages
        {
            get => _availableLanguages;
            private set => SetProperty(ref _availableLanguages, value);
        }

        public ReadOnlyCollection<ThemeOption> AvailableThemes
        {
            get => _availableThemes;
            private set => SetProperty(ref _availableThemes, value);
        }

        public ReadOnlyCollection<DateFormatOption> AvailableDateFormats
        {
            get => _availableDateFormats;
            private set => SetProperty(ref _availableDateFormats, value);
        }

        [ObservableProperty]
        private string selectedLanguage = AppPreferencesState.DefaultLanguage;

        [ObservableProperty]
        private string selectedTheme = AppPreferencesState.DefaultTheme;

        [ObservableProperty]
        private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

        [ObservableProperty]
        private bool showNotificationBadge = true;

        [ObservableProperty]
        private bool reduceMotion;

        public string LanguageHint =>
            SelectedLanguage == AppLanguages.Russian
                ? AppText.Get("Settings.LanguageHint.Russian")
                : AppText.Get("Settings.LanguageHint.English");

        public string ThemeHint =>
            SelectedTheme == ThemeKeys.Graphite
                ? AppText.Get("Settings.ThemeHint.Graphite")
                : AppText.Get("Settings.ThemeHint.Steel");

        public string DateFormatHint =>
            SelectedDateFormat switch
            {
                DateDisplayFormats.MonthDayYear => AppText.Get("Settings.DateFormatHint.MonthDayYear"),
                DateDisplayFormats.YearMonthDay => AppText.Get("Settings.DateFormatHint.YearMonthDay"),
                _ => AppText.Get("Settings.DateFormatHint.DayMonthYear")
            };

        public string NotificationsHint =>
            ShowNotificationBadge
                ? AppText.Get("Settings.NotificationsHint.Enabled")
                : AppText.Get("Settings.NotificationsHint.Disabled");

        public string MotionHint =>
            ReduceMotion
                ? AppText.Get("Settings.MotionHint.Enabled")
                : AppText.Get("Settings.MotionHint.Disabled");

        [RelayCommand]
        private void ResetPreferences()
        {
            _preferences.ResetToDefaults();
            ReloadFromPreferences();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            var normalized = AppLanguages.Normalize(value);
            if (_preferences.SelectedLanguage != normalized)
                _preferences.SelectedLanguage = normalized;

            if (_localization.ApplyLanguage(normalized))
                _ = _birdManager.ReloadAsync(CancellationToken.None);

            OnPropertyChanged(nameof(LanguageHint));
        }

        partial void OnSelectedThemeChanged(string value)
        {
            var normalized = ThemeKeys.Normalize(value);
            if (_preferences.SelectedTheme != normalized)
                _preferences.SelectedTheme = normalized;

            _themeService.ApplyTheme(normalized);
            OnPropertyChanged(nameof(ThemeHint));
        }

        partial void OnSelectedDateFormatChanged(string value)
        {
            var normalized = DateDisplayFormats.Normalize(value);
            if (_preferences.SelectedDateFormat != normalized)
                _preferences.SelectedDateFormat = normalized;

            _localization.ApplyDateFormat(normalized);
            OnPropertyChanged(nameof(DateFormatHint));
        }

        partial void OnShowNotificationBadgeChanged(bool value)
        {
            if (_preferences.ShowNotificationBadge != value)
                _preferences.ShowNotificationBadge = value;

            OnPropertyChanged(nameof(NotificationsHint));
        }

        partial void OnReduceMotionChanged(bool value)
        {
            if (_preferences.ReduceMotion != value)
                _preferences.ReduceMotion = value;

            OnPropertyChanged(nameof(MotionHint));
        }

        private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IAppPreferencesService.SelectedLanguage)
                or nameof(IAppPreferencesService.SelectedTheme)
                or nameof(IAppPreferencesService.SelectedDateFormat)
                or nameof(IAppPreferencesService.ShowNotificationBadge)
                or nameof(IAppPreferencesService.ReduceMotion))
            {
                ReloadFromPreferences();
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            BuildAvailableLanguages();
            BuildAvailableThemes();
            BuildAvailableDateFormats();
            OnPropertyChanged(nameof(LanguageHint));
            OnPropertyChanged(nameof(ThemeHint));
            OnPropertyChanged(nameof(DateFormatHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
        }

        private void BuildAvailableLanguages()
        {
            AvailableLanguages = new ReadOnlyCollection<LanguageOption>(
                new List<LanguageOption>
                {
                    new(AppLanguages.Russian, AppText.Get("Language.Russian")),
                    new(AppLanguages.English, AppText.Get("Language.English"))
                });
        }

        private void BuildAvailableThemes()
        {
            AvailableThemes = new ReadOnlyCollection<ThemeOption>(
                _themeService.AvailableThemes
                    .Select(theme => new ThemeOption(theme, AppText.Get($"Settings.Theme.{theme}")))
                    .ToList());
        }

        private void BuildAvailableDateFormats()
        {
            AvailableDateFormats = new ReadOnlyCollection<DateFormatOption>(
                new List<DateFormatOption>
                {
                    new(DateDisplayFormats.DayMonthYear, AppText.Get("Settings.DateFormat.DayMonthYear")),
                    new(DateDisplayFormats.MonthDayYear, AppText.Get("Settings.DateFormat.MonthDayYear")),
                    new(DateDisplayFormats.YearMonthDay, AppText.Get("Settings.DateFormat.YearMonthDay"))
                });
        }

        private void ReloadFromPreferences()
        {
            SelectedLanguage = AppLanguages.Normalize(_preferences.SelectedLanguage);
            SelectedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);
            SelectedDateFormat = DateDisplayFormats.Normalize(_preferences.SelectedDateFormat);
            ShowNotificationBadge = _preferences.ShowNotificationBadge;
            ReduceMotion = _preferences.ReduceMotion;

            OnPropertyChanged(nameof(LanguageHint));
            OnPropertyChanged(nameof(ThemeHint));
            OnPropertyChanged(nameof(DateFormatHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
        }
    }
}

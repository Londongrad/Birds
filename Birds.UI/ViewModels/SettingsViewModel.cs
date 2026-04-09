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
        private bool _isSynchronizingSelections;

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
                ? _localization.GetString("Settings.LanguageHint.Russian")
                : _localization.GetString("Settings.LanguageHint.English");

        public string ThemeHint =>
            SelectedTheme == ThemeKeys.Graphite
                ? _localization.GetString("Settings.ThemeHint.Graphite")
                : _localization.GetString("Settings.ThemeHint.Steel");

        public string DateFormatHint =>
            SelectedDateFormat switch
            {
                DateDisplayFormats.MonthDayYear => _localization.GetString("Settings.DateFormatHint.MonthDayYear"),
                DateDisplayFormats.YearMonthDay => _localization.GetString("Settings.DateFormatHint.YearMonthDay"),
                _ => _localization.GetString("Settings.DateFormatHint.DayMonthYear")
            };

        public string NotificationsHint =>
            ShowNotificationBadge
                ? _localization.GetString("Settings.NotificationsHint.Enabled")
                : _localization.GetString("Settings.NotificationsHint.Disabled");

        public string MotionHint =>
            ReduceMotion
                ? _localization.GetString("Settings.MotionHint.Enabled")
                : _localization.GetString("Settings.MotionHint.Disabled");

        [RelayCommand]
        private void ResetPreferences()
        {
            _preferences.ResetToDefaults();
            ReloadFromPreferences();
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            var normalized = AppLanguages.Normalize(value);

            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(LanguageHint));
                return;
            }

            if (_preferences.SelectedLanguage != normalized)
                _preferences.SelectedLanguage = normalized;

            if (_localization.ApplyLanguage(normalized))
                _ = _birdManager.ReloadAsync(CancellationToken.None);

            OnPropertyChanged(nameof(LanguageHint));
        }

        partial void OnSelectedThemeChanged(string value)
        {
            var normalized = ThemeKeys.Normalize(value);

            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(ThemeHint));
                return;
            }

            if (_preferences.SelectedTheme != normalized)
                _preferences.SelectedTheme = normalized;

            _themeService.ApplyTheme(normalized);
            OnPropertyChanged(nameof(ThemeHint));
        }

        partial void OnSelectedDateFormatChanged(string value)
        {
            var normalized = DateDisplayFormats.Normalize(value);

            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(DateFormatHint));
                return;
            }

            if (_preferences.SelectedDateFormat != normalized)
                _preferences.SelectedDateFormat = normalized;

            _localization.ApplyDateFormat(normalized);
            OnPropertyChanged(nameof(DateFormatHint));
        }

        partial void OnShowNotificationBadgeChanged(bool value)
        {
            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(NotificationsHint));
                return;
            }

            if (_preferences.ShowNotificationBadge != value)
                _preferences.ShowNotificationBadge = value;

            OnPropertyChanged(nameof(NotificationsHint));
        }

        partial void OnReduceMotionChanged(bool value)
        {
            if (_isSynchronizingSelections)
            {
                OnPropertyChanged(nameof(MotionHint));
                return;
            }

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
                ReloadFromPreferences(reapplyTheme: true);
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            var preservedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);

            BuildAvailableLanguages();
            BuildAvailableThemes();
            BuildAvailableDateFormats();
            ReloadFromPreferences(reapplyTheme: true);
            _themeService.ApplyTheme(preservedTheme);
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
                    new(AppLanguages.Russian, _localization.GetString("Language.Russian")),
                    new(AppLanguages.English, _localization.GetString("Language.English"))
                });
        }

        private void BuildAvailableThemes()
        {
            AvailableThemes = new ReadOnlyCollection<ThemeOption>(
                _themeService.AvailableThemes
                    .Select(theme => new ThemeOption(theme, _localization.GetString($"Settings.Theme.{theme}")))
                    .ToList());
        }

        private void BuildAvailableDateFormats()
        {
            AvailableDateFormats = new ReadOnlyCollection<DateFormatOption>(
                new List<DateFormatOption>
                {
                    new(DateDisplayFormats.DayMonthYear, _localization.GetString("Settings.DateFormat.DayMonthYear")),
                    new(DateDisplayFormats.MonthDayYear, _localization.GetString("Settings.DateFormat.MonthDayYear")),
                    new(DateDisplayFormats.YearMonthDay, _localization.GetString("Settings.DateFormat.YearMonthDay"))
                });
        }

        private void ReloadFromPreferences(bool reapplyTheme = false)
        {
            var normalizedLanguage = AppLanguages.Normalize(_preferences.SelectedLanguage);
            var normalizedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);
            var normalizedDateFormat = DateDisplayFormats.Normalize(_preferences.SelectedDateFormat);

            _isSynchronizingSelections = true;
            try
            {
                SelectedLanguage = normalizedLanguage;
                SelectedTheme = normalizedTheme;
                SelectedDateFormat = normalizedDateFormat;
                ShowNotificationBadge = _preferences.ShowNotificationBadge;
                ReduceMotion = _preferences.ReduceMotion;
            }
            finally
            {
                _isSynchronizingSelections = false;
            }

            if (reapplyTheme)
                _themeService.ApplyTheme(normalizedTheme);

            OnPropertyChanged(nameof(LanguageHint));
            OnPropertyChanged(nameof(ThemeHint));
            OnPropertyChanged(nameof(DateFormatHint));
            OnPropertyChanged(nameof(NotificationsHint));
            OnPropertyChanged(nameof(MotionHint));
        }
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using Birds.Shared.Localization;
using Birds.UI.Services.Background;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.ViewModels;

public partial class AppearanceSettingsViewModel : ObservableObject, IDisposable
{
    private readonly IBirdManager _birdManager;
    private readonly IBackgroundTaskRunner _backgroundTaskRunner;
    private readonly ILocalizationService _localization;
    private readonly IAppPreferencesService _preferences;
    private readonly IThemeService _themeService;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _disposed;
    private bool _isSynchronizingSelections;
    private CancellationTokenSource? _languageReloadCancellation;

    private ReadOnlyCollection<DateFormatOption> _availableDateFormats =
        new(new List<DateFormatOption>());

    private ReadOnlyCollection<LanguageOption> _availableLanguages =
        new(new List<LanguageOption>());

    private ReadOnlyCollection<ThemeOption> _availableThemes =
        new(new List<ThemeOption>());

    [ObservableProperty] private string selectedDateFormat = AppPreferencesState.DefaultDateFormat;

    [ObservableProperty] private DateFormatOption? selectedDateFormatOption;

    [ObservableProperty] private string selectedLanguage = AppPreferencesState.DefaultLanguage;

    [ObservableProperty] private LanguageOption? selectedLanguageOption;

    [ObservableProperty] private string selectedTheme = AppPreferencesState.DefaultTheme;

    [ObservableProperty] private ThemeOption? selectedThemeOption;

    [ObservableProperty] private bool showNotificationBadge = true;

    [ObservableProperty] private bool showSyncStatusIndicator = AppPreferencesState.DefaultShowSyncStatusIndicator;

    public AppearanceSettingsViewModel(
        IAppPreferencesService preferences,
        IThemeService themeService,
        ILocalizationService localization,
        IBirdManager birdManager,
        IBackgroundTaskRunner backgroundTaskRunner)
    {
        _preferences = preferences;
        _themeService = themeService;
        _localization = localization;
        _birdManager = birdManager;
        _backgroundTaskRunner = backgroundTaskRunner;

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

    public string SyncIndicatorHint =>
        ShowSyncStatusIndicator
            ? _localization.GetString("Settings.SyncIndicatorHint.Enabled")
            : _localization.GetString("Settings.SyncIndicatorHint.Disabled");

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetimeCancellation.Cancel();
        _languageReloadCancellation?.Cancel();
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _localization.LanguageChanged -= OnLanguageChanged;
        _lifetimeCancellation.Dispose();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            RestoreSelectedLanguageFromPreferences();
            return;
        }

        var normalized = AppLanguages.Normalize(value);

        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(LanguageHint));
            return;
        }

        if (_preferences.SelectedLanguage != normalized)
            _preferences.SelectedLanguage = normalized;

        if (_localization.ApplyLanguage(normalized))
            _backgroundTaskRunner.Run(
                ReloadBirdsForLanguageChangeAsync,
                new BackgroundTaskOptions("Reload birds after language change"),
                _lifetimeCancellation.Token);

        OnPropertyChanged(nameof(LanguageHint));
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (_isSynchronizingSelections)
            return;

        if (value is null)
        {
            RestoreSelectedLanguageFromPreferences();
            return;
        }

        SelectedLanguage = value.Code;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            RestoreSelectedThemeFromPreferences();
            return;
        }

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

    partial void OnSelectedThemeOptionChanged(ThemeOption? value)
    {
        if (_isSynchronizingSelections)
            return;

        if (value is null)
        {
            RestoreSelectedThemeFromPreferences();
            return;
        }

        SelectedTheme = value.Code;
    }

    partial void OnSelectedDateFormatChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            RestoreSelectedDateFormatFromPreferences();
            return;
        }

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

    partial void OnSelectedDateFormatOptionChanged(DateFormatOption? value)
    {
        if (_isSynchronizingSelections)
            return;

        if (value is null)
        {
            RestoreSelectedDateFormatFromPreferences();
            return;
        }

        SelectedDateFormat = value.Code;
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

    partial void OnShowSyncStatusIndicatorChanged(bool value)
    {
        if (_isSynchronizingSelections)
        {
            OnPropertyChanged(nameof(SyncIndicatorHint));
            return;
        }

        if (_preferences.ShowSyncStatusIndicator != value)
            _preferences.ShowSyncStatusIndicator = value;

        OnPropertyChanged(nameof(SyncIndicatorHint));
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IAppPreferencesService.SelectedLanguage)
            or nameof(IAppPreferencesService.SelectedTheme)
            or nameof(IAppPreferencesService.SelectedDateFormat)
            or nameof(IAppPreferencesService.ShowNotificationBadge)
            or nameof(IAppPreferencesService.ShowSyncStatusIndicator))
            ReloadFromPreferences(true);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var preservedTheme = ThemeKeys.Normalize(_preferences.SelectedTheme);

        BuildAvailableLanguages();
        BuildAvailableThemes();
        BuildAvailableDateFormats();
        ReloadFromPreferences();
        _themeService.ApplyTheme(preservedTheme);
        OnPropertyChanged(nameof(AvailableLanguages));
        OnPropertyChanged(nameof(AvailableThemes));
        OnPropertyChanged(nameof(AvailableDateFormats));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedDateFormat));
        OnPropertyChanged(nameof(SelectedLanguageOption));
        OnPropertyChanged(nameof(SelectedThemeOption));
        OnPropertyChanged(nameof(SelectedDateFormatOption));
        OnPropertyChanged(nameof(LanguageHint));
        OnPropertyChanged(nameof(ThemeHint));
        OnPropertyChanged(nameof(DateFormatHint));
        OnPropertyChanged(nameof(NotificationsHint));
        OnPropertyChanged(nameof(SyncIndicatorHint));
    }

    private async Task ReloadBirdsForLanguageChangeAsync(CancellationToken cancellationToken)
    {
        var operationCancellation = CreateLanguageReloadCancellation(cancellationToken);

        try
        {
            await _birdManager.ReloadAsync(operationCancellation.Token);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            // A newer language change superseded this reload or the view model is being disposed.
        }
        finally
        {
            ClearLanguageReloadCancellation(operationCancellation);
        }
    }

    private CancellationTokenSource CreateLanguageReloadCancellation(CancellationToken cancellationToken)
    {
        var previous = _languageReloadCancellation;
        var current = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        _languageReloadCancellation = current;

        previous?.Cancel();
        previous?.Dispose();

        return current;
    }

    private void ClearLanguageReloadCancellation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_languageReloadCancellation, operationCancellation))
            _languageReloadCancellation = null;

        operationCancellation.Dispose();
    }

    private void BuildAvailableLanguages()
    {
        AvailableLanguages = CreateLocalizedOptions(
            [
                (AppLanguages.Russian, _localization.GetString("Language.Russian")),
                (AppLanguages.English, _localization.GetString("Language.English"))
            ],
            static (code, displayName) => new LanguageOption(code, displayName));
    }

    private void BuildAvailableThemes()
    {
        AvailableThemes = CreateLocalizedOptions(
            _themeService.AvailableThemes
                .Select(theme => (theme, _localization.GetString($"Settings.Theme.{theme}")))
                .ToArray(),
            static (code, displayName) => new ThemeOption(code, displayName));
    }

    private void BuildAvailableDateFormats()
    {
        AvailableDateFormats = CreateLocalizedOptions(
            [
                (DateDisplayFormats.DayMonthYear, _localization.GetString("Settings.DateFormat.DayMonthYear")),
                (DateDisplayFormats.MonthDayYear, _localization.GetString("Settings.DateFormat.MonthDayYear")),
                (DateDisplayFormats.YearMonthDay, _localization.GetString("Settings.DateFormat.YearMonthDay"))
            ],
            static (code, displayName) => new DateFormatOption(code, displayName));
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
            SelectedLanguageOption = FindOption(AvailableLanguages, normalizedLanguage, static option => option.Code);
            SelectedThemeOption = FindOption(AvailableThemes, normalizedTheme, static option => option.Code);
            SelectedDateFormatOption = FindOption(AvailableDateFormats, normalizedDateFormat, static option => option.Code);
            ShowNotificationBadge = _preferences.ShowNotificationBadge;
            ShowSyncStatusIndicator = _preferences.ShowSyncStatusIndicator;
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
        OnPropertyChanged(nameof(SyncIndicatorHint));
    }

    private void RestoreSelectedLanguageFromPreferences()
    {
        var normalized = AppLanguages.Normalize(_preferences.SelectedLanguage);
        RestoreSelection(
            () =>
            {
                SelectedLanguage = normalized;
                SelectedLanguageOption = FindOption(AvailableLanguages, normalized, static option => option.Code);
            },
            nameof(SelectedLanguage),
            nameof(SelectedLanguageOption),
            nameof(LanguageHint));
    }

    private void RestoreSelectedThemeFromPreferences()
    {
        var normalized = ThemeKeys.Normalize(_preferences.SelectedTheme);
        RestoreSelection(
            () =>
            {
                SelectedTheme = normalized;
                SelectedThemeOption = FindOption(AvailableThemes, normalized, static option => option.Code);
            },
            nameof(SelectedTheme),
            nameof(SelectedThemeOption),
            nameof(ThemeHint));
    }

    private void RestoreSelectedDateFormatFromPreferences()
    {
        var normalized = DateDisplayFormats.Normalize(_preferences.SelectedDateFormat);
        RestoreSelection(
            () =>
            {
                SelectedDateFormat = normalized;
                SelectedDateFormatOption = FindOption(AvailableDateFormats, normalized, static option => option.Code);
            },
            nameof(SelectedDateFormat),
            nameof(SelectedDateFormatOption),
            nameof(DateFormatHint));
    }

    private void RestoreSelection(Action assign, params string[] propertyNames)
    {
        _isSynchronizingSelections = true;
        try
        {
            assign();
        }
        finally
        {
            _isSynchronizingSelections = false;
        }

        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);
    }

    private static TOption? FindOption<TOption>(
        IEnumerable<TOption> options,
        string code,
        Func<TOption, string> getCode)
        where TOption : class
    {
        return options.FirstOrDefault(option => string.Equals(getCode(option), code, StringComparison.Ordinal));
    }

    private static ReadOnlyCollection<TOption> CreateLocalizedOptions<TOption>(
        IReadOnlyList<(string Code, string DisplayName)> entries,
        Func<string, string, TOption> factory)
        where TOption : class
    {
        return new ReadOnlyCollection<TOption>(
            entries.Select(entry => factory(entry.Code, entry.DisplayName)).ToList());
    }
}

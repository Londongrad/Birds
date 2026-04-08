using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using Birds.UI.Services.Theming.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace Birds.Tests.UI.ViewModels
{
    public class SettingsViewModelTests
    {
        private readonly TestPreferencesService _preferences = new();
        private readonly Mock<IThemeService> _themeService = new();
        private readonly Mock<ILocalizationService> _localization = new();
        private readonly Mock<IBirdManager> _birdManager = new();
        private CultureInfo _culture = CultureInfo.GetCultureInfo(AppLanguages.English);

        public SettingsViewModelTests()
        {
            _themeService.SetupGet(x => x.AvailableThemes)
                .Returns(new ReadOnlyCollection<string>(ThemeKeys.SupportedThemes.ToList()));

            _localization.SetupGet(x => x.CurrentCulture).Returns(() => _culture);
            _localization.SetupGet(x => x.CurrentLanguage).Returns(() => _culture.Name);
            _localization.SetupGet(x => x.CurrentDateFormat).Returns(DateDisplayFormats.DayMonthYear);
            _localization.Setup(x => x.GetString(It.IsAny<string>()))
                .Returns((string key) => AppText.Get(key, _culture));
        }

        [Fact]
        public void Hints_Should_Use_LocalizationService_Strings()
        {
            _preferences.SelectedLanguage = AppLanguages.English;
            _preferences.SelectedTheme = ThemeKeys.Graphite;
            _preferences.SelectedDateFormat = DateDisplayFormats.DayMonthYear;
            _preferences.ShowNotificationBadge = true;
            _preferences.ReduceMotion = false;

            var sut = CreateSut();

            sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Graphite", _culture));
            sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.DayMonthYear", _culture));
            sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Enabled", _culture));
            sut.MotionHint.Should().Be(AppText.Get("Settings.MotionHint.Disabled", _culture));
        }

        [Fact]
        public void LanguageChanged_Should_Rebuild_Localized_Options_And_Hints()
        {
            _preferences.SelectedLanguage = AppLanguages.Russian;
            _preferences.SelectedTheme = ThemeKeys.Steel;
            _preferences.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
            _preferences.ShowNotificationBadge = false;
            _preferences.ReduceMotion = true;

            var sut = CreateSut();

            _culture = CultureInfo.GetCultureInfo(AppLanguages.English);
            _localization.Raise(x => x.LanguageChanged += null, EventArgs.Empty);

            sut.AvailableLanguages[0].DisplayName.Should().Be(AppText.Get("Language.Russian", _culture));
            sut.AvailableThemes.Should().Contain(x => x.DisplayName == AppText.Get("Settings.Theme.Steel", _culture));
            sut.ThemeHint.Should().Be(AppText.Get("Settings.ThemeHint.Steel", _culture));
            sut.DateFormatHint.Should().Be(AppText.Get("Settings.DateFormatHint.YearMonthDay", _culture));
            sut.NotificationsHint.Should().Be(AppText.Get("Settings.NotificationsHint.Disabled", _culture));
            sut.MotionHint.Should().Be(AppText.Get("Settings.MotionHint.Enabled", _culture));
        }

        private SettingsViewModel CreateSut()
            => new(_preferences, _themeService.Object, _localization.Object, _birdManager.Object);

        private sealed class TestPreferencesService : IAppPreferencesService
        {
            private string _selectedLanguage = AppLanguages.Russian;
            private string _selectedTheme = ThemeKeys.Graphite;
            private string _selectedDateFormat = DateDisplayFormats.DayMonthYear;
            private bool _showNotificationBadge = true;
            private bool _reduceMotion;

            public event PropertyChangedEventHandler? PropertyChanged;

            public string SelectedLanguage
            {
                get => _selectedLanguage;
                set
                {
                    if (_selectedLanguage == value)
                        return;
                    _selectedLanguage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguage)));
                }
            }

            public string SelectedTheme
            {
                get => _selectedTheme;
                set
                {
                    if (_selectedTheme == value)
                        return;
                    _selectedTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTheme)));
                }
            }

            public string SelectedDateFormat
            {
                get => _selectedDateFormat;
                set
                {
                    if (_selectedDateFormat == value)
                        return;
                    _selectedDateFormat = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDateFormat)));
                }
            }

            public bool ShowNotificationBadge
            {
                get => _showNotificationBadge;
                set
                {
                    if (_showNotificationBadge == value)
                        return;
                    _showNotificationBadge = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowNotificationBadge)));
                }
            }

            public bool ReduceMotion
            {
                get => _reduceMotion;
                set
                {
                    if (_reduceMotion == value)
                        return;
                    _reduceMotion = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReduceMotion)));
                }
            }

            public void ResetToDefaults()
            {
                SelectedLanguage = AppPreferencesState.DefaultLanguage;
                SelectedTheme = AppPreferencesState.DefaultTheme;
                SelectedDateFormat = AppPreferencesState.DefaultDateFormat;
                ShowNotificationBadge = true;
                ReduceMotion = false;
            }
        }
    }
}

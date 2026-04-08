using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using FluentAssertions;

namespace Birds.Tests.UI.Services
{
    public sealed class AppPreferencesServiceTests
    {
        [Fact]
        public void Constructor_WhenPreferencesFileDoesNotExist_Should_UseDefaults()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var sut = new JsonAppPreferencesService(new TestPreferencesPathProvider(tempDirectory));

                sut.SelectedLanguage.Should().Be(AppPreferencesState.DefaultLanguage);
                sut.SelectedTheme.Should().Be(AppPreferencesState.DefaultTheme);
                sut.SelectedDateFormat.Should().Be(AppPreferencesState.DefaultDateFormat);
                sut.ShowNotificationBadge.Should().BeTrue();
                sut.ReduceMotion.Should().BeFalse();
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void PropertyChanges_Should_PersistAndRestorePreferences()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var provider = new TestPreferencesPathProvider(tempDirectory);
                var sut = new JsonAppPreferencesService(provider)
                {
                    SelectedLanguage = AppLanguages.English,
                    SelectedTheme = ThemeKeys.Steel,
                    SelectedDateFormat = DateDisplayFormats.YearMonthDay,
                    ShowNotificationBadge = false,
                    ReduceMotion = true
                };

                var reloaded = new JsonAppPreferencesService(provider);

                reloaded.SelectedLanguage.Should().Be(AppLanguages.English);
                reloaded.SelectedTheme.Should().Be(ThemeKeys.Steel);
                reloaded.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
                reloaded.ShowNotificationBadge.Should().BeFalse();
                reloaded.ReduceMotion.Should().BeTrue();
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void Constructor_WhenLegacyValuesArePersisted_Should_NormalizeThem()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var provider = new TestPreferencesPathProvider(tempDirectory);
                File.WriteAllText(
                    provider.GetPreferencesPath(),
                    """
                    {
                      "selectedLanguage": "ru-RU",
                      "selectedTheme": "Сталь",
                      "selectedDateFormat": "ymd",
                      "showNotificationBadge": true,
                      "reduceMotion": false
                    }
                    """);

                var sut = new JsonAppPreferencesService(provider);

                sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
                sut.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "Birds.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class TestPreferencesPathProvider(string directory) : IAppPreferencesPathProvider
        {
            public string GetPreferencesPath() => Path.Combine(directory, "preferences.json");
        }
    }
}

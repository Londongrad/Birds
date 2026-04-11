using Birds.Shared.Localization;
using Birds.UI.Services.Import;
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
                sut.SelectedImportMode.Should().Be(AppPreferencesState.DefaultImportMode);
                sut.CustomExportPath.Should().BeEmpty();
                sut.AutoExportEnabled.Should().BeTrue();
                sut.ShowNotificationBadge.Should().BeTrue();
                sut.ShowSyncStatusIndicator.Should().BeTrue();
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
                    SelectedImportMode = BirdImportModes.Replace,
                    CustomExportPath = "C:\\exports\\birds-custom.json",
                    AutoExportEnabled = false,
                    ShowNotificationBadge = false,
                    ShowSyncStatusIndicator = false,
                    ReduceMotion = true
                };

                var reloaded = new JsonAppPreferencesService(provider);

                reloaded.SelectedLanguage.Should().Be(AppLanguages.English);
                reloaded.SelectedTheme.Should().Be(ThemeKeys.Steel);
                reloaded.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
                reloaded.SelectedImportMode.Should().Be(BirdImportModes.Replace);
                reloaded.CustomExportPath.Should().Be("C:\\exports\\birds-custom.json");
                reloaded.AutoExportEnabled.Should().BeFalse();
                reloaded.ShowNotificationBadge.Should().BeFalse();
                reloaded.ShowSyncStatusIndicator.Should().BeFalse();
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
                      "selectedImportMode": "replace",
                      "customExportPath": "C:\\exports\\birds-custom.json",
                      "autoExportEnabled": false,
                      "showNotificationBadge": true,
                      "showSyncStatusIndicator": false,
                      "reduceMotion": false
                    }
                    """);

                var sut = new JsonAppPreferencesService(provider);

                sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
                sut.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
                sut.SelectedImportMode.Should().Be(BirdImportModes.Replace);
                sut.CustomExportPath.Should().Be("C:\\exports\\birds-custom.json");
                sut.AutoExportEnabled.Should().BeFalse();
                sut.ShowSyncStatusIndicator.Should().BeFalse();
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

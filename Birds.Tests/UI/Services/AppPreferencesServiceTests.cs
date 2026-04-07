using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
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
                    SelectedLanguage = "English",
                    SelectedTheme = "Сталь",
                    ShowNotificationBadge = false,
                    ReduceMotion = true
                };

                var reloaded = new JsonAppPreferencesService(provider);

                reloaded.SelectedLanguage.Should().Be("English");
                reloaded.SelectedTheme.Should().Be("Сталь");
                reloaded.ShowNotificationBadge.Should().BeFalse();
                reloaded.ReduceMotion.Should().BeTrue();
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

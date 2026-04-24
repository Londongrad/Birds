using System.Text.Json;
using Birds.Shared.Localization;
using Birds.Shared.Sync;
using Birds.UI.Services.Import;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Theming;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Birds.Tests.UI.Services;

public sealed class AppPreferencesServiceTests
{
    [Fact]
    public void Constructor_WhenPreferencesFileDoesNotExist_Should_UseDefaults()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sut = CreateSut(new TestPreferencesPathProvider(tempDirectory));

            sut.SelectedLanguage.Should().Be(AppPreferencesState.DefaultLanguage);
            sut.SelectedTheme.Should().Be(AppPreferencesState.DefaultTheme);
            sut.SelectedDateFormat.Should().Be(AppPreferencesState.DefaultDateFormat);
            sut.SelectedImportMode.Should().Be(AppPreferencesState.DefaultImportMode);
            sut.SelectedSyncInterval.Should().Be(AppPreferencesState.DefaultSyncInterval);
            sut.CustomExportPath.Should().BeEmpty();
            sut.AutoExportEnabled.Should().BeTrue();
            sut.ShowNotificationBadge.Should().BeTrue();
            sut.ShowSyncStatusIndicator.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void PropertyChanges_Should_PersistAndRestorePreferences()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var provider = new TestPreferencesPathProvider(tempDirectory);
            var sut = CreateSut(provider);
            sut.SelectedLanguage = AppLanguages.English;
            sut.SelectedTheme = ThemeKeys.Steel;
            sut.SelectedDateFormat = DateDisplayFormats.YearMonthDay;
            sut.SelectedImportMode = BirdImportModes.Replace;
            sut.SelectedSyncInterval = RemoteSyncIntervalPresets.ThirtySeconds;
            sut.CustomExportPath = "C:\\exports\\birds-custom.json";
            sut.AutoExportEnabled = false;
            sut.ShowNotificationBadge = false;
            sut.ShowSyncStatusIndicator = false;

            var reloaded = CreateSut(provider);

            reloaded.SelectedLanguage.Should().Be(AppLanguages.English);
            reloaded.SelectedTheme.Should().Be(ThemeKeys.Steel);
            reloaded.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
            reloaded.SelectedImportMode.Should().Be(BirdImportModes.Replace);
            reloaded.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.ThirtySeconds);
            reloaded.CustomExportPath.Should().Be("C:\\exports\\birds-custom.json");
            reloaded.AutoExportEnabled.Should().BeFalse();
            reloaded.ShowNotificationBadge.Should().BeFalse();
            reloaded.ShowSyncStatusIndicator.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
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
                  "selectedSyncInterval": "60",
                  "customExportPath": "C:\\exports\\birds-custom.json",
                  "autoExportEnabled": false,
                  "showNotificationBadge": true,
                  "showSyncStatusIndicator": false
                }
                """);

            var sut = CreateSut(provider);

            sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
            sut.SelectedDateFormat.Should().Be(DateDisplayFormats.YearMonthDay);
            sut.SelectedImportMode.Should().Be(BirdImportModes.Replace);
            sut.SelectedSyncInterval.Should().Be(RemoteSyncIntervalPresets.OneMinute);
            sut.CustomExportPath.Should().Be("C:\\exports\\birds-custom.json");
            sut.AutoExportEnabled.Should().BeFalse();
            sut.ShowSyncStatusIndicator.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WhenPreferencesJsonIsMalformed_Should_BackupBrokenFile_LogFailure_AndUseDefaults()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var provider = new TestPreferencesPathProvider(tempDirectory);
            var logger = new Mock<ILogger<JsonAppPreferencesService>>();
            File.WriteAllText(provider.GetPreferencesPath(), "{ this is not valid json");

            var sut = CreateSut(provider, logger.Object);

            sut.SelectedLanguage.Should().Be(AppPreferencesState.DefaultLanguage);
            sut.SelectedTheme.Should().Be(AppPreferencesState.DefaultTheme);
            Directory.GetFiles(tempDirectory, "preferences.json.broken-*")
                .Should()
                .ContainSingle();
            File.ReadAllText(provider.GetPreferencesPath()).Should().Contain("SelectedLanguage");
            VerifyLogged(logger, LogLevel.Error, "Failed to load preferences");
            VerifyLogged(logger, LogLevel.Warning, "Backed up broken preferences file");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void SaveFailure_Should_BeLogged_And_NotCrashUi()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var logger = new Mock<ILogger<JsonAppPreferencesService>>();
            var provider = new FixedPreferencesPathProvider(tempDirectory);
            var sut = CreateSut(provider, logger.Object);

            sut.SelectedTheme = ThemeKeys.Steel;

            sut.SelectedTheme.Should().Be(ThemeKeys.Steel);
            VerifyLogged(logger, LogLevel.Error, "Failed to save preferences");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void SaveFailure_WhenReplaceFails_Should_KeepExistingPreferencesFile()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var provider = new TestPreferencesPathProvider(tempDirectory);
            var originalJson = """
                               {
                                 "selectedTheme": "Graphite",
                                 "customExportPath": "C:\\exports\\stable.json"
                               }
                               """;
            File.WriteAllText(provider.GetPreferencesPath(), originalJson);
            var logger = new Mock<ILogger<JsonAppPreferencesService>>();
            var sut = CreateSut(provider, logger.Object);

            using (File.Open(provider.GetPreferencesPath(), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                sut.CustomExportPath = "C:\\exports\\new.json";
            }

            File.ReadAllText(provider.GetPreferencesPath()).Should().Be(originalJson);
            VerifyLogged(logger, LogLevel.Error, "Failed to save preferences");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void PropertyChanges_Should_WriteValidJson()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var provider = new TestPreferencesPathProvider(tempDirectory);
            var sut = CreateSut(provider);

            sut.SelectedTheme = ThemeKeys.Steel;

            using var document = JsonDocument.Parse(File.ReadAllText(provider.GetPreferencesPath()));
            document.RootElement.GetProperty("SelectedTheme").GetString().Should().Be(ThemeKeys.Steel);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public async Task ConcurrentPropertyChanges_Should_NotCorruptPreferencesFile()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var provider = new TestPreferencesPathProvider(tempDirectory);
            var sut = CreateSut(provider);
            var tasks = Enumerable.Range(0, 32)
                .Select(index => Task.Run(() =>
                    sut.CustomExportPath = $"C:\\exports\\birds-{index}.json"))
                .ToArray();

            await Task.WhenAll(tasks);

            using var document = JsonDocument.Parse(File.ReadAllText(provider.GetPreferencesPath()));
            document.RootElement.GetProperty("CustomExportPath").GetString()
                .Should()
                .StartWith("C:\\exports\\birds-");
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private static JsonAppPreferencesService CreateSut(
        IAppPreferencesPathProvider pathProvider,
        ILogger<JsonAppPreferencesService>? logger = null)
    {
        return new JsonAppPreferencesService(
            pathProvider,
            logger ?? NullLogger<JsonAppPreferencesService>.Instance);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Birds.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestPreferencesPathProvider(string directory) : IAppPreferencesPathProvider
    {
        public string GetPreferencesPath()
        {
            return Path.Combine(directory, "preferences.json");
        }
    }

    private sealed class FixedPreferencesPathProvider(string path) : IAppPreferencesPathProvider
    {
        public string GetPreferencesPath()
        {
            return path;
        }
    }

    private static void VerifyLogged(
        Mock<ILogger<JsonAppPreferencesService>> logger,
        LogLevel level,
        string messagePart)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains(messagePart, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

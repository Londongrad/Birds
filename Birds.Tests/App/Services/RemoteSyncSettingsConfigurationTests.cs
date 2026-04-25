using System.ComponentModel;
using Birds.App.Services;
using Birds.Infrastructure.Configuration;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Sync;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.App.Services;

public sealed class RemoteSyncSettingsConfigurationTests
{
    [Fact]
    public void RuntimeOptionsProvider_WhenUserConfigurationIsNotSaved_ShouldUseStartupOptions()
    {
        var startupOptions = new RemoteSyncRuntimeOptions(true, "Host=startup;Database=birds;Username=user;Password=secret");
        var preferences = new TestPreferencesService { RemoteSyncConfigurationSaved = false };
        var credentialStore = new TestCredentialStore();
        var sut = new RemoteSyncRuntimeOptionsProvider(startupOptions, preferences, credentialStore);

        var result = sut.Current;

        result.IsConfigured.Should().BeTrue();
        result.ConnectionString.Should().Be(startupOptions.ConnectionString);
    }

    [Fact]
    public void RuntimeOptionsProvider_WhenSavedConfigurationIsEnabled_ShouldBuildConnectionFromPreferences()
    {
        var preferences = new TestPreferencesService
        {
            RemoteSyncConfigurationSaved = true,
            RemoteSyncEnabled = true,
            RemoteSyncHost = "localhost",
            RemoteSyncPort = 5544,
            RemoteSyncDatabase = "birds",
            RemoteSyncUsername = "tester"
        };
        var credentialStore = new TestCredentialStore { Password = "secret" };
        var sut = new RemoteSyncRuntimeOptionsProvider(RemoteSyncRuntimeOptions.Disabled, preferences, credentialStore);

        var result = sut.Current;

        result.IsConfigured.Should().BeTrue();
        result.ConnectionString.Should().Contain("Host=localhost");
        result.ConnectionString.Should().Contain("Port=5544");
        result.ConnectionString.Should().Contain("Database=birds");
        result.ConnectionString.Should().Contain("Username=tester");
        result.ConnectionString.Should().Contain("Password=secret");
    }

    [Fact]
    public void RuntimeOptionsProvider_WhenSavedConfigurationHasNoPassword_ShouldReportMissingConfiguration()
    {
        var preferences = new TestPreferencesService
        {
            RemoteSyncConfigurationSaved = true,
            RemoteSyncEnabled = true,
            RemoteSyncHost = "localhost",
            RemoteSyncPort = 5432,
            RemoteSyncDatabase = "birds",
            RemoteSyncUsername = "tester"
        };
        var sut = new RemoteSyncRuntimeOptionsProvider(
            RemoteSyncRuntimeOptions.Disabled,
            preferences,
            new TestCredentialStore());

        var result = sut.Current;

        result.IsEnabled.Should().BeTrue();
        result.IsConfigured.Should().BeFalse();
        result.ConfigurationErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreConnectionFieldsAndPasswordSeparately()
    {
        var preferences = new TestPreferencesService();
        var credentialStore = new TestCredentialStore();
        var provider = new StaticRemoteSyncRuntimeOptionsProvider(RemoteSyncRuntimeOptions.Disabled);
        var sut = new RemoteSyncSettingsService(
            preferences,
            credentialStore,
            provider,
            NullLogger<RemoteSyncSettingsService>.Instance);

        var result = await sut.SaveAsync(
            new RemoteSyncSettingsUpdate(
                true,
                "localhost",
                5432,
                "birds",
                "tester",
                "secret"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        preferences.RemoteSyncConfigurationSaved.Should().BeTrue();
        preferences.RemoteSyncEnabled.Should().BeTrue();
        preferences.RemoteSyncHost.Should().Be("localhost");
        preferences.RemoteSyncPort.Should().Be(5432);
        preferences.RemoteSyncDatabase.Should().Be("birds");
        preferences.RemoteSyncUsername.Should().Be("tester");
        credentialStore.Password.Should().Be("secret");
    }

    private sealed class TestCredentialStore : IRemoteSyncCredentialStore
    {
        public string? Password { get; set; }

        public bool HasPassword()
        {
            return !string.IsNullOrWhiteSpace(Password);
        }

        public string? TryLoadPassword()
        {
            return Password;
        }

        public void SavePassword(string password)
        {
            Password = password;
        }

        public void ClearPassword()
        {
            Password = null;
        }
    }

    private sealed class TestPreferencesService : IAppPreferencesService
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedLanguage { get; set; } = AppPreferencesState.DefaultLanguage;

        public string SelectedTheme { get; set; } = AppPreferencesState.DefaultTheme;

        public string SelectedDateFormat { get; set; } = AppPreferencesState.DefaultDateFormat;

        public string SelectedImportMode { get; set; } = AppPreferencesState.DefaultImportMode;

        public string SelectedSyncInterval { get; set; } = AppPreferencesState.DefaultSyncInterval;

        public bool RemoteSyncConfigurationSaved { get; set; } =
            AppPreferencesState.DefaultRemoteSyncConfigurationSaved;

        public bool RemoteSyncEnabled { get; set; } = AppPreferencesState.DefaultRemoteSyncEnabled;

        public string RemoteSyncHost { get; set; } = string.Empty;

        public int RemoteSyncPort { get; set; } = AppPreferencesState.DefaultRemoteSyncPort;

        public string RemoteSyncDatabase { get; set; } = string.Empty;

        public string RemoteSyncUsername { get; set; } = string.Empty;

        public string CustomExportPath { get; set; } = string.Empty;

        public bool AutoExportEnabled { get; set; } = AppPreferencesState.DefaultAutoExportEnabled;

        public bool ShowNotificationBadge { get; set; } = true;

        public bool ShowSyncStatusIndicator { get; set; } = AppPreferencesState.DefaultShowSyncStatusIndicator;

        public void ResetToDefaults()
        {
            RemoteSyncConfigurationSaved = AppPreferencesState.DefaultRemoteSyncConfigurationSaved;
            RemoteSyncEnabled = AppPreferencesState.DefaultRemoteSyncEnabled;
            RemoteSyncHost = string.Empty;
            RemoteSyncPort = AppPreferencesState.DefaultRemoteSyncPort;
            RemoteSyncDatabase = string.Empty;
            RemoteSyncUsername = string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}

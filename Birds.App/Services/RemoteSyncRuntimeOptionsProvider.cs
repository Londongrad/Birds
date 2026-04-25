using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Npgsql;

namespace Birds.App.Services;

internal sealed class RemoteSyncRuntimeOptionsProvider(
    RemoteSyncRuntimeOptions startupOptions,
    IAppPreferencesService preferences,
    IRemoteSyncCredentialStore credentialStore) : IRemoteSyncRuntimeOptionsProvider
{
    private readonly IRemoteSyncCredentialStore _credentialStore = credentialStore;
    private readonly IAppPreferencesService _preferences = preferences;
    private readonly RemoteSyncRuntimeOptions _startupOptions = startupOptions;

    public event EventHandler? Changed;

    public RemoteSyncRuntimeOptions Current => BuildCurrentOptions();

    public void Refresh()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private RemoteSyncRuntimeOptions BuildCurrentOptions()
    {
        if (!_preferences.RemoteSyncConfigurationSaved)
            return _startupOptions;

        if (!_preferences.RemoteSyncEnabled)
            return RemoteSyncRuntimeOptions.Disabled;

        var validation = ValidateConfiguredFields(
            _preferences.RemoteSyncHost,
            _preferences.RemoteSyncPort,
            _preferences.RemoteSyncDatabase,
            _preferences.RemoteSyncUsername);
        if (validation is not null)
            return RemoteSyncRuntimeOptions.EnabledButNotConfigured(validation);

        var password = _credentialStore.TryLoadPassword();
        if (string.IsNullOrWhiteSpace(password))
            return RemoteSyncRuntimeOptions.EnabledButNotConfigured(ErrorMessages.RemoteSyncPasswordMissing);

        return new RemoteSyncRuntimeOptions(
            true,
            BuildConnectionString(
                _preferences.RemoteSyncHost,
                _preferences.RemoteSyncPort,
                _preferences.RemoteSyncDatabase,
                _preferences.RemoteSyncUsername,
                password));
    }

    internal static string BuildConnectionString(string host, int port, string database, string username, string password)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = port,
            Database = database.Trim(),
            Username = username.Trim(),
            Password = password,
            Pooling = true
        };

        return builder.ConnectionString;
    }

    internal static string? ValidateConfiguredFields(string host, int port, string database, string username)
    {
        if (string.IsNullOrWhiteSpace(host))
            return ErrorMessages.RemoteSyncHostMissing;

        if (port is <= 0 or > 65535)
            return ErrorMessages.RemoteSyncPortInvalid;

        if (string.IsNullOrWhiteSpace(database))
            return ErrorMessages.RemoteSyncDatabaseMissing;

        if (string.IsNullOrWhiteSpace(username))
            return ErrorMessages.RemoteSyncUsernameMissing;

        return null;
    }
}

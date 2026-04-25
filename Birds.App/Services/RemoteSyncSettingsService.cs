using System.Text.RegularExpressions;
using Birds.Infrastructure.Configuration;
using Birds.Shared.Constants;
using Birds.Shared.Localization;
using Birds.UI.Services.Preferences;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Sync;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Birds.App.Services;

internal sealed class RemoteSyncSettingsService(
    IAppPreferencesService preferences,
    IRemoteSyncCredentialStore credentialStore,
    IRemoteSyncRuntimeOptionsProvider runtimeOptionsProvider,
    ILogger<RemoteSyncSettingsService> logger) : IRemoteSyncSettingsService
{
    private readonly IRemoteSyncCredentialStore _credentialStore = credentialStore;
    private readonly ILogger<RemoteSyncSettingsService> _logger = logger;
    private readonly IAppPreferencesService _preferences = preferences;
    private readonly IRemoteSyncRuntimeOptionsProvider _runtimeOptionsProvider = runtimeOptionsProvider;

    public RemoteSyncSettingsSnapshot GetSnapshot()
    {
        if (!_preferences.RemoteSyncConfigurationSaved)
        {
            var runtimeOptions = _runtimeOptionsProvider.Current;
            if (runtimeOptions.IsEnabled)
                return CreateStartupSnapshot(runtimeOptions);
        }

        return new RemoteSyncSettingsSnapshot(
            _preferences.RemoteSyncConfigurationSaved,
            _preferences.RemoteSyncEnabled,
            _preferences.RemoteSyncHost,
            _preferences.RemoteSyncPort > 0
                ? _preferences.RemoteSyncPort
                : AppPreferencesState.DefaultRemoteSyncPort,
            _preferences.RemoteSyncDatabase,
            _preferences.RemoteSyncUsername,
            _credentialStore.HasPassword());
    }

    public Task<RemoteSyncSettingsResult> SaveAsync(RemoteSyncSettingsUpdate update,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateUpdate(update, requirePassword: update.IsEnabled && !_credentialStore.HasPassword());
        if (validation is not null)
            return Task.FromResult(validation);

        try
        {
            if (!string.IsNullOrWhiteSpace(update.Password))
                _credentialStore.SavePassword(update.Password);

            _preferences.RemoteSyncConfigurationSaved = true;
            _preferences.RemoteSyncEnabled = update.IsEnabled;
            _preferences.RemoteSyncHost = update.Host.Trim();
            _preferences.RemoteSyncPort = update.Port;
            _preferences.RemoteSyncDatabase = update.Database.Trim();
            _preferences.RemoteSyncUsername = update.Username.Trim();
            _runtimeOptionsProvider.Refresh();

            return Task.FromResult(RemoteSyncSettingsResult.Success(AppText.Get("Info.RemoteSyncSettingsSaved")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save remote sync configuration.");
            return Task.FromResult(RemoteSyncSettingsResult.Failure(ErrorMessages.RemoteSyncSettingsSaveFailed));
        }
    }

    public async Task<RemoteSyncSettingsResult> TestConnectionAsync(RemoteSyncSettingsUpdate update,
        CancellationToken cancellationToken)
    {
        var validation = ValidateUpdate(update, requirePassword: false);
        if (validation is not null)
            return validation;

        var password = string.IsNullOrWhiteSpace(update.Password)
            ? _credentialStore.TryLoadPassword()
            : update.Password;
        if (string.IsNullOrWhiteSpace(password))
            return RemoteSyncSettingsResult.Failure(
                ErrorMessages.RemoteSyncPasswordMissing,
                nameof(RemoteSyncSettingsUpdate.Password));

        var connectionString = RemoteSyncRuntimeOptionsProvider.BuildConnectionString(
            update.Host,
            update.Port,
            update.Database,
            update.Username,
            password);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return RemoteSyncSettingsResult.Success(AppText.Get("Info.RemoteSyncConnectionSucceeded"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Remote sync connection test failed for {ConnectionString}.",
                DiagnosticRedactor.RedactConnectionString(connectionString));
            return RemoteSyncSettingsResult.Failure(BuildConnectionFailureMessage(ex));
        }
    }

    private static string BuildConnectionFailureMessage(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        if (string.IsNullOrWhiteSpace(message))
            return ErrorMessages.RemoteSyncConnectionTestFailed;

        message = message.ReplaceLineEndings(" ").Trim();
        var safeMessage = Regex.Replace(
            message,
            @"(?<key>password|pwd|secret|token|access\s*token|api\s*key)\s*=\s*[^;\s]+",
            match => $"{match.Groups["key"].Value}={DiagnosticRedactor.RedactedValue}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return ErrorMessages.RemoteSyncConnectionFailed(safeMessage);
    }

    private RemoteSyncSettingsResult? ValidateUpdate(RemoteSyncSettingsUpdate update, bool requirePassword)
    {
        if (!update.IsEnabled)
            return null;

        var validation = RemoteSyncRuntimeOptionsProvider.ValidateConfiguredFields(
            update.Host,
            update.Port,
            update.Database,
            update.Username);
        if (validation is not null)
            return RemoteSyncSettingsResult.Failure(validation);

        if (requirePassword && string.IsNullOrWhiteSpace(update.Password))
            return RemoteSyncSettingsResult.Failure(
                ErrorMessages.RemoteSyncPasswordMissing,
                nameof(RemoteSyncSettingsUpdate.Password));

        return null;
    }

    private RemoteSyncSettingsSnapshot CreateStartupSnapshot(RemoteSyncRuntimeOptions runtimeOptions)
    {
        if (!runtimeOptions.IsConfigured || string.IsNullOrWhiteSpace(runtimeOptions.ConnectionString))
            return new RemoteSyncSettingsSnapshot(
                false,
                true,
                string.Empty,
                AppPreferencesState.DefaultRemoteSyncPort,
                string.Empty,
                string.Empty,
                _credentialStore.HasPassword());

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(runtimeOptions.ConnectionString);
            return new RemoteSyncSettingsSnapshot(
                false,
                true,
                builder.Host ?? string.Empty,
                builder.Port,
                builder.Database ?? string.Empty,
                builder.Username ?? string.Empty,
                _credentialStore.HasPassword());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse startup remote sync connection string {ConnectionString}.",
                DiagnosticRedactor.RedactConnectionString(runtimeOptions.ConnectionString));
            return new RemoteSyncSettingsSnapshot(
                false,
                true,
                string.Empty,
                AppPreferencesState.DefaultRemoteSyncPort,
                string.Empty,
                string.Empty,
                _credentialStore.HasPassword());
        }
    }
}

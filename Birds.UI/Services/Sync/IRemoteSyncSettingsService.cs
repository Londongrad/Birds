namespace Birds.UI.Services.Sync;

public interface IRemoteSyncSettingsService
{
    RemoteSyncSettingsSnapshot GetSnapshot();

    Task<RemoteSyncSettingsResult> SaveAsync(RemoteSyncSettingsUpdate update, CancellationToken cancellationToken);

    Task<RemoteSyncSettingsResult> TestConnectionAsync(RemoteSyncSettingsUpdate update,
        CancellationToken cancellationToken);
}

public sealed record RemoteSyncSettingsSnapshot(
    bool IsUserConfigured,
    bool IsEnabled,
    string Host,
    int Port,
    string Database,
    string Username,
    bool HasSavedPassword);

public sealed record RemoteSyncSettingsUpdate(
    bool IsEnabled,
    string Host,
    int Port,
    string Database,
    string Username,
    string? Password);

public sealed record RemoteSyncSettingsResult(
    bool IsSuccess,
    string Message,
    string? ErrorProperty = null)
{
    public static RemoteSyncSettingsResult Success(string message)
    {
        return new RemoteSyncSettingsResult(true, message);
    }

    public static RemoteSyncSettingsResult Failure(string message, string? errorProperty = null)
    {
        return new RemoteSyncSettingsResult(false, message, errorProperty);
    }
}

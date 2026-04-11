namespace Birds.Infrastructure.Configuration;

public sealed class RemoteSyncRuntimeOptions(bool isEnabled, string? connectionString)
{
    public static RemoteSyncRuntimeOptions Disabled { get; } = new(false, null);

    public bool IsEnabled { get; } = isEnabled;

    public string? ConnectionString { get; } =
        string.IsNullOrWhiteSpace(connectionString)
            ? null
            : connectionString;

    public bool IsConfigured => IsEnabled && !string.IsNullOrWhiteSpace(ConnectionString);
}
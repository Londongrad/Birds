namespace Birds.Infrastructure.Configuration;

public sealed class RemoteSyncRuntimeOptions(
    bool isEnabled,
    string? connectionString,
    string? configurationErrorMessage = null)
{
    public static RemoteSyncRuntimeOptions Disabled { get; } = new(false, null);

    public static RemoteSyncRuntimeOptions EnabledButNotConfigured(string configurationErrorMessage)
    {
        return new RemoteSyncRuntimeOptions(true, null, configurationErrorMessage);
    }

    public bool IsEnabled { get; } = isEnabled;

    public string? ConnectionString { get; } =
        string.IsNullOrWhiteSpace(connectionString)
            ? null
            : connectionString;

    public string? ConfigurationErrorMessage { get; } =
        string.IsNullOrWhiteSpace(configurationErrorMessage)
            ? null
            : configurationErrorMessage;

    public bool HasConfigurationError => IsEnabled
                                         && !string.IsNullOrWhiteSpace(ConfigurationErrorMessage);

    public bool IsConfigured => IsEnabled
                                && !string.IsNullOrWhiteSpace(ConnectionString)
                                && !HasConfigurationError;
}

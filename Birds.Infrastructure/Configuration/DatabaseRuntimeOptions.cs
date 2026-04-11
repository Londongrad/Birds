namespace Birds.Infrastructure.Configuration;

public sealed class DatabaseRuntimeOptions(DatabaseProvider provider, string connectionString)
{
    public DatabaseProvider Provider { get; } = provider;

    public string ConnectionString { get; } = connectionString;
}
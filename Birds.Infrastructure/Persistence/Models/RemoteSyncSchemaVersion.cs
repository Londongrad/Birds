namespace Birds.Infrastructure.Persistence.Models;

public sealed class RemoteSyncSchemaVersion
{
    private RemoteSyncSchemaVersion()
    {
    }

    public int Version { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public string? Description { get; private set; }

    public static RemoteSyncSchemaVersion Create(int version, DateTime appliedAtUtc, string? description)
    {
        return new RemoteSyncSchemaVersion
        {
            Version = version,
            AppliedAtUtc = UtcDateTimeStorage.NormalizeForStorage(appliedAtUtc),
            Description = description
        };
    }
}

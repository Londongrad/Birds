namespace Birds.Infrastructure.Persistence.Models;

public sealed class RemoteBirdTombstone
{
    private RemoteBirdTombstone()
    {
    }

    public Guid BirdId { get; private set; }
    public DateTime DeletedAtUtc { get; private set; }

    public static RemoteBirdTombstone Create(Guid birdId, DateTime deletedAtUtc)
    {
        return new RemoteBirdTombstone
        {
            BirdId = birdId,
            DeletedAtUtc = NormalizeForStorage(deletedAtUtc)
        };
    }

    public void AdvanceTo(DateTime deletedAtUtc)
    {
        var normalized = NormalizeForStorage(deletedAtUtc);
        if (normalized > DeletedAtUtc)
            DeletedAtUtc = normalized;
    }

    private static DateTime NormalizeForStorage(DateTime value)
    {
        return UtcDateTimeStorage.NormalizeForStorage(value);
    }
}

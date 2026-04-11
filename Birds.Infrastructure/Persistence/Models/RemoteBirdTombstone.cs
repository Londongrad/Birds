namespace Birds.Infrastructure.Persistence.Models
{
    public sealed class RemoteBirdTombstone
    {
        public Guid BirdId { get; private set; }
        public DateTime DeletedAtUtc { get; private set; }

        private RemoteBirdTombstone()
        { }

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
            => value.Kind switch
            {
                DateTimeKind.Utc => DateTime.SpecifyKind(value.ToLocalTime(), DateTimeKind.Unspecified),
                DateTimeKind.Local => DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
                _ => value
            };
    }
}

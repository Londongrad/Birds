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
                DeletedAtUtc = deletedAtUtc
            };
        }

        public void AdvanceTo(DateTime deletedAtUtc)
        {
            if (deletedAtUtc > DeletedAtUtc)
                DeletedAtUtc = deletedAtUtc;
        }
    }
}

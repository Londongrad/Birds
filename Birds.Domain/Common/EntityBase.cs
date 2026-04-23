namespace Birds.Domain.Common;

public abstract class EntityBase
{
    #region [ Methods ]

    /// <summary>Method for updating the entity's last modified time.</summary>
    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.Now;
        SyncStampUtc = DateTime.UtcNow;
    }

    #endregion [ Methods ]

    #region [ Properties ]

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime SyncStampUtc { get; private set; }

    #endregion [ Properties ]

    #region [ Ctors ]

    protected EntityBase()
    {
    }

    protected EntityBase(Guid id)
    {
        CreatedAt = DateTime.Now;
        SyncStampUtc = DateTime.UtcNow;
        Id = id;
    }

    protected EntityBase(Guid id, DateTime createdAt, DateTime? updatedAt, DateTime? syncStampUtc = null)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        SyncStampUtc = NormalizeUtcStamp(syncStampUtc ?? updatedAt ?? createdAt);
    }

    #endregion [ Ctors ]

    private static DateTime NormalizeUtcStamp(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

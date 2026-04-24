using Birds.Domain.Common;
using Birds.Domain.Enums;

namespace Birds.Domain.Entities;

public class Bird : EntityBase
{
    public const long InitialVersion = 1;

    #region [ Properties ]

    public BirdSpecies Name { get; private set; }
    public string? Description { get; private set; }
    public DateOnly Arrival { get; private set; }
    public DateOnly? Departure { get; private set; }
    public bool IsAlive { get; private set; }
    public long Version { get; private set; } = InitialVersion;

    #endregion [ Properties ]

    #region [ Ctors ]

    private Bird()
    {
    }

    private Bird(
        Guid id,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure = null,
        bool isAlive = true,
        long version = InitialVersion)
        : base(id)
    {
        Initialize(id, name, description, arrival, departure, isAlive);
        SetVersion(version);
    }

    private Bird(
        Guid id,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure,
        bool isAlive,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? syncStampUtc = null,
        long version = InitialVersion)
        : base(id, createdAt, updatedAt, syncStampUtc)
    {
        Initialize(id, name, description, arrival, departure, isAlive);
        SetVersion(version);
    }

    private void Initialize(
        Guid id,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure,
        bool isAlive)
    {
        GuardHelper.AgainstEmptyGuid(id, nameof(id));
        GuardHelper.AgainstInvalidEnum(name, nameof(name));
        GuardHelper.AgainstExceedsMaxLength(description, BirdValidationRules.DescriptionMaxLength, nameof(description));
        GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
        GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
        GuardHelper.AgainstInvalidDateRange(arrival, departure);
        GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, nameof(departure));

        Name = name;
        Description = description;
        Arrival = arrival;
        Departure = departure;
        IsAlive = isAlive;
    }

    #endregion [ Ctors ]

    #region [ Methods ]

    /// <summary>
    ///     Factory method to create a new Bird instance.
    /// </summary>
    /// <returns>A newly created <see cref="Bird" /> instance.</returns>
    public static Bird Create(
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure = null,
        bool isAlive = true)
    {
        GuardHelper.AgainstInvalidEnum(name, nameof(name));
        GuardHelper.AgainstExceedsMaxLength(description, BirdValidationRules.DescriptionMaxLength, nameof(description));
        GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
        GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
        GuardHelper.AgainstInvalidDateRange(arrival, departure);
        GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, nameof(departure));

        return new Bird(Guid.NewGuid(), name, description, arrival, departure, isAlive);
    }

    /// <summary>
    ///     Recreates an existing bird instance using its stored identifier and data.
    /// </summary>
    /// <remarks>
    ///     Used to restore a previously created bird from persistent storage or an update command.
    /// </remarks>
    /// <param name="id">The unique identifier of the existing bird.</param>
    /// <param name="name">The bird's name.</param>
    /// <returns>A reconstructed <see cref="Bird" /> instance.</returns>
    public static Bird Restore(
        Guid id,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure,
        bool isAlive,
        DateTime? createdAt = null,
        DateTime? updatedAt = null,
        DateTime? syncStampUtc = null,
        long version = InitialVersion)
    {
        GuardHelper.AgainstInvalidEnum(name, nameof(name));
        GuardHelper.AgainstExceedsMaxLength(description, BirdValidationRules.DescriptionMaxLength, nameof(description));
        GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
        GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
        GuardHelper.AgainstInvalidDateRange(arrival, departure);
        GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, nameof(departure));
        GuardHelper.AgainstEmptyGuid(id, nameof(id));

        return createdAt.HasValue
            ? new Bird(id, name, description, arrival, departure, isAlive, createdAt.Value, updatedAt, syncStampUtc,
                version)
            : new Bird(id, name, description, arrival, departure, isAlive, version);
    }

    /// <summary>
    ///     Updates the bird's state by replacing all its core attributes at once.
    /// </summary>
    /// <remarks>
    ///     Intended for full entity updates when all fields are known and validated externally.
    /// </remarks>
    public void Update(BirdSpecies name, string? description, DateOnly arrival, DateOnly? departure, bool isAlive)
    {
        GuardHelper.AgainstInvalidEnum(name, nameof(name));
        GuardHelper.AgainstExceedsMaxLength(description, BirdValidationRules.DescriptionMaxLength, nameof(description));
        GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
        GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
        GuardHelper.AgainstInvalidDateRange(arrival, departure);
        GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, nameof(departure));

        Name = name;
        Description = description;
        Arrival = arrival;
        Departure = departure;
        IsAlive = isAlive;
        Version++;

        UpdateTimestamp();
    }

    #endregion [ Methods ]

    private void SetVersion(long version)
    {
        if (version < InitialVersion)
            throw new ArgumentOutOfRangeException(nameof(version), version, "Version must be greater than zero.");

        Version = version;
    }
}

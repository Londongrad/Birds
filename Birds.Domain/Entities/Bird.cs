using Birds.Domain.Common;
using Birds.Domain.Enums;

namespace Birds.Domain.Entities
{
    public class Bird : EntityBase
    {
        #region [ Properties ]

        public BirdsName Name { get; private set; }
        public string? Description { get; private set; }
        public DateOnly Arrival { get; private set; }
        public DateOnly? Departure { get; private set; }
        public bool IsAlive { get; private set; }

        #endregion [ Properties ]

        #region [ Ctors ]

        private Bird()
        { }

        private Bird(
            Guid id, 
            BirdsName name, 
            string? description, 
            DateOnly arrival, 
            DateOnly? departure = null, 
            bool isAlive = true)
            : base(id)
        {
            GuardHelper.AgainstEmptyGuid(id, nameof(id));
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
            GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
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
        /// Factory method to create a new Bird instance
        /// </summary>
        /// <returns>A newly created <see cref="Bird"/> instance.</returns>
        public static Bird Create(
            BirdsName name, 
            string? description, 
            DateOnly arrival, 
            DateOnly? departure = null, 
            bool isAlive = true)
        {
            // Validate inputs (duplicate validation as in constructor)
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
            GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));

            return new Bird(Guid.NewGuid(), name, description, arrival, departure, isAlive);
        }

        /// <summary>
        /// Recreates an existing bird instance using its stored identifier and data.
        /// </summary>
        /// <remarks>
        /// Used to restore a previously created bird from persistent storage or an update command.
        /// </remarks>
        /// <param name="id">The unique identifier of the existing bird.</param>
        /// <param name="name">The bird’s name.</param>
        /// <returns>A reconstructed <see cref="Bird"/> instance.</returns>
        public static Bird Restore(
            Guid id, 
            BirdsName name, 
            string? description, 
            DateOnly arrival, 
            DateOnly? departure,
            bool isAlive)
        {
            // Validate inputs (duplicate validation as in constructor)
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
            GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
            GuardHelper.AgainstEmptyGuid(id, nameof(id));

            return new Bird(id, name, description, arrival, departure, isAlive);
        }

        /// <summary>
        /// Updates the bird’s state by replacing all its core attributes at once.
        /// </summary>
        /// <remarks>
        /// Intended for full entity updates when all fields are known and validated externally.
        /// </remarks>
        public void Update(BirdsName name, string? description, DateOnly arrival, DateOnly? departure, bool isAlive)
        {
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));
            GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
            GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, nameof(departure));

            Name = name;
            Description = description;
            Arrival = arrival;
            Departure = departure;
            IsAlive = isAlive;

            UpdateTimestamp();
        }

        #endregion [ Methods ]
    }
}
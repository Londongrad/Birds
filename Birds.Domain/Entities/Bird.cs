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

        private Bird(Guid id, BirdsName name, string? description, DateOnly arrival, bool isAlive = true)
            : base(id)
        {
            GuardHelper.AgainstEmptyGuid(id, nameof(id));
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));

            Name = name;
            Description = description;
            Arrival = arrival;
            IsAlive = isAlive;
        }

        #endregion [ Ctors ]

        #region [ Methods ]

        /// <summary>
        /// Factory method to create a new Bird instance
        /// </summary>
        public static Bird Create(BirdsName name, string? description, DateOnly arrival, bool isAlive = true)
        {
            // Validate inputs (duplicate validation as in constructor)
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));

            return new Bird(Guid.NewGuid(), name, description, arrival, isAlive);
        }

        public void SetName(BirdsName name)
        {
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            Name = name;
            UpdateTimestamp();
        }

        /// <summary>Update the entity in one go</summary>
        public void Update(DateOnly arrival, DateOnly? departure, string? description, bool isAlive)
        {
            GuardHelper.AgainstInvalidDateOnly(arrival, nameof(arrival));

            Arrival = arrival;
            Description = description;

            if (departure.HasValue)
                SetDeparture(departure.Value);
            else
                ClearDeparture();

            UpdateStatus(isAlive);
            UpdateTimestamp();
        }

        public void SetDeparture(DateOnly departure)
        {
            GuardHelper.AgainstInvalidDateOnly(departure, nameof(departure));
            GuardHelper.AgainstInvalidDateRange(Arrival, departure);

            Departure = departure;
            UpdateTimestamp();
        }

        public void ClearDeparture()
        {
            Departure = null;
            UpdateTimestamp();
        }

        public void UpdateStatus(bool isAlive)
        {
            GuardHelper.AgainstInvalidStatusUpdate(Departure, isAlive, nameof(Departure));

            IsAlive = isAlive;
            UpdateTimestamp();
        }

        public void SetDescription(string? description)
        {
            Description = description;
            UpdateTimestamp();
        }

        #endregion [ Methods ]
    }
}
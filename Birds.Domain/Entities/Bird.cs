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

        private Bird() { }
        public Bird(Guid id, BirdsName name, string? description, DateOnly arrival, bool isAlive = true)
            : base(id)
        {
            GuardHelper.AgainstEmptyGuid(id, nameof(id));
            GuardHelper.AgainstInvalidEnum(name, nameof(name));
            GuardHelper.AgainstInvalidDate(arrival, nameof(arrival));

            Name = name;
            Description = description;
            Arrival = arrival;
            IsAlive = isAlive;
        }

        #endregion [ Ctors ]

        #region [ Methods ]

        public void SetDeparture(DateOnly departure)
        {
            GuardHelper.AgainstInvalidDate(departure, nameof(departure));
            GuardHelper.AgainstInvalidDateRange(Arrival, departure, nameof(departure));

            Departure = departure;
            UpdateTimestamp();
        }

        public void ClearDeparture()
        {
            Departure = null;
            UpdateTimestamp();
        }

        public void MarkAsDead()
        {
            if (Departure is null)
                throw new ArgumentException("Departure date must be set before marking the bird as dead.");

            IsAlive = false;
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

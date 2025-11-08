namespace Birds.Domain.Common
{
    public abstract class EntityBase
    {
        #region [ Properties ]

        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        #endregion [ Properties ]

        #region [ Ctors ]

        protected EntityBase()
        { }

        protected EntityBase(Guid id)
        {
            CreatedAt = DateTime.Now;
            Id = id;
        }

        #endregion [ Ctors ]

        #region [ Methods ]

        /// <summary>Method for updating the entity's last modified time.</summary>
        protected void UpdateTimestamp()
        {
            UpdatedAt = DateTime.Now;
        }

        #endregion [ Methods ]
    }
}
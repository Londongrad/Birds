namespace Birds.Domain.Common
{
    public abstract class EntityBase
    {
        #region [ Properties ]

        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        #endregion [ Properties ]

        #region [ Ctors ]

        protected EntityBase()
        {
            CreatedAt = UpdatedAt = DateTime.UtcNow;
        }

        protected EntityBase(Guid id)
        {
            Id = id;
        }

        #endregion [ Ctors ]

        #region [ Methods ]

        /// <summary>Метод для обновления времени редактирования сущности</summary>
        protected void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion [ Methods ]
    }
}

using Birds.Domain.Entities;

namespace Birds.Application.Interfaces
{
    public interface IUnitOfWork
    {
        /// <summary>
        /// Defines data access operations for <see cref="Bird"/> entities.
        /// </summary>
        /// <remarks>
        /// Provides CRUD functionality and database interaction for the <see cref="Bird"/> aggregate.
        /// Implementations are responsible for persisting and retrieving domain objects from storage.
        /// </remarks>
        IBirdRepository BirdRepository { get; }

        /// <summary>Saves all changes made in the current context.</summary>
        /// <returns>The number of records modified in the database.</returns>
        /// <remarks>
        /// This method commits the changes made through the repositories.
        /// It must be called manually after performing <c>AddAsync</c>, <c>Update</c>, or <c>Remove</c> operations.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
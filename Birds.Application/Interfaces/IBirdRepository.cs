using Birds.Application.Exceptions;
using Birds.Domain.Entities;

namespace Birds.Application.Interfaces
{
    /// <summary>Defines the repository contract for working with <see cref="Bird"/> entities.</summary>
    public interface IBirdRepository
    {
        /// <summary>Retrieves a <see cref="Bird"/> entity by its identifier.</summary>
        /// <returns>An instance of <see cref="Bird"/>.</returns>
        /// <exception cref="NotFoundException"></exception>
        Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Returns a non-tracked collection of all birds.</summary>
        /// <returns>A collection of <see cref="IReadOnlyList{Bird}"/>.</returns>
        Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Adds a new bird and immediately persists the change to the database.</summary>
        /// <remarks>
        /// This method adds the specified <see cref="Bird"/> entity to the data source and saves the change.
        /// </remarks>
        Task AddAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing bird and immediately persists the changes to the database.</summary>
        /// <remarks>
        /// This method marks the provided <see cref="Bird"/> entity as modified and saves the update to the data source.
        /// </remarks>
        Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>Removes a bird and immediately persists the change to the database.</summary>
        /// <remarks>
        /// This method deletes the specified <see cref="Bird"/> entity from the data source and saves the change.
        /// </remarks>
        Task RemoveAsync(Bird bird, CancellationToken cancellationToken = default);
    }
}
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

        /// <summary>
        /// Adds a new bird to the underlying data store.
        /// </summary>
        /// <remarks>
        /// The bird is inserted asynchronously. 
        /// Actual persistence behavior depends on the specific data access implementation.
        /// </remarks>
        Task AddAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing bird in the data store.
        /// </summary>
        /// <remarks>
        /// The update is applied directly without loading the entity into memory.
        /// </remarks>
        /// <param name="bird">The bird entity containing the new values.</param>
        Task UpdateAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a bird from the data store.
        /// </summary>
        /// <remarks>
        /// The deletion is performed directly without loading the entity into memory.
        /// </remarks>
        /// <param name="id">The unique identifier of the bird to remove.</param>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
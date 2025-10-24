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

        /// <summary>Adds a new bird to the tracking context.</summary>
        /// <remarks>
        /// This method <b>does not perform saving to the database</b>.
        /// To persist the changes, you must call <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        Task AddAsync(Bird bird, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing bird in the tracking context.</summary>
        /// <remarks>
        /// This method <b>does not perform saving to the database</b>.
        /// To persist the changes, you must call <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        void Update(Bird bird);

        /// <summary>Removes a bird from the tracking context.</summary>
        /// <remarks>
        /// This method <b>does not perform saving to the database</b>.
        /// To persist the changes, you must call <see cref="IUnitOfWork.SaveChangesAsync"/>.
        /// </remarks>
        void Remove(Bird bird);
    }
}
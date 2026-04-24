using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Entities;
using Birds.Domain.Enums;

namespace Birds.Application.Interfaces;

/// <summary>Defines the repository contract for working with <see cref="Bird" /> entities.</summary>
public interface IBirdRepository
{
    /// <summary>Retrieves a <see cref="Bird" /> entity by its identifier.</summary>
    /// <returns>An instance of <see cref="Bird" />.</returns>
    /// <exception cref="NotFoundException"></exception>
    Task<Bird> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a non-tracked collection of all birds.</summary>
    /// <returns>A collection of <see cref="IReadOnlyList{Bird}" />.</returns>
    Task<IReadOnlyList<Bird>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new bird and immediately persists the change to the database.</summary>
    /// <remarks>
    ///     This method adds the specified <see cref="Bird" /> entity to the data source and saves the change.
    /// </remarks>
    Task AddAsync(Bird bird, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing bird and immediately persists the changes to the database.</summary>
    /// <remarks>
    ///     The update succeeds only when <paramref name="expectedVersion" /> matches the current persisted version.
    /// </remarks>
    Task<Bird> UpdateAsync(
        Guid id,
        long expectedVersion,
        BirdSpecies name,
        string? description,
        DateOnly arrival,
        DateOnly? departure,
        bool isAlive,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds missing birds and updates existing ones in a single persistence operation.
    /// </summary>
    Task<UpsertBirdsResult> UpsertAsync(
        IReadOnlyCollection<Bird> birds,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Replaces the persisted bird snapshot with the provided collection in one atomic operation.
    /// </summary>
    Task<UpsertBirdsResult> ReplaceWithSnapshotAsync(
        IReadOnlyCollection<Bird> birds,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a bird and immediately persists the change to the database.</summary>
    /// <remarks>
    ///     This method deletes the specified <see cref="Bird" /> entity from the data source and saves the change.
    /// </remarks>
    Task RemoveAsync(Bird bird, CancellationToken cancellationToken = default);
}

using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.UI.Services.Stores.BirdStore;

namespace Birds.UI.Services.Managers.Bird
{
    /// <summary>
    /// Provides high-level operations for managing the bird collection.
    /// Acts as a facade over <see cref="IBirdStore"/>, <see cref="BirdStoreInitializer"/>,
    /// and MediatR commands for create/update/delete.
    /// </summary>
    public interface IBirdManager
    {
        /// <summary>
        /// Reloads the entire bird collection from the database.
        /// </summary>
        Task ReloadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new bird and updates the store accordingly.
        /// Handles offline recovery if the store is not yet initialized.
        /// </summary>
        Task<Result<BirdDTO>> AddAsync(BirdCreateDTO newBird, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing bird and refreshes the collection if required.
        /// </summary>
        Task<Result> UpdateAsync(BirdDTO updatedBird, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a bird by ID and refreshes the collection if required.
        /// </summary>
        Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the current collection of birds (shared store reference).
        /// </summary>
        IBirdStore Store { get; }
    }
}

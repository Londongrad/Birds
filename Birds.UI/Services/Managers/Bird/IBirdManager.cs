using System.ComponentModel;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.UI.Services.Stores.BirdStore;

namespace Birds.UI.Services.Managers.Bird;

/// <summary>
///     Provides high-level operations for managing the bird collection.
///     Acts as a facade over <see cref="IBirdStore" />, <see cref="BirdStoreInitializer" />,
///     and MediatR commands for create/update/delete.
/// </summary>
public interface IBirdManager : INotifyPropertyChanged
{
    /// <summary>
    ///     Indicates that a recent delete operation can still be undone.
    /// </summary>
    bool HasPendingDeleteUndo { get; }

    /// <summary>
    ///     A monotonic counter that changes whenever a new undo window is opened.
    /// </summary>
    int PendingDeleteUndoVersion { get; }

    /// <summary>
    ///     The amount of time the undo affordance stays active.
    /// </summary>
    TimeSpan PendingDeleteUndoDuration { get; }

    /// <summary>
    ///     Gets the current collection of birds (shared store reference).
    /// </summary>
    IBirdStore Store { get; }

    /// <summary>
    ///     Reloads the entire bird collection from the database.
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Adds a new bird and updates the store accordingly.
    ///     Handles offline recovery if the store is not yet initialized.
    /// </summary>
    Task<Result<BirdDTO>> AddAsync(BirdCreateDTO newBird, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing bird and refreshes the collection if required.
    /// </summary>
    Task<Result<BirdDTO>> UpdateAsync(BirdUpdateDTO updatedBird, CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes a bird by ID and refreshes the collection if required.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    ///     Restores the most recently deleted bird while the undo window is still active.
    /// </summary>
    Task UndoPendingDeleteAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Forces any deferred operations, such as pending deletes, to be committed immediately.
    /// </summary>
    Task FlushPendingOperationsAsync(CancellationToken cancellationToken);
}
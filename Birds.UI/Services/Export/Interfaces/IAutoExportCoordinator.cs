namespace Birds.UI.Services.Export.Interfaces
{
    /// <summary>
    /// Coordinates background auto-export of the current bird snapshot.
    /// </summary>
    public interface IAutoExportCoordinator
    {
        /// <summary>
        /// Marks the current snapshot as changed and schedules a debounced export.
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// Flushes the latest snapshot to disk immediately.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken);
    }
}

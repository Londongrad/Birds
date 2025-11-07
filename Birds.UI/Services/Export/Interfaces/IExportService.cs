using Birds.Application.DTOs;

namespace Birds.UI.Services.Export.Interfaces
{
    /// <summary>
    /// Serializes and writes a bird collection to a JSON file.
    /// Implementations should ensure the write is safe and does not produce partial files.
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports <paramref name="birds"/> to <paramref name="path"/> in JSON format.
        /// </summary>
        /// <param name="birds">Sequence of birds to export.</param>
        /// <param name="path">Absolute target file path.</param>
        /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
        /// <returns>A task that completes when the export has finished.</returns>
        /// <remarks>
        /// Recommended behavior is to write to a temporary file and then replace the target file
        /// to avoid leaving a half-written file if the process crashes mid-write.
        /// </remarks>
        Task ExportAsync(IEnumerable<BirdDTO> birds, string path, CancellationToken cancellationToken);
    }
}

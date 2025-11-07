namespace Birds.UI.Services.Export.Interfaces
{
    /// <summary>
    /// Provides target file paths for JSON exports.
    /// Implementations decide where exports are written (e.g., project folder in Development,
    /// %LOCALAPPDATA% in Production) and typically return a single, stable file path so that
    /// subsequent exports overwrite the same file.
    /// </summary>
    public interface IExportPathProvider
    {
        /// <summary>
        /// Returns an absolute path for the export file. Implementations may ignore <paramref name="ext"/>
        /// or <paramref name="baseName"/> if they enforce a fixed naming policy.
        /// </summary>
        /// <param name="baseName">Logical base name for the file (e.g., "birds").</param>
        /// <param name="ext">File extension without dot (default: "json").</param>
        /// <returns>Absolute file path to write the export to.</returns>
        string GetLatestPath(string baseName, string ext = "json");
    }
}

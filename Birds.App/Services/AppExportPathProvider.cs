using Birds.UI.Services.Export.Interfaces;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Birds.App.Services
{
    /// <summary>
    /// Provides an absolute path for data exports based on the current hosting environment.
    /// In Development, exports are written under the project folder (<c>&lt;project&gt;\exports</c>).
    /// In Production, exports are written under <c>%LOCALAPPDATA%\Birds\exports</c>.
    /// </summary>
    internal sealed class AppExportPathProvider : IExportPathProvider
    {
        private readonly string _exportsDir;

        /// <summary>
        /// Creates a new instance that resolves the export directory from <see cref="IHostEnvironment"/>.
        /// </summary>
        /// <param name="env">
        /// Current host environment. Used to choose between the project content root (Development)
        /// and the per-user application data folder (Production).
        /// </param>
        public AppExportPathProvider(IHostEnvironment env)
        {
            // Dev → project root; Prod → %LOCALAPPDATA%\Birds
            var root = env.IsDevelopment()
                ? env.ContentRootPath
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Birds");

            _exportsDir = Path.Combine(root, "exports");

            // Ensure the target directory exists (no-op if already present).
            Directory.CreateDirectory(_exportsDir);
        }

        /// <summary>
        /// Returns a stable, absolute path for the export file. Subsequent calls with the same
        /// parameters will point to the same location so the export can overwrite the file.
        /// </summary>
        /// <param name="baseName">Logical base name (e.g., "birds").</param>
        /// <param name="ext">File extension without a leading dot (default: "json").</param>
        /// <returns>
        /// Absolute path in the resolved export directory, e.g., <c>.../exports/birds.json</c>.
        /// </returns>
        public string GetLatestPath(string baseName, string ext = "json")
            => Path.Combine(_exportsDir, $"{baseName}.{ext}");
    }
}

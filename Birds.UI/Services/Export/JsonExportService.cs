using Birds.Application.DTOs;
using Birds.UI.Services.Export.Interfaces;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Birds.UI.Services.Export
{
    /// <summary>
    /// JSON export service based on <see cref="System.Text.Json"/> that writes pretty-printed JSON,
    /// omits nulls, and preserves readable Cyrillic characters (no \uXXXX escapes).
    /// Uses a temp file + replace strategy for an atomic-looking update of the destination file.
    /// </summary>
    public sealed class JsonExportService : IExportService
    {
        // Precomputed serializer options:
        //  - WriteIndented: human-friendly file
        //  - Ignore nulls: cleaner payloads
        //  - Encoder: keep Latin + Cyrillic readable instead of \uXXXX escaping
        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            // Alternative: Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping (allows all, less strict)
        };

        /// <inheritdoc />
        public async Task ExportAsync(IEnumerable<BirdDTO> birds, string path, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(birds);
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path must be a non-empty absolute path.", nameof(path));

            // Ensure target directory exists
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);

            ct.ThrowIfCancellationRequested();

            // Write to a unique temp file first to avoid partial updates if the app crashes mid-write
            var tmp = Path.Combine(dir, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Envelope allows versioning and export metadata evolution without breaking consumers
                var envelope = new
                {
                    version = 1,
                    exportedAt = DateTimeOffset.UtcNow,
                    items = birds.ToList()
                };

                await JsonSerializer.SerializeAsync(fs, envelope, _opts, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
            }

            // Replace (overwrite) the destination with the fully written temp file.
            // On Windows this is effectively atomic for most practical purposes.
            File.Move(tmp, path, overwrite: true);
        }
    }
}

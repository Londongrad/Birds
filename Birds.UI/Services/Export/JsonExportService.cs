using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Birds.Application.DTOs;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Serialization;
using Microsoft.Extensions.Logging;

namespace Birds.UI.Services.Export;

/// <summary>
///     JSON export service based on <see cref="System.Text.Json" /> that writes pretty-printed JSON,
///     omits nulls, and preserves readable Cyrillic characters (no \uXXXX escapes).
/// </summary>
public sealed class JsonExportService(ILogger<JsonExportService>? logger = null) : IExportService
{
    private const int CurrentFormatVersion = 1;

    // Precomputed serializer options:
    //  - WriteIndented: human-friendly file
    //  - Ignore nulls: cleaner payloads
    //  - Encoder: keep Latin + Cyrillic readable instead of \uXXXX escaping
    private static readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        Converters = { new BirdSpeciesJsonConverter() }
    };

    /// <inheritdoc />
    public async Task ExportAsync(IEnumerable<BirdDTO> birds, string path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(birds);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must be a non-empty absolute path.", nameof(path));

        ct.ThrowIfCancellationRequested();

        var finalPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(finalPath);
        if (string.IsNullOrWhiteSpace(dir))
            throw new ArgumentException("Path must include a target directory.", nameof(path));

        var itemCount = 0;
        string? tmp = null;
        try
        {
            var items = birds.ToList();
            itemCount = items.Count;

            Directory.CreateDirectory(dir);

            tmp = Path.Combine(dir, $"{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");
            await using (var fs = new FileStream(tmp, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 81920,
                Options = FileOptions.WriteThrough
            }))
            {
                var exportedAtUtc = DateTime.UtcNow;

                // Keep legacy fields while adding explicit current metadata.
                var envelope = new
                {
                    formatVersion = CurrentFormatVersion,
                    version = CurrentFormatVersion,
                    exportedAtUtc,
                    exportedAt = exportedAtUtc,
                    appVersion = ResolveAppVersion(),
                    itemCount,
                    items
                };

                await JsonSerializer.SerializeAsync(fs, envelope, _opts, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
                fs.Flush(true);
            }

            CommitTempFile(tmp, finalPath);
            tmp = null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to export bird archive to {ExportPath}. Item count: {ItemCount}.",
                finalPath,
                itemCount);

            throw;
        }
        finally
        {
            TryDeleteTempFile(tmp);
        }
    }

    private static void CommitTempFile(string tempPath, string finalPath)
    {
        if (!File.Exists(finalPath))
        {
            File.Move(tempPath, finalPath);
            return;
        }

        var backupPath = $"{finalPath}.bak";
        if (File.Exists(backupPath))
            File.Delete(backupPath);

        File.Replace(tempPath, finalPath, backupPath, true);
    }

    private static void TryDeleteTempFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ResolveAppVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
               ?? typeof(JsonExportService).Assembly.GetName().Version?.ToString()
               ?? "unknown";
    }
}

using System.IO;
using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Shared.Constants;
using Birds.UI.Services.Import.Interfaces;

namespace Birds.UI.Services.Import;

public sealed class JsonImportService : IImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<IReadOnlyList<BirdDTO>>> ImportAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.ImportPathCannotBeEmpty);

        if (!File.Exists(path))
            return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.ImportFileNotFound(path));

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var envelope =
                await JsonSerializer.DeserializeAsync<ExportEnvelope>(stream, JsonOptions, cancellationToken);

            if (envelope is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.InvalidImportFileFormat);

            if (envelope.Version != 1)
                return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.UnsupportedImportVersion(envelope.Version));

            if (envelope.Items is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.ImportPayloadCannotBeNull);

            return Result<IReadOnlyList<BirdDTO>>.Success(envelope.Items);
        }
        catch (JsonException)
        {
            return Result<IReadOnlyList<BirdDTO>>.Failure(ErrorMessages.InvalidImportFileFormat);
        }
        catch (IOException ex)
        {
            return Result<IReadOnlyList<BirdDTO>>.Failure(ex.Message);
        }
    }

    private sealed record ExportEnvelope(int Version, DateTime? ExportedAt, IReadOnlyList<BirdDTO>? Items);
}
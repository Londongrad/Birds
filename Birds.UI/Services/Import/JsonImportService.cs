using System.IO;
using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Shared.Constants;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Serialization;

namespace Birds.UI.Services.Import;

public sealed class JsonImportService : IImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new BirdSpeciesJsonConverter() }
    };

    public async Task<Result<IReadOnlyList<BirdDTO>>> ImportAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.ImportPathCannotBeEmpty, AppErrorCodes.ImportInvalidFile));

        if (!File.Exists(path))
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.ImportFileNotFound(path), AppErrorCodes.ImportInvalidFile));

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var envelope =
                await JsonSerializer.DeserializeAsync<ExportEnvelope>(stream, JsonOptions, cancellationToken);

            if (envelope is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure(
                    AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));

            if (envelope.Version != 1)
                return Result<IReadOnlyList<BirdDTO>>.Failure(
                    AppErrors.Import(
                        ErrorMessages.UnsupportedImportVersion(envelope.Version),
                        AppErrorCodes.ImportInvalidFile));

            if (envelope.Items is null)
                return Result<IReadOnlyList<BirdDTO>>.Failure(
                    AppErrors.Import(ErrorMessages.ImportPayloadCannotBeNull, AppErrorCodes.ImportInvalidPayload));

            return Result<IReadOnlyList<BirdDTO>>.Success(envelope.Items);
        }
        catch (JsonException)
        {
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));
        }
        catch (IOException ex)
        {
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ex.Message, AppErrorCodes.ImportInvalidFile));
        }
    }

    private sealed record ExportEnvelope(int Version, DateTime? ExportedAt, IReadOnlyList<BirdDTO>? Items);
}

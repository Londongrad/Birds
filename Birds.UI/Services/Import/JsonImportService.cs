using System.IO;
using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Common.Exceptions;
using Birds.Domain.Entities;
using Birds.Shared.Constants;
using Birds.UI.Services.Import.Interfaces;
using Birds.UI.Services.Serialization;
using Microsoft.Extensions.Logging;

namespace Birds.UI.Services.Import;

public sealed class JsonImportService(ILogger<JsonImportService>? logger = null) : IImportService
{
    private const int CurrentFormatVersion = 1;

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
            await using var stream = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                BufferSize = 81920,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return ParseAndValidate(document.RootElement, path);
        }
        catch (JsonException ex) when (IsInvalidSpeciesJsonException(ex))
        {
            logger?.LogWarning(ex, "Rejected bird import archive {ImportPath}: invalid species value.", path);
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportedBirdSpecies, AppErrorCodes.ImportInvalidSpecies));
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Failed to parse bird import archive {ImportPath}.", path);
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));
        }
        catch (IOException ex)
        {
            logger?.LogWarning(ex, "Failed to read bird import archive {ImportPath}.", path);
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));
        }
    }

    private Result<IReadOnlyList<BirdDTO>> ParseAndValidate(JsonElement root, string path)
    {
        var parseResult = ParseArchive(root);
        if (!parseResult.IsSuccess)
        {
            logger?.LogWarning(
                "Rejected bird import archive {ImportPath}: {ErrorCode}.",
                path,
                parseResult.ErrorCode);

            return Result<IReadOnlyList<BirdDTO>>.Failure(parseResult.AppError!);
        }

        var archive = parseResult.Value!;
        var validationResult = ValidateItems(archive.Items, archive.ItemCount);
        if (!validationResult.IsSuccess)
        {
            logger?.LogWarning(
                "Rejected bird import archive {ImportPath}. Version: {FormatVersion}. Item count: {ItemCount}. Error: {ErrorCode}.",
                path,
                archive.Version,
                archive.Items.Count,
                validationResult.ErrorCode);

            return validationResult;
        }

        return Result<IReadOnlyList<BirdDTO>>.Success(archive.Items);
    }

    private static Result<ArchivePayload> ParseArchive(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            var legacyItems = root.Deserialize<IReadOnlyList<BirdDTO>>(JsonOptions);
            if (legacyItems is null)
                return Result<ArchivePayload>.Failure(
                    AppErrors.Import(ErrorMessages.ImportPayloadCannotBeNull, AppErrorCodes.ImportInvalidPayload));

            return Result<ArchivePayload>.Success(new ArchivePayload(CurrentFormatVersion, null, legacyItems));
        }

        if (root.ValueKind != JsonValueKind.Object)
            return Result<ArchivePayload>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));

        var envelope = root.Deserialize<ExportEnvelope>(JsonOptions);
        if (envelope is null)
            return Result<ArchivePayload>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));

        var version = envelope.FormatVersion ?? envelope.Version ?? CurrentFormatVersion;
        if (version > CurrentFormatVersion)
            return Result<ArchivePayload>.Failure(
                AppErrors.Import(
                    ErrorMessages.UnsupportedImportVersion(version),
                    AppErrorCodes.ImportUnsupportedVersion));

        if (version < 1)
            return Result<ArchivePayload>.Failure(
                AppErrors.Import(ErrorMessages.InvalidImportFileFormat, AppErrorCodes.ImportInvalidFile));

        if (envelope.Items is null)
            return Result<ArchivePayload>.Failure(
                AppErrors.Import(ErrorMessages.ImportPayloadCannotBeNull, AppErrorCodes.ImportInvalidPayload));

        return Result<ArchivePayload>.Success(new ArchivePayload(version, envelope.ItemCount, envelope.Items));
    }

    private static Result<IReadOnlyList<BirdDTO>> ValidateItems(IReadOnlyList<BirdDTO> items, int? expectedItemCount)
    {
        if (expectedItemCount.HasValue && expectedItemCount.Value != items.Count)
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Validation(
                    ErrorMessages.ImportValidationFailed,
                    new Dictionary<string, string[]>
                    {
                        ["itemCount"] =
                        [
                            $"Expected {expectedItemCount.Value} item(s), but found {items.Count}."
                        ]
                    },
                    AppErrorCodes.ImportValidationFailed));

        var duplicateId = items
            .GroupBy(static bird => bird.Id)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;

        if (duplicateId.HasValue)
            return Result<IReadOnlyList<BirdDTO>>.Failure(
                AppErrors.Import(
                    ErrorMessages.ImportContainsDuplicateIds(duplicateId.Value),
                    AppErrorCodes.ImportDuplicateIds));

        for (var index = 0; index < items.Count; index++)
        {
            var dto = items[index];
            var species = dto.ResolveSpecies();
            if (!species.HasValue)
                return Result<IReadOnlyList<BirdDTO>>.Failure(
                    AppErrors.Import(
                        ErrorMessages.InvalidImportedBirdName(dto.Name ?? string.Empty),
                        AppErrorCodes.ImportInvalidSpecies));

            try
            {
                _ = Bird.Restore(
                    dto.Id,
                    species.Value,
                    dto.Description,
                    dto.Arrival,
                    dto.Departure,
                    dto.IsAlive,
                    dto.CreatedAt,
                    dto.UpdatedAt,
                    version: dto.Version);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Result<IReadOnlyList<BirdDTO>>.Failure(
                    AppErrors.Validation(
                        ErrorMessages.ImportValidationFailed,
                        new Dictionary<string, string[]>
                        {
                            [$"items[{index}]"] = [ex.Message]
                        },
                        AppErrorCodes.ImportValidationFailed));
            }
        }

        return Result<IReadOnlyList<BirdDTO>>.Success(items);
    }

    private static bool IsValidationException(Exception exception)
    {
        return exception is DomainValidationException or ArgumentOutOfRangeException or ArgumentException;
    }

    private static bool IsInvalidSpeciesJsonException(JsonException exception)
    {
        return exception.Message.Contains("bird species", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ArchivePayload(int Version, int? ItemCount, IReadOnlyList<BirdDTO> Items);

    private sealed record ExportEnvelope(
        int? FormatVersion,
        int? Version,
        DateTime? ExportedAtUtc,
        DateTime? ExportedAt,
        string? AppVersion,
        int? ItemCount,
        IReadOnlyList<BirdDTO>? Items);
}

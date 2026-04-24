using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Mappings;
using Birds.Domain.Common.Exceptions;
using Birds.Domain.Entities;
using Birds.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Birds.Application.Commands.ImportBirds;

public sealed class ImportBirdsCommandHandler(
    IBirdRepository repository,
    ILogger<ImportBirdsCommandHandler>? logger = null)
    : IRequestHandler<ImportBirdsCommand, Result<BirdImportResultDTO>>
{
    public async Task<Result<BirdImportResultDTO>> Handle(
        ImportBirdsCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Result<BirdImportResultDTO>.Failure(AppErrors.InvalidRequest(ErrorMessages.RequestCannotBeNull));

        if (request.Birds is null)
            return Result<BirdImportResultDTO>.Failure(
                AppErrors.Import(ErrorMessages.ImportPayloadCannotBeNull, AppErrorCodes.ImportInvalidPayload));

        if (request.Birds.Count == 0)
        {
            if (request.Mode == BirdImportMode.Replace)
            {
                var replaceResult = await repository.ReplaceWithSnapshotAsync(
                    Array.Empty<Bird>(),
                    cancellationToken);

                return Result<BirdImportResultDTO>.Success(
                    new BirdImportResultDTO(
                        0,
                        replaceResult.Added,
                        replaceResult.Updated,
                        replaceResult.Removed,
                        Array.Empty<BirdDTO>()));
            }

            return Result<BirdImportResultDTO>.Success(new BirdImportResultDTO(0, 0, 0, 0, Array.Empty<BirdDTO>()));
        }

        var duplicateId = request.Birds
            .GroupBy(static bird => bird.Id)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;

        if (duplicateId.HasValue)
            return Result<BirdImportResultDTO>.Failure(
                AppErrors.Import(
                    ErrorMessages.ImportContainsDuplicateIds(duplicateId.Value),
                    AppErrorCodes.ImportDuplicateIds));

        var restoredBirds = new List<Bird>(request.Birds.Count);

        foreach (var dto in request.Birds)
        {
            var species = dto.ResolveSpecies();
            if (!species.HasValue)
                return Result<BirdImportResultDTO>.Failure(
                    AppErrors.Import(
                        ErrorMessages.InvalidImportedBirdName(dto.Name),
                        AppErrorCodes.ImportInvalidSpecies));

            try
            {
                restoredBirds.Add(Bird.Restore(
                    dto.Id,
                    species.Value,
                    dto.Description,
                    dto.Arrival,
                    dto.Departure,
                    dto.IsAlive,
                    dto.CreatedAt,
                    dto.UpdatedAt,
                    version: dto.Version));
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                logger?.LogWarning(
                    ex,
                    "Rejected invalid imported bird {BirdId}.",
                    dto.Id);

                return Result<BirdImportResultDTO>.Failure(
                    AppErrors.Validation(
                        ErrorMessages.ImportValidationFailed,
                        new Dictionary<string, string[]>
                        {
                            [nameof(BirdDTO)] = [ex.Message]
                        },
                        AppErrorCodes.ImportValidationFailed));
            }
        }

        UpsertBirdsResult upsertResult;
        try
        {
            upsertResult = request.Mode == BirdImportMode.Replace
                ? await repository.ReplaceWithSnapshotAsync(restoredBirds, cancellationToken)
                : await repository.UpsertAsync(restoredBirds, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Failed to apply bird import in {ImportMode} mode for {BirdCount} bird(s).",
                request.Mode,
                restoredBirds.Count);

            return Result<BirdImportResultDTO>.Failure(
                AppErrors.Import(
                    ErrorMessages.ImportTransactionFailed,
                    AppErrorCodes.ImportTransactionFailed));
        }

        var snapshot = (await repository.GetAllAsync(cancellationToken)).ToDtos();

        return Result<BirdImportResultDTO>.Success(
            new BirdImportResultDTO(
                restoredBirds.Count,
                upsertResult.Added,
                upsertResult.Updated,
                upsertResult.Removed,
                snapshot));
    }

    private static bool IsValidationException(Exception exception)
    {
        return exception is DomainValidationException or ArgumentOutOfRangeException or ArgumentException;
    }
}

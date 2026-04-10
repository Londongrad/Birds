using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Application.Interfaces;
using Birds.Application.Mappings;
using Birds.Domain.Entities;
using Birds.Shared.Constants;
using MediatR;

namespace Birds.Application.Commands.ImportBirds
{
    public sealed class ImportBirdsCommandHandler(IBirdRepository repository)
        : IRequestHandler<ImportBirdsCommand, Result<BirdImportResultDTO>>
    {
        public async Task<Result<BirdImportResultDTO>> Handle(
            ImportBirdsCommand request,
            CancellationToken cancellationToken)
        {
            if (request is null)
                return Result<BirdImportResultDTO>.Failure(ErrorMessages.RequestCannotBeNull);

            if (request.Birds is null)
                return Result<BirdImportResultDTO>.Failure(ErrorMessages.ImportPayloadCannotBeNull);

            if (request.Birds.Count == 0)
                return Result<BirdImportResultDTO>.Success(new BirdImportResultDTO(0, 0, 0, Array.Empty<BirdDTO>()));

            var duplicateId = request.Birds
                .GroupBy(static bird => bird.Id)
                .FirstOrDefault(group => group.Count() > 1)?
                .Key;

            if (duplicateId.HasValue)
                return Result<BirdImportResultDTO>.Failure(ErrorMessages.ImportContainsDuplicateIds(duplicateId.Value));

            var restoredBirds = new List<Bird>(request.Birds.Count);

            foreach (var dto in request.Birds)
            {
                var parsedName = BirdEnumHelper.ParseBirdName(dto.Name);
                if (!parsedName.HasValue)
                    return Result<BirdImportResultDTO>.Failure(ErrorMessages.InvalidImportedBirdName(dto.Name));

                restoredBirds.Add(Bird.Restore(
                    dto.Id,
                    parsedName.Value,
                    dto.Description,
                    dto.Arrival,
                    dto.Departure,
                    dto.IsAlive,
                    dto.CreatedAt,
                    dto.UpdatedAt));
            }

            var upsertResult = await repository.UpsertAsync(restoredBirds, cancellationToken);
            var snapshot = (await repository.GetAllAsync(cancellationToken)).ToDtos();

            return Result<BirdImportResultDTO>.Success(
                new BirdImportResultDTO(
                    restoredBirds.Count,
                    upsertResult.Added,
                    upsertResult.Updated,
                    snapshot));
        }
    }
}

using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Application.Mappings;
using Birds.Shared.Constants;
using MediatR;

namespace Birds.Application.Commands.UpdateBird;

public class UpdateBirdCommandHandler(IBirdRepository birdRepository)
    : IRequestHandler<UpdateBirdCommand, Result<BirdDTO>>
{
    public async Task<Result<BirdDTO>> Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
            return Result<BirdDTO>.Failure(ErrorMessages.RequestCannotBeNull);

        BirdDTO birdDTO;
        try
        {
            var bird = await birdRepository.UpdateAsync(
                request.Id,
                request.Version,
                request.Name,
                request.Description,
                request.Arrival,
                request.Departure,
                request.IsAlive,
                cancellationToken);

            birdDTO = bird.ToDto();
        }
        catch (ConcurrencyConflictException)
        {
            return Result<BirdDTO>.Failure(ErrorMessages.BirdConcurrencyConflict);
        }

        return Result<BirdDTO>.Success(birdDTO);
    }
}

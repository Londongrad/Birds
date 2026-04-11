using Birds.Application.Common.Models;
using Birds.Application.DTOs;
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

        var bird = await birdRepository.GetByIdAsync(request.Id, cancellationToken);

        bird.Update(
            request.Name,
            request.Description,
            request.Arrival,
            request.Departure,
            request.IsAlive);

        await birdRepository.UpdateAsync(bird, cancellationToken);

        var birdDTO = bird.ToDto();

        return Result<BirdDTO>.Success(birdDTO);
    }
}
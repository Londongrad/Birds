using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Mappings;
using Birds.Shared.Constants;
using MediatR;

namespace Birds.Application.Commands.CreateBird;

public class CreateBirdCommandHandler(IBirdRepository birdRepository)
    : IRequestHandler<CreateBirdCommand, Result<BirdDTO>>
{
    public async Task<Result<BirdDTO>> Handle(CreateBirdCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
            return Result<BirdDTO>.Failure(AppErrors.InvalidRequest(ErrorMessages.RequestCannotBeNull));

        var bird = request.ToEntity();

        await birdRepository.AddAsync(bird, cancellationToken);

        var birdDTO = bird.ToDto();

        return Result<BirdDTO>.Success(birdDTO);
    }
}

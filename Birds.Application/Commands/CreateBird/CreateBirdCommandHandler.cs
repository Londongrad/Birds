using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public class CreateBirdCommandHandler(
        IBirdRepository birdRepository,
        IMapper mapper)
        : IRequestHandler<CreateBirdCommand, Result<BirdDTO>>
    {
        public async Task<Result<BirdDTO>> Handle(CreateBirdCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                return Result<BirdDTO>.Failure("Request cannot be null.");

            var bird = mapper.Map<Bird>(request);

            await birdRepository.AddAsync(bird, cancellationToken);

            var birdDTO = mapper.Map<BirdDTO>(bird);

            return Result<BirdDTO>.Success(birdDTO);
        }
    }
}
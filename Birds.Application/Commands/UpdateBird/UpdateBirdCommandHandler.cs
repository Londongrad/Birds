using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBird
{
    public class UpdateBirdCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        : IRequestHandler<UpdateBirdCommand, Result<BirdDTO>>
    {
        public async Task<Result<BirdDTO>> Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result<BirdDTO>.Failure("Request cannot be null");

            var bird = await unitOfWork.BirdRepository.GetByIdAsync(request.Id, cancellationToken);

            bird.Update(
                request.Name, 
                request.Description, 
                request.Arrival, 
                request.Departure, 
                request.IsAlive);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var birdDTO = mapper.Map<BirdDTO>(bird);

            return Result<BirdDTO>.Success(birdDTO);
        }
    }
}

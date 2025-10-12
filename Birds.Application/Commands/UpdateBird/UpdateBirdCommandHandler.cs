using AutoMapper;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Notifications;
using MediatR;

namespace Birds.Application.Commands.UpdateBird
{
    public class UpdateBirdCommandHandler(
        IBirdRepository repository,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IMapper mapper)
        : IRequestHandler<UpdateBirdCommand, Result>
    {
        public async Task<Result> Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request cannot be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.Update(request.Arrival, request.Departure, request.Description, request.IsAlive);
            bird.SetName(request.Name);

            repository.Update(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var birdDTO = mapper.Map<BirdDTO>(bird);
            await mediator.Publish(new BirdUpdatedNotification(birdDTO), cancellationToken);

            return Result.Success();
        }
    }
}

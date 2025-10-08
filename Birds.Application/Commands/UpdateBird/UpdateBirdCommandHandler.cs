using AutoMapper;
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
        : IRequestHandler<UpdateBirdCommand>
    {
        public async Task Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.Update(request.Arrival, request.Departure, request.Description, request.IsAlive);
            bird.SetName(request.Name);

            repository.Update(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var birdDTO = mapper.Map<BirdDTO>(bird);
            await mediator.Publish(new BirdUpdatedNotification(birdDTO), cancellationToken);
        }
    }
}

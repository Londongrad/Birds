using Birds.Application.Interfaces;
using Birds.Application.Notifications;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork, IMediator mediator)
        : IRequestHandler<DeleteBirdCommand>
    {
        public async Task Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            repository.Remove(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new BirdDeletedNotification(bird.Id), cancellationToken);
        }
    }
}
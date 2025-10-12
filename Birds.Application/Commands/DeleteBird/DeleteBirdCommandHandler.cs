using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using Birds.Application.Notifications;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork, IMediator mediator)
        : IRequestHandler<DeleteBirdCommand, Result>
    {
        public async Task<Result> Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request canno be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            repository.Remove(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new BirdDeletedNotification(bird.Id), cancellationToken);

            return Result.Success();
        }
    }
}
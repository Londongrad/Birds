using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdStatus
{
    public class UpdateBirdStatusCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdStatusCommand>
    {
        public async Task Handle(UpdateBirdStatusCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.UpdateStatus(request.IsAlive);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    internal class DeleteBirdCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteBirdCommand>
    {
        public async Task Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            repository.Remove(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(
        IBirdRepository repository, 
        IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteBirdCommand, Result>
    {
        public async Task<Result> Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request canno be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            repository.Remove(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
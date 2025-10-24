using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteBirdCommand, Result>
    {
        public async Task<Result> Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request canno be null");

            var bird = await unitOfWork.BirdRepository.GetByIdAsync(request.Id, cancellationToken);

            unitOfWork.BirdRepository.Remove(bird);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
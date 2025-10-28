using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(IBirdRepository birdRepository)
        : IRequestHandler<DeleteBirdCommand, Result>
    {
        public async Task<Result> Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request canno be null");

            var bird = await birdRepository.GetByIdAsync(request.Id, cancellationToken);

            await birdRepository.RemoveAsync(bird, cancellationToken);

            return Result.Success();
        }
    }
}
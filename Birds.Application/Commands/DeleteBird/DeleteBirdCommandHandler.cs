using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandler(IBirdRepository repository)
        : IRequestHandler<DeleteBirdCommand, Result>
    {
        public async Task<Result> Handle(DeleteBirdCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request canno be null");

            await repository.DeleteAsync(request.Id, cancellationToken);

            return Result.Success();
        }
    }
}
using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdStatus
{
    public class UpdateBirdStatusCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdStatusCommand, Result>
    {
        public async Task<Result> Handle(UpdateBirdStatusCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request cannot be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.UpdateStatus(request.IsAlive);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
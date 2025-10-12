using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdDescription
{
    public class UpdateBirdDescriptionCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdDescriptionCommand, Result>
    {
        public async Task<Result> Handle(UpdateBirdDescriptionCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request cannot be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.SetDescription(request.Description);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
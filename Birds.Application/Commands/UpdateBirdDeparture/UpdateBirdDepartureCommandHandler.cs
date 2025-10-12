using Birds.Application.Common.Models;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdDeparture
{
    public class UpdateBirdDepartureCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdDepartureCommand, Result>
    {
        public async Task<Result> Handle(UpdateBirdDepartureCommand request, CancellationToken cancellationToken)
        {
            if (request is null)
                return Result.Failure("Request cannot be null");

            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            if (request.Departure.HasValue)
                bird.SetDeparture(request.Departure.Value);
            else
                bird.ClearDeparture();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
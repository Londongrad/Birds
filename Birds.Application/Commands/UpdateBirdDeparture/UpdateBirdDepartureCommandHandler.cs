using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdDeparture
{
    public class UpdateBirdDepartureCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdDepartureCommand>
    {
        public async Task Handle(UpdateBirdDepartureCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            if (request.Departure.HasValue)
                bird.SetDeparture(request.Departure.Value);
            else
                bird.ClearDeparture();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

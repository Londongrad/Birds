using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBirdDescription
{
    public class UpdateBirdDescriptionCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateBirdDescriptionCommand>
    {
        public async Task Handle(UpdateBirdDescriptionCommand request, CancellationToken cancellationToken)
        {
            var bird = await repository.GetByIdAsync(request.Id, cancellationToken);

            bird.SetDescription(request.Description);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

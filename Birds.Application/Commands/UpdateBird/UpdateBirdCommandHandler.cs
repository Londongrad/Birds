using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Commands.UpdateBird
{
    public class UpdateBirdCommandHandler(
        IBirdRepository repository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
        : IRequestHandler<UpdateBirdCommand>
    {
        public async Task Handle(UpdateBirdCommand request, CancellationToken cancellationToken)
        {
            
        }
    }
}

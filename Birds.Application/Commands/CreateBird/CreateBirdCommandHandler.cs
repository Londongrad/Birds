using AutoMapper;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Notifications;
using Birds.Domain.Entities;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public class CreateBirdCommandHandler(
        IBirdRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMediator mediator)
        : IRequestHandler<CreateBirdCommand>
    {
        public async Task Handle(CreateBirdCommand request, CancellationToken cancellationToken)
        {
            var bird = mapper.Map<Bird>(request);

            await repository.AddAsync(bird, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var birdDTO = mapper.Map<BirdDTO>(bird);
            await mediator.Publish(new BirdCreatedNotification(birdDTO), cancellationToken);
        }
    }
}
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
        : IRequestHandler<CreateBirdCommand, Guid>
    {
        public async Task<Guid> Handle(CreateBirdCommand request, CancellationToken cancellationToken)
        {
            var id = await repository.AddAsync(mapper.Map<Bird>(request), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var bird = mapper.Map<BirdDTO>(request);
            await mediator.Publish(new BirdCreatedNotification(bird), cancellationToken);

            return id;
        }
    }
}

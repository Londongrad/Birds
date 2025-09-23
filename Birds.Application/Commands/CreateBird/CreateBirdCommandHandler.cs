using AutoMapper;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public class CreateBirdCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        : IRequestHandler<CreateBirdCommand, Guid>
    {
        public async Task<Guid> Handle(CreateBirdCommand request, CancellationToken cancellationToken)
        {
            var id = await repository.AddAsync(mapper.Map<Bird>(request), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return id;
        }
    }
}

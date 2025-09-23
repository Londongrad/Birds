using AutoMapper;
using Birds.Application.Interfaces;
using Birds.Domain.Common;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using MediatR;

namespace Birds.Application.Commands.CreateBird
{
    public class CreateBirdCommandHandler(IBirdRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        : IRequestHandler<CreateBirdCommand, Guid>
    {
        private readonly IBirdRepository _repository = repository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;

        public async Task<Guid> Handle(CreateBirdCommand request, CancellationToken cancellationToken)
        {
            GuardHelper.AgainstInvalidDate(request.Dto.Arrival, nameof(request.Dto.Arrival));
            GuardHelper.AgainstInvalidStringToEnum<BirdsName>(request.Dto.Name, nameof(request.Dto.Name));

            var id = await _repository.AddAsync(_mapper.Map<Bird>(request.Dto), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return id;
        }
    }
}

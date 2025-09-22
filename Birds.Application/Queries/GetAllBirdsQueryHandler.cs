using AutoMapper;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Queries
{
    public class GetAllBirdsQueryHandler(IBirdRepository repository, IMapper mapper)
        : IRequestHandler<GetAllBirdsQuery, IReadOnlyList<BirdDTO>>
    {
        private readonly IBirdRepository _repository = repository;
        private readonly IMapper _mapper = mapper;

        public async Task<IReadOnlyList<BirdDTO>> Handle(GetAllBirdsQuery query, CancellationToken cancellationToken = default)
        {
            var birds = await _repository.GetAllAsync(cancellationToken);

            return _mapper.Map<IReadOnlyList<BirdDTO>>(birds);
        }
    }
}

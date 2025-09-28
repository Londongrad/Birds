using AutoMapper;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using MediatR;

namespace Birds.Application.Queries.GetAllBirds
{
    public class GetAllBirdsQueryHandler(IBirdRepository repository, IMapper mapper)
        : IRequestHandler<GetAllBirdsQuery, IReadOnlyList<BirdDTO>>
    {
        public async Task<IReadOnlyList<BirdDTO>> Handle(GetAllBirdsQuery query, CancellationToken cancellationToken = default)
        {
            var birds = await repository.GetAllAsync(cancellationToken);

            return mapper.Map<IReadOnlyList<BirdDTO>>(birds);
        }
    }
}
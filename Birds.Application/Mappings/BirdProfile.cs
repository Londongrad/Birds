using AutoMapper;
using Birds.Application.DTOs;
using Birds.Domain.Entities;
using Birds.Domain.Enums;

namespace Birds.Application.Mappings
{
    public class BirdProfile : Profile
    {
        public BirdProfile()
        {
            CreateMap<Bird, BirdDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.ToString()));

            CreateMap<CreateBirdDTO, Bird>()
            .ConstructUsing(dto => new Bird(
                Guid.NewGuid(),
                Enum.Parse<BirdsName>(dto.Name, true),
                dto.Description,
                dto.Arrival,
                true
            ));
        }
    }
}

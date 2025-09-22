using AutoMapper;
using Birds.Application.DTOs;
using Birds.Domain.Entities;

namespace Birds.Application.Mappings
{
    public class BirdProfile : Profile
    {
        public BirdProfile()
        {
            CreateMap<Bird, BirdDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.ToString()));
        }
    }
}

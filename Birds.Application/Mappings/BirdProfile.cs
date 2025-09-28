using AutoMapper;
using Birds.Application.Commands.CreateBird;
using Birds.Application.DTOs;
using Birds.Domain.Entities;
using Birds.Domain.Extensions;

namespace Birds.Application.Mappings
{
    public class BirdProfile : Profile
    {
        public BirdProfile()
        {
            CreateMap<Bird, BirdDTO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.ToDisplayName()));

            CreateMap<CreateBirdCommand, Bird>()
                .ConstructUsing(cmd => new Bird(
                    Guid.NewGuid(),
                    cmd.Name,
                    cmd.Description,
                    cmd.Arrival,
                    true
                ));

            CreateMap<CreateBirdCommand, BirdDTO>()
                .ConstructUsing(cmd => new BirdDTO(
                    Guid.NewGuid(),
                    cmd.Name.ToString(),
                    cmd.Description,
                    cmd.Arrival,
                    null,
                    true
                ));
        }
    }
}
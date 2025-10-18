using AutoMapper;
using Birds.Application.Commands.CreateBird;
using Birds.Application.Commands.UpdateBird;
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
                .ConstructUsing(cmd => Bird.Create(
                    cmd.Name,
                    cmd.Description,
                    cmd.Arrival,
                    true
                ));

            CreateMap<UpdateBirdCommand, Bird>()
            .ConstructUsing(cmd => Bird.Restore(
                cmd.Id,
                cmd.Name,
                cmd.Description,
                cmd.Arrival,
                cmd.IsAlive
            ));
        }
    }
}
using AutoMapper;
using Birds.Application.Commands.CreateBird;
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

            CreateMap<CreateBirdCommand, Bird>()
                .ConstructUsing(cmd => new Bird(
                    Guid.NewGuid(),
                    Enum.Parse<BirdsName>(cmd.Name, true), // Безопасно, т.к. валидируется в CreateBirdCommandValidator
                    cmd.Description,
                    cmd.Arrival,
                    true
                ));
        }
    }
}

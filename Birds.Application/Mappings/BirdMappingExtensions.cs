using Birds.Application.Commands.CreateBird;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Domain.Entities;

namespace Birds.Application.Mappings;

internal static class BirdMappingExtensions
{
    public static Bird ToEntity(this CreateBirdCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return Bird.Create(
            command.Name,
            command.Description,
            command.Arrival,
            command.Departure,
            command.IsAlive);
    }

    public static BirdDTO ToDto(this Bird bird)
    {
        ArgumentNullException.ThrowIfNull(bird);

        return new BirdDTO(
            bird.Id,
            BirdNameDisplayNames.GetDisplayName(bird.Name),
            bird.Description,
            bird.Arrival,
            bird.Departure,
            bird.IsAlive,
            bird.CreatedAt,
            bird.UpdatedAt);
    }

    public static IReadOnlyList<BirdDTO> ToDtos(this IEnumerable<Bird> birds)
    {
        ArgumentNullException.ThrowIfNull(birds);

        return birds.Select(static bird => bird.ToDto()).ToArray();
    }
}

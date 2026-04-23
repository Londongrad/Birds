using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;

namespace Birds.Application.DTOs;

public record BirdDTO(
    Guid Id,
    string Name,
    string? Description,
    DateOnly Arrival,
    DateOnly? Departure,
    bool IsAlive,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
)
{
    public BirdSpecies Species { get; init; } = BirdEnumHelper.ParseBirdName(Name) ?? default;

    public BirdSpecies? ResolveSpecies()
    {
        return Enum.IsDefined(Species)
            ? Species
            : BirdEnumHelper.ParseBirdName(Name);
    }
}

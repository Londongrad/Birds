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
    public BirdsName Species { get; init; } = BirdEnumHelper.ParseBirdName(Name) ?? default;

    public BirdsName? ResolveSpecies()
    {
        return Enum.IsDefined(Species)
            ? Species
            : BirdEnumHelper.ParseBirdName(Name);
    }
}

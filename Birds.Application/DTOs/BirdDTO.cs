using Birds.Application.DTOs.Helpers;
using Birds.Domain.Common;
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
    public long Version { get; init; } = BirdValidationRules.MinimumVersion;

    public BirdSpecies? ResolveSpecies()
    {
        return Enum.IsDefined(Species)
            ? Species
            : BirdEnumHelper.ParseBirdName(Name);
    }
}

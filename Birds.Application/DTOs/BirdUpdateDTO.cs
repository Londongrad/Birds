using Birds.Domain.Common;
using Birds.Domain.Enums;

namespace Birds.Application.DTOs;

public record BirdUpdateDTO(
    Guid Id,
    BirdSpecies Species,
    string? Description,
    DateOnly Arrival,
    DateOnly? Departure,
    bool IsAlive,
    long Version = BirdValidationRules.MinimumVersion
);

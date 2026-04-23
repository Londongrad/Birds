using Birds.Domain.Enums;

namespace Birds.Application.DTOs;

public record BirdUpdateDTO(
    Guid Id,
    BirdsName Species,
    string? Description,
    DateOnly Arrival,
    DateOnly? Departure,
    bool IsAlive
);

using Birds.Domain.Enums;

namespace Birds.Application.DTOs
{
    /// <summary>
    /// Data transfer object used to create a new bird.
    /// Contains only fields required for creation.
    /// </summary>
    public record BirdCreateDTO(
        BirdsName Name,
        string? Description,
        DateOnly Arrival,
        DateOnly? Departure = null,
        bool IsAlive = true
    );
}

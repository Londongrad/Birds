namespace Birds.Application.DTOs
{
    public record BirdUpdateDTO(
        Guid Id,
        string Name,
        string? Description,
        DateOnly Arrival,
        DateOnly? Departure,
        bool IsAlive
        );
}

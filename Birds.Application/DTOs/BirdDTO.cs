namespace Birds.Application.DTOs
{
    public record BirdDTO(
        Guid Id,
        string Name,
        string? Description,
        DateOnly Arrival,
        DateOnly? Departure,
        bool IsAlive
    );
}

namespace Birds.Application.DTOs
{
    public record CreateBirdDTO(
        string Name,
        string? Description,
        DateOnly Arrival
    );
}

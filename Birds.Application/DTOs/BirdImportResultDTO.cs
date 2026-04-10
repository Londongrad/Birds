namespace Birds.Application.DTOs
{
    public sealed record BirdImportResultDTO(
        int Imported,
        int Added,
        int Updated,
        IReadOnlyList<BirdDTO> Snapshot);
}

namespace Birds.Infrastructure.Persistence.Models;

public sealed record BirdSyncPayload(
    Guid Id,
    string Name,
    string? Description,
    DateOnly Arrival,
    DateOnly? Departure,
    bool IsAlive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? SyncStampUtc);

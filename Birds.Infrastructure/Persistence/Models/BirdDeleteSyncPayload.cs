namespace Birds.Infrastructure.Persistence.Models;

public sealed record BirdDeleteSyncPayload(Guid Id, DateTime DeletedAtUtc);
namespace Birds.Infrastructure.Services;

public sealed record LocalStoreStateSnapshot(int BirdCount, int PendingOperationCount)
{
    public bool HasBirds => BirdCount > 0;

    public bool HasPendingOperations => PendingOperationCount > 0;

    public bool IsEmptyAndClean => BirdCount == 0 && PendingOperationCount == 0;
}
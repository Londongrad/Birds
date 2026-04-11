namespace Birds.Infrastructure.Services;

public interface ILocalStoreStateService
{
    Task<LocalStoreStateSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
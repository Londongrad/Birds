namespace Birds.Application.Interfaces
{
    public interface IDatabaseMaintenanceService
    {
        bool CanResetLocalDatabase { get; }

        Task<int> ClearBirdRecordsAsync(CancellationToken cancellationToken = default);

        Task ResetLocalDatabaseAsync(CancellationToken cancellationToken = default);
    }
}

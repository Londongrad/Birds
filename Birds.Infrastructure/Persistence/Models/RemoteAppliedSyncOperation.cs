namespace Birds.Infrastructure.Persistence.Models;

public sealed class RemoteAppliedSyncOperation
{
    private RemoteAppliedSyncOperation()
    {
    }

    public Guid OperationId { get; private set; }
    public SyncOperationType OperationType { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }

    public static RemoteAppliedSyncOperation Create(
        Guid operationId,
        SyncOperationType operationType,
        Guid entityId,
        DateTime appliedAtUtc)
    {
        return new RemoteAppliedSyncOperation
        {
            OperationId = operationId,
            OperationType = operationType,
            EntityId = entityId,
            AppliedAtUtc = UtcDateTimeStorage.NormalizeForStorage(appliedAtUtc)
        };
    }
}

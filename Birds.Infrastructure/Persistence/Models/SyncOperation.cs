namespace Birds.Infrastructure.Persistence.Models;

public sealed class SyncOperation
{
    private SyncOperation()
    {
    }

    public Guid Id { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public SyncOperationType OperationType { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public static SyncOperation CreatePending(string aggregateType,
        Guid aggregateId,
        SyncOperationType operationType,
        string payloadJson,
        DateTime timestampUtc)
    {
        return new SyncOperation
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            OperationType = operationType,
            PayloadJson = payloadJson,
            CreatedAtUtc = UtcDateTimeStorage.Normalize(timestampUtc),
            UpdatedAtUtc = UtcDateTimeStorage.Normalize(timestampUtc),
            RetryCount = 0,
            LastAttemptAtUtc = null,
            LastError = null
        };
    }

    public void ReplacePendingPayload(SyncOperationType operationType, string payloadJson, DateTime timestampUtc)
    {
        OperationType = operationType;
        PayloadJson = payloadJson;
        UpdatedAtUtc = UtcDateTimeStorage.Normalize(timestampUtc);
        RetryCount = 0;
        LastAttemptAtUtc = null;
        LastError = null;
    }

    public void MarkFailed(string errorMessage, DateTime attemptUtc)
    {
        RetryCount++;
        LastAttemptAtUtc = UtcDateTimeStorage.Normalize(attemptUtc);
        UpdatedAtUtc = UtcDateTimeStorage.Normalize(attemptUtc);
        LastError = string.IsNullOrWhiteSpace(errorMessage)
            ? null
            : errorMessage.Length <= 2048
                ? errorMessage
                : errorMessage[..2048];
    }
}

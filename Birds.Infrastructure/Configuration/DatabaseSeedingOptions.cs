namespace Birds.Infrastructure.Configuration;

public sealed class DatabaseSeedingOptions(
    DatabaseSeedingMode mode,
    int recordCount,
    int batchSize,
    int randomSeed)
{
    public DatabaseSeedingMode Mode { get; } = mode;

    public int RecordCount { get; } = recordCount;

    public int BatchSize { get; } = batchSize;

    public int RandomSeed { get; } = randomSeed;
}
namespace Birds.Infrastructure.Configuration;

public enum DatabaseSeedingMode
{
    None = 0,
    SeedIfEmpty = 1,
    RecreateAndSeed = 2
}
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Birds.Infrastructure.Seeding
{
    /// <summary>
    /// Generates deterministic demo data for local performance and UX checks.
    /// Intended for explicit seeding scenarios only.
    /// </summary>
    public sealed class BirdSeeder(
        IDbContextFactory<BirdDbContext> contextFactory,
        ILogger<BirdSeeder> logger)
    {
        private static readonly string[] DescriptionTemplates =
        [
            "Поступил на осмотр после краткого наблюдения в вольере.",
            "Активный, хорошо ест, заметных отклонений в поведении не выявлено.",
            "Требует наблюдения за динамикой веса и режима кормления.",
            "Переведен в общий блок после стабилизации состояния.",
            "Наблюдался в стационаре, затем подготовлен к выпуску."
        ];

        private readonly IDbContextFactory<BirdDbContext> _contextFactory = contextFactory;
        private readonly ILogger<BirdSeeder> _logger = logger;

        public async Task SeedAsync(DatabaseSeedingOptions options, CancellationToken cancellationToken)
        {
            var targetCount = Math.Max(0, options.RecordCount);
            if (targetCount == 0)
            {
                _logger.LogInformation("Database seeding skipped because record count is 0.");
                return;
            }

            var batchSize = Math.Max(1, options.BatchSize);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            if (options.Mode == DatabaseSeedingMode.SeedIfEmpty
                && await context.Birds.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Database seeding skipped because the Birds table already contains data.");
                return;
            }

            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var random = new Random(options.RandomSeed);
            var now = DateOnly.FromDateTime(DateTime.Today);
            var species = Enum.GetValues<BirdsName>();
            var counters = species.ToDictionary(x => x, _ => 0);
            var birds = new List<Bird>(batchSize);

            for (var index = 0; index < targetCount; index++)
            {
                var bird = CreateBird(random, species, counters, now);
                birds.Add(bird);

                if (birds.Count < batchSize)
                    continue;

                await SaveBatchAsync(context, birds, cancellationToken);
            }

            if (birds.Count > 0)
                await SaveBatchAsync(context, birds, cancellationToken);

            _logger.LogInformation(
                "Database seeding completed with {Count} generated bird records (batch size: {BatchSize}, seed: {Seed}).",
                targetCount,
                batchSize,
                options.RandomSeed);
        }

        private static Bird CreateBird(Random random,
                                       IReadOnlyList<BirdsName> species,
                                       IDictionary<BirdsName, int> counters,
                                       DateOnly now)
        {
            var birdName = species[random.Next(species.Count)];
            counters[birdName]++;

            var arrival = now.AddDays(-random.Next(0, 365 * 4));
            var statusRoll = random.Next(100);
            var stayLength = random.Next(0, Math.Max(1, now.DayNumber - arrival.DayNumber + 1));

            DateOnly? departure = statusRoll < 55
                ? null
                : arrival.AddDays(stayLength);

            var isAlive = statusRoll < 85;
            var description = BuildDescription(random, birdName, counters[birdName]);

            return Bird.Create(
                birdName,
                description,
                arrival,
                departure,
                isAlive);
        }

        private static string? BuildDescription(Random random, BirdsName birdName, int number)
        {
            if (random.Next(100) < 22)
                return null;

            var template = DescriptionTemplates[random.Next(DescriptionTemplates.Length)];
            return $"{birdName} #{number}. {template}";
        }

        private static async Task SaveBatchAsync(BirdDbContext context,
                                                 List<Bird> birds,
                                                 CancellationToken cancellationToken)
        {
            await context.Birds.AddRangeAsync(birds, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            birds.Clear();
        }
    }
}

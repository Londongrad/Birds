using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Birds.Infrastructure.Seeding
{
    public class BirdSeeder(IServiceProvider services) : IHostedService
    {
        private readonly IServiceProvider _services = services;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BirdDbContext>();

            //await context.Database.EnsureDeletedAsync();
            //await context.Database.EnsureCreatedAsync(cancellationToken);

            if (!await context.Birds.AnyAsync(cancellationToken))
            {
                var now = DateOnly.FromDateTime(DateTime.Now);
                var random = Random.Shared;
                var counters = Enum.GetValues<BirdsName>()
                    .ToDictionary(x => x, _ => 0);

                const int total = 3000;
                const int batchSize = 400;
                var birds = new List<Bird>(batchSize);

                for (int i = 0; i < total; i++)
                {
                    var birdName = (BirdsName)random.Next(1, 8);
                    var arrival = now.AddDays(-random.Next(0, 365 * 3));
                    var departure = arrival.AddDays(random.Next(0, (now.DayNumber - arrival.DayNumber) + 1));
                    var isAlive = random.Next(0, 2) == 1;

                    counters[birdName]++;
                    var number = counters[birdName];

                    var bird = Bird.Create(
                        birdName,
                        $"{birdName} #{number}",
                        arrival,
                        departure,
                        isAlive
                    );

                    birds.Add(bird);

                    // When batch is full, save to database
                    if (birds.Count >= batchSize)
                    {
                        await context.Birds.AddRangeAsync(birds, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                        birds.Clear();
                    }
                }

                // Last batch
                if (birds.Count > 0)
                {
                    await context.Birds.AddRangeAsync(birds, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
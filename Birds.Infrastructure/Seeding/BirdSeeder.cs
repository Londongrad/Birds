using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Infrastructure.Persistence;
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

            if (!context.Birds.Any())
            {
                var birds = new List<Bird>();
                var now = DateOnly.FromDateTime(DateTime.UtcNow);
                var random = new Random();

                // Счётчики для каждого вида
                var counters = Enum.GetValues<BirdsName>()
                    .ToDictionary(x => x, _ => 0);

                for (int i = 0; i < 1_000_000; i++)
                {
                    var birdName = (BirdsName)random.Next(1, 8);
                    var arrival = now.AddDays(-random.Next(0, 365 * 3));
                    var isAlive = random.Next(0, 2) == 1;

                    counters[birdName]++;
                    var number = counters[birdName];

                    var bird = new Bird(
                        Guid.NewGuid(),
                        birdName,
                        $"{birdName} #{number}",
                        arrival,
                        isAlive
                    );

                    if (!isAlive)
                    {
                        var departure = arrival.AddDays(random.Next(0, (now.DayNumber - arrival.DayNumber) + 1));
                        bird.SetDeparture(departure);
                    }

                    birds.Add(bird);
                }

                await context.Birds.AddRangeAsync(birds, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

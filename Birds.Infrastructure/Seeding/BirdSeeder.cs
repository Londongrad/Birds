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
        private Task? _seedingTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Запускаем сидинг в фоне
            _seedingTask = Task.Run(async () =>
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BirdDbContext>();

                //context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                if (!context.Birds.Any())
                {
                    var now = DateOnly.FromDateTime(DateTime.UtcNow);
                    var random = new Random();
                    var counters = Enum.GetValues<BirdsName>()
                        .ToDictionary(x => x, _ => 0);

                    const int total = 100;
                    const int batchSize = 10;
                    var birds = new List<Bird>(batchSize);

                    for (int i = 0; i < total; i++)
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

                        // Когда накопился батч — сохраняем
                        if (birds.Count >= batchSize)
                        {
                            await context.Birds.AddRangeAsync(birds, cancellationToken);
                            await context.SaveChangesAsync(cancellationToken);
                            birds.Clear();
                        }
                    }

                    // Последний хвостик
                    if (birds.Count > 0)
                    {
                        await context.Birds.AddRangeAsync(birds, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
            }, cancellationToken);

            // Возвращаем сразу, чтобы окно WPF показалось
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_seedingTask is not null)
                await _seedingTask;
        }
    }
}

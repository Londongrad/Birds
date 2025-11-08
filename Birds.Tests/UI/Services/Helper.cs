using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.Tests.Helpers;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Retry;

namespace Birds.Tests.UI.Services;

public static class TestHelpers
{
    // ---------- Policies ----------
    public static AsyncRetryPolicy<Result<IReadOnlyList<BirdDTO>>> RetryNoDelay(int retries) =>
        Policy.HandleResult<Result<IReadOnlyList<BirdDTO>>>(r => !r.IsSuccess)
              .WaitAndRetryAsync(retries, _ => TimeSpan.Zero);

    // ---------- Sample data ----------
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    public static BirdDTO Bird(Guid? id = null, string? name = "Воробей", string? desc = null,
                               DateOnly? arrival = null, DateOnly? departure = null, bool isAlive = true) =>
        new(id ?? Guid.NewGuid(), name!, desc, arrival ?? Today(), departure, isAlive, null, null);

    public static IReadOnlyList<BirdDTO> Birds(params BirdDTO[] items) =>
        (items.Length == 0
            ? new[] { Bird(name: "Sparrow", desc: "d"), Bird(name: "Tit", desc: "d") }
            : items).ToList().AsReadOnly();

    // ---------- Mediator setups ----------
    public static void SetupGetAllBirdsSuccess(this Mock<IMediator> mediator, IReadOnlyList<BirdDTO>? value = null) =>
        mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(value ?? Birds()));

    public static void SetupGetAllBirdsFailure(this Mock<IMediator> mediator, string error = "db down") =>
        mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure(error));

    public static void SetupGetAllBirdsSequence(this Mock<IMediator> mediator, params Result<IReadOnlyList<BirdDTO>>[] results)
    {
        var seq = mediator.SetupSequence(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()));
        foreach (var r in results) seq = seq.ReturnsAsync(r);
    }

    // ---------- Export mocks ----------
    public static (Mock<IExportService> export, Mock<IExportPathProvider> path) CreateExportMocks()
    {
        var export = new Mock<IExportService>();
        var path = new Mock<IExportPathProvider>();
        path.Setup(p => p.GetLatestPath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => Path.Combine(Path.GetTempPath(), $"birds-test-{Guid.NewGuid():N}.json"));
        return (export, path);
    }

    // ---------- SUT factories ----------
    public static BirdStoreInitializer MakeInitializer(
        IBirdStore store,
        IMediator mediator,
        out Mock<INotificationService> notify,
        out Mock<ILogger<BirdStoreInitializer>> logger,
        int retries = 4,
        Mock<IExportService>? export = null,
        Mock<IExportPathProvider>? exportPath = null)
    {
        logger = new Mock<ILogger<BirdStoreInitializer>>();
        notify = new Mock<INotificationService>();

        export ??= new Mock<IExportService>();
        exportPath ??= new Mock<IExportPathProvider>();
        exportPath.Setup(p => p.GetLatestPath(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(() => Path.Combine(Path.GetTempPath(), $"birds-test-{Guid.NewGuid():N}.json"));

        return new BirdStoreInitializer(
            store,
            mediator,
            logger.Object,
            notify.Object,
            exportService: export.Object,
            exportPathProvider: exportPath.Object,
            uiDispatcher: new InlineUiDispatcher(),
            retryPolicy: RetryNoDelay(retries));
    }
}

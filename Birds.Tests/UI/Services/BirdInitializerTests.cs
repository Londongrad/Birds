using System.Collections.Specialized;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.UI.Enums;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.UI.Services;

public class BirdInitializerTests
{
    [Fact]
    public async Task StartAsync_Success_PopulatesStoreAndLogs()
    {
        // Arrange
        var store = new BirdStore();
        var mediator = new Mock<IMediator>();
        var autoExport = new Mock<IAutoExportCoordinator>();
        var collectionChangedCount = 0;
        store.Birds.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                collectionChangedCount++;
        };

        mediator.SetupGetAllBirdsSuccess(TestHelpers.Birds(
            TestHelpers.Bird(name: "Sparrow", desc: "d"),
            TestHelpers.Bird(name: "Tit", desc: "d")
        ));

        var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _, autoExport: autoExport);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        store.LoadState.Should().Be(LoadState.Loaded);
        store.Birds.Should().HaveCount(2);
        collectionChangedCount.Should().Be(1);
        notify.Verify(n => n.ShowInfoLocalized(It.IsAny<string>(), It.IsAny<object[]>()),
            Times.AtLeast(2)); // Loading..., LoadedSuccessfully
        autoExport.Verify(x => x.MarkDirty(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_FailsAfterRetries_SetsFailedAndNotifies()
    {
        // Arrange
        var store = new BirdStore();
        var mediator = new Mock<IMediator>();
        mediator.SetupGetAllBirdsFailure();

        var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        store.LoadState.Should().Be(LoadState.Failed);
        store.Birds.Should().BeEmpty();

        mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        notify.Verify(n => n.ShowLocalized(It.IsAny<string>(), It.IsAny<NotificationOptions>(), It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_FailsThenSucceeds_Loads()
    {
        // Arrange
        var store = new BirdStore();
        var mediator = new Mock<IMediator>();
        mediator.SetupGetAllBirdsSequence(Result<IReadOnlyList<BirdDTO>>.Failure("temp"),
            Result<IReadOnlyList<BirdDTO>>.Failure("temp"),
            Result<IReadOnlyList<BirdDTO>>.Success(TestHelpers.Birds(TestHelpers.Bird(name: "Sparrow")))
        );

        var sut = TestHelpers.MakeInitializer(store, mediator.Object, out _, out _);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        store.LoadState.Should().Be(LoadState.Loaded);
        store.Birds.Should().HaveCount(1);
        mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_When_Database_Is_Empty_Loads_Empty_State_Without_Retrying()
    {
        // Arrange
        var store = new BirdStore();
        var mediator = new Mock<IMediator>();
        mediator.SetupGetAllBirdsSuccess(Array.Empty<BirdDTO>());

        var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        store.LoadState.Should().Be(LoadState.Loaded);
        store.Birds.Should().BeEmpty();

        mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        notify.Verify(n => n.ShowInfoLocalized("Info.NoBirdRecordsYet", It.IsAny<object[]>()), Times.Once);
        notify.Verify(
            n => n.ShowLocalized("Error.BirdLoadFailed", It.IsAny<NotificationOptions>(), It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_CanceledEarly_ThrowsException()
    {
        // Arrange
        var store = new BirdStore();
        var mediator = new Mock<IMediator>();
        var cts = new CancellationTokenSource();

        cts.Cancel();

        var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

        // Act
        var act = async () => await sut.StartAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        store.LoadState.Should().Be(LoadState.Uninitialized);
        mediator.VerifyNoOtherCalls();
        notify.VerifyNoOtherCalls();
    }
}
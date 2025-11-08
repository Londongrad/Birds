using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.UI.Enums;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.UI.Services
{
    public class BirdInitializerTests
    {
        [Fact]
        public async Task StartAsync_Success_PopulatesStoreAndLogs()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            mediator.SetupGetAllBirdsSuccess(TestHelpers.Birds(
                TestHelpers.Bird(name: "Sparrow", desc: "d"),
                TestHelpers.Bird(name: "Tit", desc: "d")
            ));

            var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            store.LoadState.Should().Be(LoadState.Loaded);
            store.Birds.Should().HaveCount(2);
            notify.Verify(n => n.ShowInfo(It.IsAny<string>()), Times.AtLeast(2)); // Loading..., LoadedSuccessfully
        }

        [Fact]
        public async Task StartAsync_FailsAfterRetries_SetsFailedAndNotifies()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            mediator.SetupGetAllBirdsFailure("db down");

            var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            store.LoadState.Should().Be(LoadState.Failed);
            store.Birds.Should().BeEmpty();

            mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            notify.Verify(n => n.Show(It.IsAny<string>(), It.IsAny<NotificationOptions>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_FailsThenSucceeds_Loads()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            TestHelpers.SetupGetAllBirdsSequence(
                mediator,
                Result<IReadOnlyList<BirdDTO>>.Failure("temp"),
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
        public async Task StartAsync_CanceledEarly_DoesNothing()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            var cts = new CancellationTokenSource(); cts.Cancel();

            var sut = TestHelpers.MakeInitializer(store, mediator.Object, out var notify, out _);

            // Act
            await sut.StartAsync(cts.Token);

            // Assert
            store.LoadState.Should().Be(LoadState.Uninitialized);
            mediator.VerifyNoOtherCalls();
            notify.VerifyNoOtherCalls();
        }
    }
}

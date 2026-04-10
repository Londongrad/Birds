using Birds.App.Services;
using Birds.Infrastructure.Services;
using Birds.Shared.Constants;
using Birds.Tests.Helpers;
using Birds.Tests.UI.Services;
using Birds.UI.Enums;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.App.Services
{
    public sealed class StartupDataCoordinatorTests
    {
        [Fact]
        public async Task InitializeAsync_WhenDatabaseInitializationSucceeds_LoadsBirdStore()
        {
            var birdStore = new BirdStore();
            var databaseInitializer = new Mock<IDatabaseInitializer>();
            var remoteSyncCoordinator = new Mock<IRemoteSyncCoordinator>();
            var mediator = new Mock<IMediator>();
            mediator.SetupGetAllBirdsSuccess(TestHelpers.Birds(TestHelpers.Bird(name: "Sparrow")));

            var birdStoreInitializer = TestHelpers.MakeInitializer(
                birdStore,
                mediator.Object,
                out var notificationService,
                out _);

            var coordinator = new StartupDataCoordinator(
                databaseInitializer.Object,
                remoteSyncCoordinator.Object,
                birdStoreInitializer,
                birdStore,
                notificationService.Object,
                new InlineUiDispatcher());

            await coordinator.InitializeAsync(CancellationToken.None);

            birdStore.LoadState.Should().Be(LoadState.Loaded);
            birdStore.Birds.Should().HaveCount(1);
            databaseInitializer.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
            remoteSyncCoordinator.Verify(x => x.Start(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenDatabaseInitializationFails_MarksStoreAsFailedAndNotifies()
        {
            var birdStore = new BirdStore();
            var databaseInitializer = new Mock<IDatabaseInitializer>();
            var remoteSyncCoordinator = new Mock<IRemoteSyncCoordinator>();
            databaseInitializer
                .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("db init failed"));

            var mediator = new Mock<IMediator>();
            var birdStoreInitializer = TestHelpers.MakeInitializer(
                birdStore,
                mediator.Object,
                out var notificationService,
                out _);

            var coordinator = new StartupDataCoordinator(
                databaseInitializer.Object,
                remoteSyncCoordinator.Object,
                birdStoreInitializer,
                birdStore,
                notificationService.Object,
                new InlineUiDispatcher());

            var act = async () => await coordinator.InitializeAsync(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            birdStore.LoadState.Should().Be(LoadState.Failed);
            mediator.VerifyNoOtherCalls();
            notificationService.Verify(
                x => x.ShowLocalized(
                    "Error.BirdLoadFailed",
                    It.Is<NotificationOptions>(o => o.Type == NotificationType.Error),
                    It.IsAny<object[]>()),
                Times.Once);
            remoteSyncCoordinator.Verify(x => x.Start(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

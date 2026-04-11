using Birds.App.Services;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Services;
using Birds.Shared.Sync;
using FluentAssertions;
using Moq;

namespace Birds.Tests.App.Services
{
    public sealed class RemoteSyncCoordinatorTests
    {
        [Fact]
        public async Task RunSingleIterationAsync_WhenRemoteSyncIsNotConfigured_Should_ReportDisabled_And_SkipService()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var localStoreStateService = CreateLocalStateServiceMock();
            statusReporter.Setup(x => x.SetDisabledAsync(0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                RemoteSyncRuntimeOptions.Disabled,
                statusReporter.Object,
                localStoreStateService.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(15));
            remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
            statusReporter.Verify(x => x.SetDisabledAsync(0, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenRemoteBackendIsUnavailable_Should_ReportOffline()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = CreateLocalStateServiceMock(pendingOperationCount: 7);
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, 3, "backend unavailable"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(7, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 3, 7, "backend unavailable", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenNothingToSync_Should_ReportSyncedState()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = CreateLocalStateServiceMock();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 0, 0, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(12));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_Should_ProcessRemoteBatches_UntilNothingToSync()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = new Mock<ILocalStoreStateService>();
            localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalStoreStateSnapshot(0, 0))
                .ReturnsAsync(new LocalStoreStateSnapshot(128, 64))
                .ReturnsAsync(new LocalStoreStateSnapshot(192, 0));
            remoteSyncService.SetupSequence(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 128))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 64))
                .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 192, 0, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            await sut.BootstrapLocalStoreAsync(CancellationToken.None);

            remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_WhenRemoteBackendIsUnavailable_Should_ReportOffline()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = new Mock<ILocalStoreStateService>();
            localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalStoreStateSnapshot(0, 0))
                .ReturnsAsync(new LocalStoreStateSnapshot(0, 5));
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, 0, "backend unavailable"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 0, 5, "backend unavailable", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            await sut.BootstrapLocalStoreAsync(CancellationToken.None);
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenSyncServiceThrows_Should_ReportLoopFailure_And_BackOff()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = CreateLocalStateServiceMock(pendingOperationCount: 9);
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("remote timeout"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(9, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetLoopFailedAsync("remote timeout", 9, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_WhenSyncServiceThrows_Should_ReportError_WithoutThrowing()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = new Mock<ILocalStoreStateService>();
            localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalStoreStateSnapshot(0, 0))
                .ReturnsAsync(new LocalStoreStateSnapshot(0, 11));
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("bootstrap timeout"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Error, 0, 11, "bootstrap timeout", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            var act = () => sut.BootstrapLocalStoreAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task PauseAsync_Should_ReportPausedState_WithPendingCount()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = CreateLocalStateServiceMock(pendingOperationCount: 6);
            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            statusReporter.Setup(x => x.SetPausedAsync(6, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            await sut.PauseAsync(CancellationToken.None);

            statusReporter.Verify(x => x.SetPausedAsync(6, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SyncNowAsync_Should_RunSingleImmediateSynchronization()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            var localStoreStateService = new Mock<ILocalStoreStateService>();
            localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalStoreStateSnapshot(3, 4))
                .ReturnsAsync(new LocalStoreStateSnapshot(3, 1));
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 3));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(4, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 3, 1, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object,
                localStoreStateService.Object);

            await sut.SyncNowAsync(CancellationToken.None);

            remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Mock<ILocalStoreStateService> CreateLocalStateServiceMock(int birdCount = 0, int pendingOperationCount = 0)
        {
            var mock = new Mock<ILocalStoreStateService>();
            mock.Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalStoreStateSnapshot(birdCount, pendingOperationCount));
            return mock;
        }
    }
}

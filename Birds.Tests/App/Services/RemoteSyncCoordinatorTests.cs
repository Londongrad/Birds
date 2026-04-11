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
            statusReporter.Setup(x => x.SetDisabledAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                RemoteSyncRuntimeOptions.Disabled,
                statusReporter.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(15));
            remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
            statusReporter.Verify(x => x.SetDisabledAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenRemoteBackendIsUnavailable_Should_ReportOffline()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, 3, "backend unavailable"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 3, "backend unavailable", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenNothingToSync_Should_ReportSyncedState()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 0, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(12));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_Should_ProcessRemoteBatches_UntilNothingToSync()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.SetupSequence(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 128))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 64))
                .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 192, null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            await sut.BootstrapLocalStoreAsync(CancellationToken.None);

            remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_WhenRemoteBackendIsUnavailable_Should_ReportOffline()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, 0, "backend unavailable"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 0, "backend unavailable", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            await sut.BootstrapLocalStoreAsync(CancellationToken.None);
        }

        [Fact]
        public async Task RunSingleIterationAsync_WhenSyncServiceThrows_Should_ReportLoopFailure_And_BackOff()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("remote timeout"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetLoopFailedAsync("remote timeout", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

            delay.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task BootstrapLocalStoreAsync_WhenSyncServiceThrows_Should_ReportError_WithoutThrowing()
        {
            var remoteSyncService = new Mock<IRemoteSyncService>();
            remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("bootstrap timeout"));

            var statusReporter = new Mock<IRemoteSyncStatusReporter>();
            var sequence = new MockSequence();
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetSyncingAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            statusReporter.InSequence(sequence)
                .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Error, 0, "bootstrap timeout", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var sut = new RemoteSyncCoordinator(
                remoteSyncService.Object,
                new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
                statusReporter.Object);

            var act = () => sut.BootstrapLocalStoreAsync(CancellationToken.None);

            await act.Should().NotThrowAsync();
        }
    }
}

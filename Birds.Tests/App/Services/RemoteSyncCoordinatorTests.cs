using Birds.App.Services;
using Birds.Tests.Helpers;
using Birds.Application.Interfaces;
using Birds.Infrastructure.Configuration;
using Birds.Infrastructure.Services;
using Birds.Shared.Sync;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using FluentAssertions;
using Moq;

namespace Birds.Tests.App.Services;

public sealed class RemoteSyncCoordinatorTests
{
    [Fact]
    public async Task RunSingleIterationAsync_WhenRemoteSyncIsNotConfigured_Should_ReportDisabled_And_SkipService()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var localStoreStateService = CreateLocalStateServiceMock();
        statusReporter.Setup(x => x.SetDisabledAsync(0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            RemoteSyncRuntimeOptions.Disabled,
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

        delay.Should().Be(TimeSpan.FromSeconds(15));
        remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
        statusReporter.Verify(x => x.SetDisabledAsync(0, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncNowAsync_WhenRemoteSyncIsEnabledButMisconfigured_Should_ReportConfigurationError_And_SkipService()
    {
        const string configurationError = "missing remote sync configuration";
        var remoteSyncService = new Mock<IRemoteSyncService>();
        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var localStoreStateService = CreateLocalStateServiceMock(pendingOperationCount: 3);
        statusReporter.Setup(x => x.SetDisabledAsync(3, configurationError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            RemoteSyncRuntimeOptions.EnabledButNotConfigured(configurationError),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        await sut.SyncNowAsync(CancellationToken.None);

        sut.IsEnabled.Should().BeTrue();
        sut.IsConfigured.Should().BeFalse();
        sut.ConfigurationErrorMessage.Should().Be(configurationError);
        remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
        statusReporter.Verify(x => x.SetDisabledAsync(3, configurationError, It.IsAny<CancellationToken>()),
            Times.Once);
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
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 3, 7, "backend unavailable",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
        remoteSyncService.Setup(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Synced, null, 42));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetRemoteSnapshotStateAsync(RemoteSyncSnapshotState.HasData, 42,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 0, 0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService(RemoteSyncIntervalPresets.ThirtySeconds).Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var delay = await sut.RunSingleIterationAsync(CancellationToken.None);

        delay.Should().Be(TimeSpan.FromSeconds(30));
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
        remoteSyncService.Setup(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Synced, null, 192));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetRemoteSnapshotStateAsync(RemoteSyncSnapshotState.HasData, 192,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 192, 0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 0, 5, "backend unavailable",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Error, 0, 11, "bootstrap timeout",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

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
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        await sut.SyncNowAsync(CancellationToken.None);

        remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedownloadRemoteSnapshotAsync_Should_ResetLocalDatabase_And_BootstrapFromRemote()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        remoteSyncService.Setup(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoteSyncBackendCheckResult.Ready);
        remoteSyncService.SetupSequence(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 128))
            .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

        var localStoreStateService = new Mock<ILocalStoreStateService>();
        localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(4, 0))
            .ReturnsAsync(new LocalStoreStateSnapshot(0, 0))
            .ReturnsAsync(new LocalStoreStateSnapshot(128, 0));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 128, 0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var databaseMaintenanceService = CreateDatabaseMaintenanceService();

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            databaseMaintenanceService.Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var restored = await sut.RedownloadRemoteSnapshotAsync(CancellationToken.None);

        restored.Should().BeTrue();
        remoteSyncService.Verify(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        databaseMaintenanceService.Verify(x => x.ResetLocalDatabaseAsync(It.IsAny<CancellationToken>()), Times.Once);
        remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RedownloadRemoteSnapshotAsync_Should_NotResetLocalDatabase_WhenBackendCheckFails()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        remoteSyncService.Setup(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.BackendUnavailable,
                "backend unavailable"));

        var localStoreStateService = new Mock<ILocalStoreStateService>();
        localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(42, 3))
            .ReturnsAsync(new LocalStoreStateSnapshot(42, 3));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(3, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 0, 3, "backend unavailable",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var databaseMaintenanceService = CreateDatabaseMaintenanceService();

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            databaseMaintenanceService.Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var restored = await sut.RedownloadRemoteSnapshotAsync(CancellationToken.None);

        restored.Should().BeFalse();
        databaseMaintenanceService.Verify(x => x.ResetLocalDatabaseAsync(It.IsAny<CancellationToken>()), Times.Never);
        remoteSyncService.Verify(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadLocalSnapshotToRemoteAsync_Should_ReportSynced_WhenUploadSucceeds()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        remoteSyncService.Setup(x => x.UploadLocalSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 372));
        remoteSyncService.Setup(x => x.CheckBackendAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncBackendCheckResult(RemoteSyncRunStatus.Synced, null, 372));

        var localStoreStateService = new Mock<ILocalStoreStateService>();
        localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(372, 4))
            .ReturnsAsync(new LocalStoreStateSnapshot(372, 0));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetRemoteSnapshotStateAsync(RemoteSyncSnapshotState.HasData, 372,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 372, 0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var uploaded = await sut.UploadLocalSnapshotToRemoteAsync(CancellationToken.None);

        uploaded.Should().BeTrue();
        remoteSyncService.Verify(x => x.UploadLocalSnapshotAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLocalSnapshotToRemoteAsync_Should_ReportOffline_WhenBackendIsUnavailable()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        remoteSyncService.Setup(x => x.UploadLocalSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.BackendUnavailable, 0, "backend unavailable"));

        var localStoreStateService = new Mock<ILocalStoreStateService>();
        localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(372, 4))
            .ReturnsAsync(new LocalStoreStateSnapshot(372, 4));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(4, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Offline, 0, 4, "backend unavailable",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            CreateNotificationService(),
            TestBackgroundTaskRunner.Create());

        var uploaded = await sut.UploadLocalSnapshotToRemoteAsync(CancellationToken.None);

        uploaded.Should().BeFalse();
        remoteSyncService.Verify(x => x.UploadLocalSnapshotAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunSingleIterationAsync_WhenRemoteWinsDetected_Should_ShowConflictWarning()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        var localStoreStateService = CreateLocalStateServiceMock(pendingOperationCount: 2);
        remoteSyncService.Setup(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 3, null, 2));

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 3, 2, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notificationService = new Mock<INotificationService>();

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            notificationService.Object,
            TestBackgroundTaskRunner.Create());

        await sut.RunSingleIterationAsync(CancellationToken.None);

        notificationService.Verify(
            x => x.ShowWarningLocalized("Info.SyncConflictResolved",
                It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 2)),
            Times.Once);
    }

    [Fact]
    public async Task BootstrapLocalStoreAsync_WhenRemoteWinsDetectedAcrossBatches_Should_ShowSingleAggregatedWarning()
    {
        var remoteSyncService = new Mock<IRemoteSyncService>();
        var localStoreStateService = new Mock<ILocalStoreStateService>();
        localStoreStateService.SetupSequence(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(0, 0))
            .ReturnsAsync(new LocalStoreStateSnapshot(192, 0));
        remoteSyncService.SetupSequence(x => x.SyncPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 128, null, 1))
            .ReturnsAsync(new RemoteSyncRunResult(RemoteSyncRunStatus.Synced, 64, null, 2))
            .ReturnsAsync(RemoteSyncRunResult.NothingToSync);

        var statusReporter = new Mock<IRemoteSyncStatusReporter>();
        var sequence = new MockSequence();
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetSyncingAsync(0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        statusReporter.InSequence(sequence)
            .Setup(x => x.SetResultAsync(RemoteSyncDisplayState.Synced, 192, 0, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notificationService = new Mock<INotificationService>();

        var sut = new RemoteSyncCoordinator(
            remoteSyncService.Object,
            new RemoteSyncRuntimeOptions(true, "Host=remote.example"),
            statusReporter.Object,
            localStoreStateService.Object,
            CreateDatabaseMaintenanceService().Object,
            CreatePreferencesService().Object,
            notificationService.Object,
            TestBackgroundTaskRunner.Create());

        await sut.BootstrapLocalStoreAsync(CancellationToken.None);

        notificationService.Verify(
            x => x.ShowWarningLocalized("Info.SyncConflictResolved",
                It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 3)),
            Times.Once);
    }

    private static Mock<ILocalStoreStateService> CreateLocalStateServiceMock(int birdCount = 0,
        int pendingOperationCount = 0)
    {
        var mock = new Mock<ILocalStoreStateService>();
        mock.Setup(x => x.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalStoreStateSnapshot(birdCount, pendingOperationCount));
        return mock;
    }

    private static Mock<IDatabaseMaintenanceService> CreateDatabaseMaintenanceService()
    {
        var mock = new Mock<IDatabaseMaintenanceService>();
        mock.SetupGet(x => x.CanResetLocalDatabase).Returns(true);
        mock.Setup(x => x.ResetLocalDatabaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IAppPreferencesService> CreatePreferencesService(string selectedSyncInterval =
        RemoteSyncIntervalPresets.Default)
    {
        var mock = new Mock<IAppPreferencesService>();
        mock.SetupGet(x => x.SelectedSyncInterval).Returns(selectedSyncInterval);
        return mock;
    }

    private static INotificationService CreateNotificationService()
    {
        return Mock.Of<INotificationService>();
    }
}


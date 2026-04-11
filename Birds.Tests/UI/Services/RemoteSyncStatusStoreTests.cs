using Birds.Shared.Sync;
using Birds.Tests.Helpers;
using Birds.UI.Services.Sync;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

public sealed class RemoteSyncStatusStoreTests
{
    [Fact]
    public async Task SetResultAsync_WhenSynced_Should_UpdateSuccessState()
    {
        var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

        await sut.SetResultAsync(RemoteSyncDisplayState.Synced, 4, 2);

        sut.Status.Should().Be(RemoteSyncDisplayState.Synced);
        sut.LastProcessedCount.Should().Be(4);
        sut.PendingOperationCount.Should().Be(2);
        sut.LastSuccessfulSyncAtUtc.Should().NotBeNull();
        sut.LastErrorMessage.Should().BeNull();
        sut.RecentActivity.Should().ContainSingle();
        sut.RecentActivity[0].Status.Should().Be(RemoteSyncDisplayState.Synced);
        sut.RecentActivity[0].ProcessedCount.Should().Be(4);
    }

    [Fact]
    public async Task SetLoopFailedAsync_WhenCalled_Should_PublishErrorState()
    {
        var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

        await sut.SetLoopFailedAsync("boom", 3);

        sut.Status.Should().Be(RemoteSyncDisplayState.Error);
        sut.LastErrorMessage.Should().Be("boom");
        sut.PendingOperationCount.Should().Be(3);
        sut.LastAttemptAtUtc.Should().NotBeNull();
        sut.RecentActivity.Should().ContainSingle();
        sut.RecentActivity[0].ErrorMessage.Should().Be("boom");
    }

    [Fact]
    public async Task SetPausedAsync_WhenCalled_Should_PublishPausedState()
    {
        var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

        await sut.SetPausedAsync(5);

        sut.Status.Should().Be(RemoteSyncDisplayState.Paused);
        sut.PendingOperationCount.Should().Be(5);
        sut.RecentActivity.Should().ContainSingle();
        sut.RecentActivity[0].Status.Should().Be(RemoteSyncDisplayState.Paused);
    }

    [Fact]
    public async Task SetResultAsync_WhenRepeatedIdleSyncsOccur_Should_NotSpamRecentActivity()
    {
        var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

        await sut.SetResultAsync(RemoteSyncDisplayState.Synced, 0, 0);
        await sut.SetResultAsync(RemoteSyncDisplayState.Synced, 0, 0);

        sut.RecentActivity.Should().ContainSingle();
    }
}
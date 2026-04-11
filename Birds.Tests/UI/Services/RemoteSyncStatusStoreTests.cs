using Birds.Shared.Sync;
using Birds.Tests.Helpers;
using Birds.UI.Services.Sync;
using FluentAssertions;

namespace Birds.Tests.UI.Services
{
    public sealed class RemoteSyncStatusStoreTests
    {
        [Fact]
        public async Task SetResultAsync_WhenSynced_Should_UpdateSuccessState()
        {
            var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

            await sut.SetResultAsync(RemoteSyncDisplayState.Synced, 4);

            sut.Status.Should().Be(RemoteSyncDisplayState.Synced);
            sut.LastProcessedCount.Should().Be(4);
            sut.LastSuccessfulSyncAtUtc.Should().NotBeNull();
            sut.LastErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task SetLoopFailedAsync_WhenCalled_Should_PublishErrorState()
        {
            var sut = new RemoteSyncStatusStore(new InlineUiDispatcher());

            await sut.SetLoopFailedAsync("boom");

            sut.Status.Should().Be(RemoteSyncDisplayState.Error);
            sut.LastErrorMessage.Should().Be("boom");
            sut.LastAttemptAtUtc.Should().NotBeNull();
        }
    }
}

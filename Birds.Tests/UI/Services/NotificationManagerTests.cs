using Birds.Tests.Helpers;
using Birds.UI.Services.Notification;
using FluentAssertions;

namespace Birds.Tests.UI.Services
{
    public sealed class NotificationManagerTests
    {
        [Fact]
        public async Task ShowNotification_WhenSameToastRepeated_Should_CoalesceIntoSingleEntry()
        {
            var sut = new NotificationManager(new InlineUiDispatcher());
            var options = new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan);

            sut.ShowNotification("Loading bird data...", options);
            sut.ShowNotification("Loading bird data...", options);

            await Task.Delay(10);

            sut.ActiveNotifications.Should().HaveCount(1);
            sut.ActiveNotifications[0].Title.Should().Be(NotificationToast.ResolveTitle(null, NotificationType.Info));
            sut.UnreadCount.Should().Be(1);
        }

        [Fact]
        public async Task DismissNotification_Should_RemoveToastFromActiveCollection()
        {
            var sut = new NotificationManager(new InlineUiDispatcher());
            sut.ShowNotification("Bird added successfully!", new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

            await Task.Delay(10);

            var notification = sut.ActiveNotifications.Single();
            sut.DismissNotification(notification);

            await Task.Delay(10);

            sut.ActiveNotifications.Should().BeEmpty();
            sut.HasNotifications.Should().BeFalse();
            sut.UnreadCount.Should().Be(0);
        }

        [Fact]
        public async Task MarkAllAsRead_Should_ResetUnreadCounter()
        {
            var sut = new NotificationManager(new InlineUiDispatcher());

            sut.ShowNotification("Archive reloaded.", new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan));
            sut.ShowNotification("Statistics updated.", new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

            await Task.Delay(10);

            sut.MarkAllAsRead();

            await Task.Delay(10);

            sut.UnreadCount.Should().Be(0);
            sut.ActiveNotifications.Should().OnlyContain(notification => notification.IsRead);
        }

        [Fact]
        public async Task ClearNotifications_Should_RemoveHistoryAndResetCounters()
        {
            var sut = new NotificationManager(new InlineUiDispatcher());

            sut.ShowNotification("One", new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan));
            sut.ShowNotification("Two", new NotificationOptions(NotificationType.Warning, Timeout.InfiniteTimeSpan));

            await Task.Delay(10);

            sut.ClearNotifications();

            await Task.Delay(10);

            sut.ActiveNotifications.Should().BeEmpty();
            sut.HasNotifications.Should().BeFalse();
            sut.UnreadCount.Should().Be(0);
        }
    }
}

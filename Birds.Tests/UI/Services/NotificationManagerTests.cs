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

            await Task.Delay(50);

            sut.ActiveNotifications.Should().HaveCount(1);
            sut.ActiveNotifications[0].Title.Should().Be("Информация");
        }

        [Fact]
        public async Task DismissNotification_Should_RemoveToastFromActiveCollection()
        {
            var sut = new NotificationManager(new InlineUiDispatcher());
            sut.ShowNotification("Bird added successfully!", new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

            await Task.Delay(50);

            var notification = sut.ActiveNotifications.Single();
            sut.DismissNotification(notification);

            await Task.Delay(50);

            sut.ActiveNotifications.Should().BeEmpty();
        }
    }
}

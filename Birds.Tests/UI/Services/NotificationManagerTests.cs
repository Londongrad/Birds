using Birds.Shared.Localization;
using Birds.Tests.Helpers;
using Birds.UI.Services.Localization;
using Birds.UI.Services.Notification;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

[Collection("LocalizationService serial")]
public sealed class NotificationManagerTests
{
    [Fact]
    public async Task ShowNotification_WhenSameToastRepeated_Should_CoalesceIntoSingleEntry()
    {
        var sut = CreateSut();
        var options = new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan);

        sut.ShowNotification("Loading bird data...", options);
        sut.ShowNotification("Loading bird data...", options);

        await Task.Delay(10);

        sut.ActiveNotifications.Should().HaveCount(1);
        sut.ActiveNotifications[0].Title.Should().Be(NotificationToast.ResolveTitle(null, NotificationType.Info));
        sut.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task ShowLocalizedNotification_WhenSameToastRepeated_Should_CoalesceIntoSingleEntry()
    {
        var sut = CreateSut();
        var options = new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan);

        sut.ShowLocalizedNotification("Info.LoadingBirdData", options);
        sut.ShowLocalizedNotification("Info.LoadingBirdData", options);

        await Task.Delay(10);

        sut.ActiveNotifications.Should().HaveCount(1);
        sut.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task ShowNotification_WhenSameSuccessRepeated_Should_KeepSeparateEntries()
    {
        var sut = CreateSut();
        var options = new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan);

        sut.ShowNotification("Bird added successfully!", options);
        sut.ShowNotification("Bird added successfully!", options);

        await Task.Delay(10);

        sut.ActiveNotifications.Should().HaveCount(2);
        sut.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task ShowNotification_WhenSuccessShown_Should_DisplayRecentOperationIndicatorTemporarily()
    {
        var sut = CreateSut(TimeSpan.FromMilliseconds(40));

        sut.ShowNotification("Bird added successfully!",
            new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

        await Task.Delay(10);

        sut.HasRecentOperationStatus.Should().BeTrue();
        sut.RecentOperationStatusType.Should().Be(NotificationType.Success);

        await Task.Delay(70);

        sut.HasRecentOperationStatus.Should().BeFalse();
        sut.RecentOperationStatusType.Should().BeNull();
    }

    [Fact]
    public async Task ShowNotification_WhenInfoShown_Should_NotDisplayRecentOperationIndicator()
    {
        var sut = CreateSut(TimeSpan.FromMilliseconds(40));

        sut.ShowNotification("Archive reloaded.",
            new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan));

        await Task.Delay(10);

        sut.HasRecentOperationStatus.Should().BeFalse();
        sut.RecentOperationStatusType.Should().BeNull();
    }

    [Fact]
    public async Task LocalizedNotification_Should_Update_Message_When_Language_Changes()
    {
        var sut = CreateSut();
        var localization = LocalizationService.Instance;
        var previousLanguage = localization.CurrentLanguage;

        try
        {
            localization.ApplyLanguage(AppLanguages.Russian);
            sut.ShowLocalizedNotification(
                "Info.LoadFailed",
                new NotificationOptions(NotificationType.Warning, Timeout.InfiniteTimeSpan),
                2,
                4);

            await Task.Delay(10);

            sut.ActiveNotifications.Should().ContainSingle();
            sut.ActiveNotifications[0].Message.Should()
                .Be(AppText.Format(localization.CurrentCulture, "Info.LoadFailed", 2, 4));

            localization.ApplyLanguage(AppLanguages.English);

            sut.ActiveNotifications[0].Message.Should()
                .Be(AppText.Format(localization.CurrentCulture, "Info.LoadFailed", 2, 4));
            sut.ActiveNotifications[0].TypeLabel.Should()
                .Be(AppText.Get("Notification.Type.Warning", localization.CurrentCulture));
        }
        finally
        {
            localization.ApplyLanguage(previousLanguage);
        }
    }

    [Fact]
    public async Task DismissNotification_Should_RemoveToastFromActiveCollection()
    {
        var sut = CreateSut();
        sut.ShowNotification("Bird added successfully!",
            new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

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
        var sut = CreateSut();

        sut.ShowNotification("Archive reloaded.",
            new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan));
        sut.ShowNotification("Statistics updated.",
            new NotificationOptions(NotificationType.Success, Timeout.InfiniteTimeSpan));

        await Task.Delay(10);

        sut.MarkAllAsRead();

        await Task.Delay(10);

        sut.UnreadCount.Should().Be(0);
        sut.ActiveNotifications.Should().OnlyContain(notification => notification.IsRead);
    }

    [Fact]
    public async Task ClearNotifications_Should_RemoveHistoryAndResetCounters()
    {
        var sut = CreateSut();

        sut.ShowNotification("One", new NotificationOptions(NotificationType.Info, Timeout.InfiniteTimeSpan));
        sut.ShowNotification("Two", new NotificationOptions(NotificationType.Warning, Timeout.InfiniteTimeSpan));

        await Task.Delay(10);

        sut.ClearNotifications();

        await Task.Delay(10);

        sut.ActiveNotifications.Should().BeEmpty();
        sut.HasNotifications.Should().BeFalse();
        sut.UnreadCount.Should().Be(0);
    }

    private static NotificationManager CreateSut(TimeSpan? recentOperationStatusDuration = null)
    {
        return new NotificationManager(
            new InlineUiDispatcher(),
            TestBackgroundTaskRunner.Create(),
            recentOperationStatusDuration);
    }
}

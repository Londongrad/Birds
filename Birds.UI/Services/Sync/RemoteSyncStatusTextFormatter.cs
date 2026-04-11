using Birds.Shared.Sync;
using Birds.UI.Services.Localization.Interfaces;

namespace Birds.UI.Services.Sync;

public static class RemoteSyncStatusTextFormatter
{
    public static string GetLabel(ILocalizationService localization, RemoteSyncDisplayState status)
    {
        return localization.GetString(status switch
        {
            RemoteSyncDisplayState.Syncing => "Settings.SyncStatus.Syncing",
            RemoteSyncDisplayState.Synced => "Settings.SyncStatus.Synced",
            RemoteSyncDisplayState.Paused => "Settings.SyncStatus.Paused",
            RemoteSyncDisplayState.Offline => "Settings.SyncStatus.Offline",
            RemoteSyncDisplayState.Error => "Settings.SyncStatus.Error",
            _ => "Settings.SyncStatus.Disabled"
        });
    }

    public static string GetHint(ILocalizationService localization, IRemoteSyncStatusSource source)
    {
        var lastSuccess = source.LastSuccessfulSyncAtUtc.HasValue
            ? FormatUtc(localization, source.LastSuccessfulSyncAtUtc.Value)
            : null;
        var pendingText = source.PendingOperationCount > 0
            ? localization.GetString("Settings.SyncStatusHint.PendingCount", source.PendingOperationCount)
            : null;

        return source.Status switch
        {
            RemoteSyncDisplayState.Syncing => Combine(
                lastSuccess is null
                    ? localization.GetString("Settings.SyncStatusHint.Syncing")
                    : localization.GetString("Settings.SyncStatusHint.SyncingWithLastSuccess", lastSuccess),
                pendingText),
            RemoteSyncDisplayState.Synced => Combine(
                lastSuccess is null
                    ? localization.GetString("Settings.SyncStatusHint.Synced")
                    : localization.GetString("Settings.SyncStatusHint.SyncedWithTimestamp", lastSuccess),
                pendingText),
            RemoteSyncDisplayState.Paused => Combine(
                lastSuccess is null
                    ? localization.GetString("Settings.SyncStatusHint.Paused")
                    : localization.GetString("Settings.SyncStatusHint.PausedWithLastSuccess", lastSuccess),
                pendingText),
            RemoteSyncDisplayState.Offline => AppendErrorDetail(
                Combine(localization.GetString("Settings.SyncStatusHint.Offline"), pendingText),
                source.LastErrorMessage,
                localization),
            RemoteSyncDisplayState.Error => AppendErrorDetail(
                Combine(localization.GetString("Settings.SyncStatusHint.Error"), pendingText),
                source.LastErrorMessage,
                localization),
            _ => Combine(localization.GetString("Settings.SyncStatusHint.Disabled"), pendingText)
        };
    }

    private static string Combine(params string?[] parts)
    {
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string AppendErrorDetail(string baseText, string? errorMessage, ILocalizationService localization)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return baseText;

        return $"{baseText} {localization.GetString("Settings.SyncStatusHint.LastError", errorMessage)}";
    }

    private static string FormatUtc(ILocalizationService localization, DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return localization.FormatDateTime(utc.ToLocalTime());
    }
}
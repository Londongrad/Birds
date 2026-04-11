using Birds.Shared.Sync;
using Birds.UI.Services.Localization.Interfaces;

namespace Birds.UI.Services.Sync
{
    public static class RemoteSyncStatusTextFormatter
    {
        public static string GetLabel(ILocalizationService localization, RemoteSyncDisplayState status)
            => localization.GetString(status switch
            {
                RemoteSyncDisplayState.Syncing => "Settings.SyncStatus.Syncing",
                RemoteSyncDisplayState.Synced => "Settings.SyncStatus.Synced",
                RemoteSyncDisplayState.Offline => "Settings.SyncStatus.Offline",
                RemoteSyncDisplayState.Error => "Settings.SyncStatus.Error",
                _ => "Settings.SyncStatus.Disabled"
            });

        public static string GetHint(ILocalizationService localization, IRemoteSyncStatusSource source)
        {
            var lastSuccess = source.LastSuccessfulSyncAtUtc.HasValue
                ? FormatUtc(localization, source.LastSuccessfulSyncAtUtc.Value)
                : null;

            return source.Status switch
            {
                RemoteSyncDisplayState.Syncing => lastSuccess is null
                    ? localization.GetString("Settings.SyncStatusHint.Syncing")
                    : localization.GetString("Settings.SyncStatusHint.SyncingWithLastSuccess", lastSuccess),
                RemoteSyncDisplayState.Synced => lastSuccess is null
                    ? localization.GetString("Settings.SyncStatusHint.Synced")
                    : localization.GetString("Settings.SyncStatusHint.SyncedWithTimestamp", lastSuccess),
                RemoteSyncDisplayState.Offline => AppendErrorDetail(
                    localization.GetString("Settings.SyncStatusHint.Offline"),
                    source.LastErrorMessage,
                    localization),
                RemoteSyncDisplayState.Error => AppendErrorDetail(
                    localization.GetString("Settings.SyncStatusHint.Error"),
                    source.LastErrorMessage,
                    localization),
                _ => localization.GetString("Settings.SyncStatusHint.Disabled")
            };
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
}

using Birds.Shared.Sync;

namespace Birds.UI.Services.Sync;

public sealed record RemoteSyncActivityDisplayItem(
    string Title,
    string Description,
    string Timestamp,
    RemoteSyncDisplayState Status);
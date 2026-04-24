namespace Birds.UI.Services.Background;

public sealed record BackgroundTaskOptions(
    string OperationName,
    Action<Exception>? OnFailure = null);

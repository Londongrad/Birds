namespace Birds.Shared.Constants
{
    public static class LogMessages
    {
        public const string LoadFailed = "Attempt {Attempt} to load birds failed. Retrying in {Delay}s. Error: {Error}.";

        public const string Error = "{Error}";

        public const string LoadedSuccessfully = "Bird data successfully loaded. {Count} birds retrieved.";

        public const string ValidationFailed = "Validation failed for {RequestName}.";
        public const string DomainRuleViolation = "Domain rule violation for {RequestName}.";
        public const string EntityNotFound = "Entity not found in {RequestName}.";
        public const string UnhandledException = "Unhandled exception for {RequestName}.";

        public const string HandlingRequest = "Handling {RequestType}.";
        public const string HandledRequest = "Handled {RequestType}.";

        public const string LogsDirectoryResolved = "Logs directory resolved to {LogsDirectory}.";
        public const string AppStarting = "\n\n\n\tApplication starting...";

        public const string HostStarted = "Host started successfully.";

        public const string AppFailed = "Application failed during startup.";
        public const string AppExited = "Application exiting normally.";
        public const string InitializerStopped = "BirdStore initialization cancelled because the application is stopping.";

        public const string UnhandledExceptionInSource = "Unhandled exception in {Source}";

        public const string EFCoreException = "Ignored EF Core connecting-state dispose: {Message}";
        public const string DisposeError = "Error during Dispose";

        public const string AutoExportFailed = "Auto-export failed";
        public const string AutoExportSucceeded = "Auto-export succeeded. Path: {Path}";

        public const string RemoteSyncProcessed = "Remote sync processed {Count} pending changes.";
        public const string RemoteSyncFailed = "Remote sync failed while processing {Count} pending changes.";
        public const string RemoteSyncStopped = "Remote sync loop stopped because the application is shutting down.";
        public const string RemoteSyncLoopFailed = "Remote sync background loop crashed unexpectedly.";
    }
}

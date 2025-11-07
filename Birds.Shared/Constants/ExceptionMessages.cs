namespace Birds.Shared.Constants
{
    public static class ExceptionMessages
    {
        public const string UiDispatcher = "UI Dispatcher";
        public const string AppDomain = "AppDomain";
        public const string UnobservedTask = "Unobserved Task";
        public const string InvalidOperation = "ExceptionHandlingBehavior can only handle responses of type Result or Result<T>. " +
                "Actual type: {0}";
        public const string NotFound = "{0} with key '{1}' was not found.";

        
        public static string OperationCanceled(string source)
        => $"{source}: The operation was canceled.";

        public static string Timeout(string source)
            => $"{source}: The operation has timed out.";

        public static string Network(string source)
            => $"{source}: A network error occurred. Check your internet connection.";

        public static string DatabaseConnection(string source)
            => $"{source}: Failed to connect to the database.";

        public static string Unexpected(string source)
            => $"{source}: An unexpected error has occurred.";
    }
}

namespace Birds.Shared.Constants
{
    public static class ErrorMessages
    {
        public const string BirdLoadFailed = "Failed to load bird data from the database. " +
            "\nPlease check your connection or restart the application.";

        public const string UnknownError = "Unknown error.";
        public const string UnexpectedError = "Unexpected error.";

        public const string CannotSaveBird = "Unable to save bird!";
        public const string CannotDeleteBird = "Unable to delete bird!";
        public const string CannotUpdateBird = "Unable to update bird!";

        public const string RequestCannotBeNull = "Request cannot be null.";
        public const string QueryCannotBeNull = "Query cannot be null.";

        public const string NoBirdsFound = "No birds found.";

        public const string ConnectionStringNotFound = "Database connection string not found.";

        public const string FailedToDisplayErrorNotification = "Failed to display error notification";

        public const string StartupErrorTitle = "Startup error";
        public static string StartupError(string message)
            => $"An error occurred during application startup: \n{message}";

        public const string ShotdownWarningTitle = "Shutdown Warning";
        public static string ShotdownError(string message)
            => $"Error during application shutdown:\n{message}";
    }
}

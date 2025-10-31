namespace Birds.Shared.Constants
{
    public static class ErrorMessages
    {
        public const string BirdLoadFailed = "Failed to load bird data from the database. " +
            "\nPlease check your connection or restart the application.";

        public const string UnknownError = "Unknown error.";

        public const string CannotSaveBird = "Unable to save bird!";
        public const string CannotDeleteBird = "Unable to delete bird!";
        public const string CannotUpdateBird = "Unable to update bird!";

        public const string InvalidOperationException = "ExceptionHandlingBehavior can only handle responses of type Result or Result<T>. " +
                "Actual type: {0}";

        public const string RequestCannotBeNull = "Request cannot be null.";
        public const string QueryCannotBeNull = "Query cannot be null.";

        public const string NotFoundException = "{0} with key '{1}' was not found.";

        public const string NoBirdsFound = "No birds found.";

        public const string ConnectionStringNotFound = "Database connection string not found.";
    }
}

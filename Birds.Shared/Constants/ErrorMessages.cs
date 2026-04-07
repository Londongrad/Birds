using Birds.Shared.Localization;

namespace Birds.Shared.Constants
{
    public static class ErrorMessages
    {
        public static string BirdLoadFailed => AppText.Get("Error.BirdLoadFailed");

        public static string UnknownError => AppText.Get("Error.UnknownError");
        public static string UnexpectedError => AppText.Get("Error.UnexpectedError");

        public static string CannotSaveBird => AppText.Get("Error.CannotSaveBird");
        public static string CannotDeleteBird => AppText.Get("Error.CannotDeleteBird");
        public static string CannotUpdateBird => AppText.Get("Error.CannotUpdateBird");

        public static string RequestCannotBeNull => AppText.Get("Error.RequestCannotBeNull");
        public static string QueryCannotBeNull => AppText.Get("Error.QueryCannotBeNull");

        public static string ConnectionStringNotFound => AppText.Get("Error.ConnectionStringNotFound");

        public static string ConnectionStringNotFoundFor(params string[] names)
            => AppText.Format("Error.ConnectionStringNotFoundFor", string.Join(", ", names));

        public static string InvalidDatabaseProvider(string provider)
            => AppText.Format("Error.InvalidDatabaseProvider", provider);

        public static string InvalidDatabaseSeedingMode(string mode)
            => AppText.Format("Error.InvalidDatabaseSeedingMode", mode);

        public static string FailedToDisplayErrorNotification
            => AppText.Get("Error.FailedToDisplayErrorNotification");

        public static string StartupErrorTitle => AppText.Get("Error.StartupErrorTitle");

        public static string StartupError(string message)
            => AppText.Format("Error.StartupError", message);

        public static string ShotdownWarningTitle => AppText.Get("Error.ShutdownWarningTitle");

        public static string ShotdownError(string message)
            => AppText.Format("Error.ShutdownError", message);
    }
}

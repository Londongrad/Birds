using Birds.Shared.Localization;

namespace Birds.Shared.Constants
{
    public static class ExceptionMessages
    {
        public static string UiDispatcher => AppText.Get("Exception.Source.UiDispatcher");

        public static string AppDomain => AppText.Get("Exception.Source.AppDomain");

        public static string UnobservedTask => AppText.Get("Exception.Source.UnobservedTask");

        public static string InvalidOperation(string actualType)
            => AppText.Format("Exception.InvalidOperation", actualType);

        public static string NotFound(string name, object key)
            => AppText.Format("Exception.NotFound", name, key);

        public static string ValidationFailure(string message)
            => AppText.Format("Exception.ValidationFailure", message);

        public static string NotFoundFailure(string message)
            => AppText.Format("Exception.NotFoundFailure", message);

        public static string UnexpectedFailure(string message)
            => AppText.Format("Exception.UnexpectedFailure", message);

        public static string OperationCanceled(string source)
            => AppText.Format("Exception.OperationCanceled", source);

        public static string Timeout(string source)
            => AppText.Format("Exception.Timeout", source);

        public static string Network(string source)
            => AppText.Format("Exception.Network", source);

        public static string DatabaseConnection(string source)
            => AppText.Format("Exception.DatabaseConnection", source);

        public static string Unexpected(string source)
            => AppText.Format("Exception.Unexpected", source);
    }
}

using Birds.Shared.Localization;

namespace Birds.Shared.Constants;

public static class ExceptionMessages
{
    public static string UiDispatcher => AppText.Get("Exception.Source.UiDispatcher");

    public static string AppDomain => AppText.Get("Exception.Source.AppDomain");

    public static string UnobservedTask => AppText.Get("Exception.Source.UnobservedTask");

    public static string InvalidOperation(string actualType)
    {
        return AppText.Format("Exception.InvalidOperation", actualType);
    }

    public static string NotFound(string name, object key)
    {
        return AppText.Format("Exception.NotFound", name, key);
    }

    public static string ValidationFailure(string message)
    {
        return AppText.Format("Exception.ValidationFailure", message);
    }

    public static string NotFoundFailure(string message)
    {
        return AppText.Format("Exception.NotFoundFailure", message);
    }

    public static string UnexpectedFailure(string message)
    {
        return AppText.Format("Exception.UnexpectedFailure", message);
    }

    public static string OperationCanceled(string source)
    {
        return AppText.Format("Exception.OperationCanceled", source);
    }

    public static string Timeout(string source)
    {
        return AppText.Format("Exception.Timeout", source);
    }

    public static string Network(string source)
    {
        return AppText.Format("Exception.Network", source);
    }

    public static string DatabaseConnection(string source)
    {
        return AppText.Format("Exception.DatabaseConnection", source);
    }

    public static string Unexpected(string source)
    {
        return AppText.Format("Exception.Unexpected", source);
    }
}
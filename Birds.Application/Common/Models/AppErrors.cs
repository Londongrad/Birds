using Birds.Shared.Constants;

namespace Birds.Application.Common.Models;

public static class AppErrors
{
    public static AppError Failure(string message, string code = AppErrorCodes.ApplicationFailure)
    {
        return new AppError(code, message);
    }

    public static AppError InvalidRequest(string message)
    {
        return new AppError(AppErrorCodes.ApplicationInvalidRequest, message);
    }

    public static AppError Validation(
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        string code = AppErrorCodes.ApplicationValidationFailed)
    {
        return new AppError(code, message, validationErrors);
    }

    public static AppError NotFound(string message, string code = AppErrorCodes.BirdNotFound)
    {
        return new AppError(code, message);
    }

    public static AppError ConcurrencyConflict(string message)
    {
        return new AppError(AppErrorCodes.BirdConcurrencyConflict, message);
    }

    public static AppError Unexpected()
    {
        return new AppError(AppErrorCodes.ApplicationUnexpected, ErrorMessages.UnexpectedError);
    }

    public static AppError Import(string message, string code = AppErrorCodes.ImportInvalidFile)
    {
        return new AppError(code, message);
    }

    public static AppError Export(string message, string code = AppErrorCodes.ExportFailure)
    {
        return new AppError(code, message);
    }

    public static AppError Sync(string message, string code = AppErrorCodes.SyncRemoteFailure)
    {
        return new AppError(code, message);
    }
}

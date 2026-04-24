using System.Diagnostics.CodeAnalysis;

namespace Birds.Application.Common.Models;

/// <summary>
///     Represents a basic operation result indicating success or failure.
/// </summary>
public class Result
{
    private Result(bool isSuccess, AppError? appError)
    {
        IsSuccess = isSuccess;
        AppError = appError;
    }

    /// <summary>
    ///     Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the structured error if the operation failed.
    /// </summary>
    public AppError? AppError { get; }

    /// <summary>
    ///     Gets an error message if the operation failed.
    /// </summary>
    public string? Error => AppError?.Message;

    /// <summary>
    ///     Gets an error message if the operation failed.
    /// </summary>
    public string? ErrorMessage => Error;

    /// <summary>
    ///     Gets the stable machine-readable error code if the operation failed.
    /// </summary>
    public string? ErrorCode => AppError?.Code;

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static Result Success()
    {
        return new Result(true, null);
    }

    /// <summary>
    ///     Creates a failed result with the specified structured error.
    /// </summary>
    public static Result Failure(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, error);
    }

    /// <summary>
    ///     Creates a failed result with the specified error message.
    /// </summary>
    public static Result Failure(string error)
    {
        return Failure(AppErrors.Failure(error));
    }
}

/// <summary>
///     Represents a result of an operation that returns a value of type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">Type of the value returned if the operation succeeds.</typeparam>
public class Result<T>
{
    private Result(bool isSuccess, AppError? appError, T? value)
    {
        IsSuccess = isSuccess;
        AppError = appError;
        Value = value;
    }

    /// <summary>
    ///     Indicates whether the operation was successful.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the structured error if the operation failed.
    /// </summary>
    public AppError? AppError { get; }

    /// <summary>
    ///     Gets an error message if the operation failed.
    /// </summary>
    public string? Error => AppError?.Message;

    /// <summary>
    ///     Gets an error message if the operation failed.
    /// </summary>
    public string? ErrorMessage => Error;

    /// <summary>
    ///     Gets the stable machine-readable error code if the operation failed.
    /// </summary>
    public string? ErrorCode => AppError?.Code;

    /// <summary>
    ///     Gets the value of the operation if it succeeded.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    ///     Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, null, value);
    }

    /// <summary>
    ///     Creates a failed result with the specified structured error.
    /// </summary>
    public static Result<T> Failure(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(false, error, default);
    }

    /// <summary>
    ///     Creates a failed result with the specified error message.
    /// </summary>
    public static Result<T> Failure(string error)
    {
        return Failure(AppErrors.Failure(error));
    }
}

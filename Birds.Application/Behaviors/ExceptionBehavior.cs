using System.Reflection;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Common.Exceptions;
using Birds.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Birds.Application.Behaviors;

/// <summary>
///     Centralized exception handling behavior for MediatR requests.
///     Converts unexpected exceptions into <see cref="Result" /> or <see cref="Result{T}" /> failures.
/// </summary>
public class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, LogMessages.ValidationFailed, typeof(TRequest).Name);
            return CreateFailureResponse(
                AppErrors.Validation(
                    ExceptionMessages.ValidationFailure(ex.Message),
                    GroupValidationErrors(ex)));
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(ex, LogMessages.DomainRuleViolation, typeof(TRequest).Name);
            return CreateFailureResponse(AppErrors.Validation(ex.Message));
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, LogMessages.EntityNotFound, typeof(TRequest).Name);
            return CreateFailureResponse(AppErrors.NotFound(ExceptionMessages.NotFoundFailure(ex.Message)));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(
                ex,
                "Optimistic concurrency conflict while handling {RequestName} for {EntityName} {EntityKey}.",
                typeof(TRequest).Name,
                ex.EntityName,
                ex.EntityKey);
            return CreateFailureResponse(AppErrors.ConcurrencyConflict(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.UnhandledException, typeof(TRequest).Name);
            return CreateFailureResponse(AppErrors.Unexpected());
        }
    }

    /// <summary>
    ///     Creates a failed <see cref="Result" /> or <see cref="Result{T}" /> depending on <typeparamref name="TResponse" />.
    /// </summary>
    private static TResponse CreateFailureResponse(AppError error)
    {
        var responseType = typeof(TResponse);

        // Case 1: Result<T>
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(Result.Failure),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(AppError) },
                null);
            var failure = failureMethod?.Invoke(null, new object[] { error });
            return (TResponse)failure!;
        }

        // Case 2: Result (non-generic)
        if (responseType == typeof(Result))
        {
            var failure = Result.Failure(error);
            return (TResponse)(object)failure;
        }

        // Case 3: Anything else → throw, because it’s not a Result
        throw new InvalidOperationException(
            ExceptionMessages.InvalidOperation(responseType.Name));
    }

    private static IReadOnlyDictionary<string, string[]> GroupValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .Where(failure => failure is not null)
            .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName)
                ? string.Empty
                : failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray());
    }
}

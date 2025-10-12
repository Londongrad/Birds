using Birds.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Birds.Application.Behaviors
{
    /// <summary>
    /// Centralized exception handling behavior for MediatR requests.
    /// Converts unexpected exceptions into <see cref="Result"/> or <see cref="Result{T}"/> failures.
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
                _logger.LogWarning(ex, "Validation failed for {RequestName}", typeof(TRequest).Name);
                return CreateFailureResponse($"Validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {RequestName}", typeof(TRequest).Name);
                return CreateFailureResponse($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a failed <see cref="Result"/> or <see cref="Result{T}"/> depending on <typeparamref name="TResponse"/>.
        /// </summary>
        private static TResponse CreateFailureResponse(string errorMessage)
        {
            var responseType = typeof(TResponse);

            // Case 1: Result<T>
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = responseType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static);
                var failure = failureMethod?.Invoke(null, new object[] { errorMessage });
                return (TResponse)failure!;
            }

            // Case 2: Result (non-generic)
            if (responseType == typeof(Result))
            {
                var failure = Result.Failure(errorMessage);
                return (TResponse)(object)failure;
            }

            // Case 3: Anything else → throw, because it’s not a Result
            throw new InvalidOperationException(
                $"ExceptionHandlingBehavior can only handle responses of type Result or Result<T>. " +
                $"Actual type: {responseType.Name}");
        }
    }
}

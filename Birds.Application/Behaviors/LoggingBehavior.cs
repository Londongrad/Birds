using MediatR;
using Microsoft.Extensions.Logging;

namespace Birds.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);

            // RequestHandlerDelegate<TResponse> уже замкнут на CancellationToken при вызове. Перенаправлять его не надо.
#pragma warning disable CA2016 // Перенаправьте параметр "CancellationToken" в методы
            var response = await next();
#pragma warning restore CA2016 // Перенаправьте параметр "CancellationToken" в методы

            _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);

            return response;
        }
    }
}

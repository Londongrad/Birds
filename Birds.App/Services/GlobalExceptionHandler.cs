using Birds.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace Birds.App.Services;

internal interface IGlobalExceptionHandler
{
    GlobalExceptionHandlingResult Handle(Exception exception, string source, GlobalExceptionSeverity severity);
}

internal enum GlobalExceptionSeverity
{
    Recoverable,
    Fatal
}

internal sealed record GlobalExceptionHandlingResult(string UserMessage, bool ShouldShutdown);

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IDiagnosticsService diagnosticsService) : IGlobalExceptionHandler
{
    public GlobalExceptionHandlingResult Handle(
        Exception exception,
        string source,
        GlobalExceptionSeverity severity)
    {
        logger.LogError(exception, LogMessages.UnhandledExceptionInSource, source);

        var userMessage = ErrorMessages.GlobalCrashMessage(diagnosticsService.LogDirectory);
        return new GlobalExceptionHandlingResult(userMessage, severity == GlobalExceptionSeverity.Fatal);
    }
}

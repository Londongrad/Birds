using System.Net.Http;
using System.Windows;
using Birds.App.Services;
using Birds.Shared.Constants;
using Birds.Shared.Localization;
using Birds.UI.Services.Notification.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Birds.App;

public partial class App
{
    private int _globalExceptionNotificationActive;

    /// <summary>
    ///     Registers global exception handlers for the application lifecycle.
    /// </summary>
    internal void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (s, args) =>
        {
            var result = HandleException(
                args.Exception,
                ExceptionMessages.UiDispatcher,
                GlobalExceptionSeverity.Fatal);
            args.Handled = true;
            if (result.ShouldShutdown)
                RequestShutdownAfterFatalException();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var result = HandleException(
                args.ExceptionObject as Exception,
                ExceptionMessages.AppDomain,
                GlobalExceptionSeverity.Fatal);
            if (result.ShouldShutdown)
                RequestShutdownAfterFatalException();
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            HandleException(
                args.Exception,
                ExceptionMessages.UnobservedTask,
                GlobalExceptionSeverity.Recoverable);
            args.SetObserved();
        };
    }

    /// <summary>
    ///     Centralized exception handling: logs the error and shows a safe user-facing notification.
    /// </summary>
    private GlobalExceptionHandlingResult HandleException(
        Exception? ex,
        string source,
        GlobalExceptionSeverity severity)
    {
        if (ex is null)
            return new GlobalExceptionHandlingResult(string.Empty, false);

        var result = _host?.Services?.GetService<IGlobalExceptionHandler>()?.Handle(ex, source, severity)
                     ?? HandleExceptionWithoutHost(ex, source, severity);
        ShowGlobalExceptionMessage(result, severity);
        return result;
    }

    private static GlobalExceptionHandlingResult HandleExceptionWithoutHost(
        Exception ex,
        string source,
        GlobalExceptionSeverity severity)
    {
        Log.Error(ex, LogMessages.UnhandledExceptionInSource, source);

        var logDirectory = string.IsNullOrWhiteSpace(SerilogSetup.CurrentLogsDirectory)
            ? AppLogPathResolver.ResolveLogsDirectory()
            : SerilogSetup.CurrentLogsDirectory;

        return new GlobalExceptionHandlingResult(
            ErrorMessages.GlobalCrashMessage(logDirectory),
            severity == GlobalExceptionSeverity.Fatal);
    }

    private void ShowGlobalExceptionMessage(
        GlobalExceptionHandlingResult result,
        GlobalExceptionSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(result.UserMessage))
            return;

        if (Interlocked.Exchange(ref _globalExceptionNotificationActive, 1) == 1)
            return;

        try
        {
            void Show()
            {
                if (severity == GlobalExceptionSeverity.Recoverable)
                {
                    var notification = _host?.Services?.GetService<INotificationService>();
                    if (notification is not null)
                    {
                        notification.ShowError(result.UserMessage);
                        return;
                    }
                }

                MessageBox.Show(
                    result.UserMessage,
                    ErrorMessages.UnexpectedError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            var dispatcher = Current?.Dispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                Show();
            else if (dispatcher.CheckAccess())
                Show();
            else
                dispatcher.Invoke(Show);
        }
        catch (Exception notifyEx)
        {
            Log.Warning(notifyEx, ErrorMessages.FailedToDisplayErrorNotification);
        }
        finally
        {
            Interlocked.Exchange(ref _globalExceptionNotificationActive, 0);
        }
    }

    private void RequestShutdownAfterFatalException()
    {
        try
        {
            var dispatcher = Current?.Dispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            if (dispatcher.CheckAccess())
                Shutdown(-1);
            else
                dispatcher.Invoke(() => Shutdown(-1));
        }
        catch (Exception shutdownEx)
        {
            Log.Warning(shutdownEx, "Failed to request shutdown after a fatal global exception.");
        }
    }

    /// <summary>
    ///     Produces a short, human-friendly message for the user based on the exception type.
    /// </summary>
    private static string BuildUserMessage(Exception ex, string source)
    {
        var descriptor = BuildUserMessageDescriptor(ex, source);
        return AppText.Format(descriptor.Key, descriptor.Args);
    }

    private static (string Key, object[] Args) BuildUserMessageDescriptor(Exception ex, string source)
    {
        if (ex is AggregateException ag && ag.InnerExceptions.Count > 0)
            ex = ag.Flatten().InnerExceptions[0];

        ex = ex.GetBaseException();

        return ex switch
        {
            OperationCanceledException => ("Exception.OperationCanceled", new object[] { source }),
            TimeoutException => ("Exception.Timeout", new object[] { source }),
            HttpRequestException => ("Exception.Network", new object[] { source }),
            _ when ex.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                => ("Exception.DatabaseConnection", new object[] { source }),
            _ => ("Exception.Unexpected", new object[] { source })
        };
    }
}

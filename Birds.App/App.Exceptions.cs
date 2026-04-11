using System.Net.Http;
using System.Windows;
using Birds.Shared.Constants;
using Birds.Shared.Localization;
using Birds.UI.Services.Notification.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Birds.App;

public partial class App
{
    /// <summary>
    ///     Registers global exception handlers for the application lifecycle.
    /// </summary>
    internal void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (s, args) =>
        {
            HandleException(args.Exception, ExceptionMessages.UiDispatcher);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            HandleException(args.ExceptionObject as Exception, ExceptionMessages.AppDomain);

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            HandleException(args.Exception, ExceptionMessages.UnobservedTask);
            args.SetObserved();
        };
    }

    /// <summary>
    ///     Centralized exception handling: logs the error and shows a user-facing notification.
    /// </summary>
    private void HandleException(Exception? ex, string source)
    {
        if (ex is null)
            return;

        Log.Error(ex, LogMessages.UnhandledExceptionInSource, source);

        var userMessageDescriptor = BuildUserMessageDescriptor(ex, source);
        var userMessage = BuildUserMessage(ex, source);

        try
        {
            var notification = _host?.Services?.GetService<INotificationService>();
            if (notification is not null)
                Current.Dispatcher.Invoke(() =>
                    notification.ShowErrorLocalized(userMessageDescriptor.Key, userMessageDescriptor.Args));
            else
                MessageBox.Show(
                    userMessage,
                    ErrorMessages.UnexpectedError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
        }
        catch (Exception notifyEx)
        {
            Log.Warning(notifyEx, ErrorMessages.FailedToDisplayErrorNotification);
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
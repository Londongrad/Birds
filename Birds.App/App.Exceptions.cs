using Birds.Shared.Constants;
using Birds.UI.Services.Notification.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net.Http;
using System.Windows;

namespace Birds.App
{
    public partial class App
    {
        /// <summary>
        /// Registers global exception handlers for the application lifecycle, wiring
        /// UI dispatcher, AppDomain, and TaskScheduler unhandled exceptions to a
        /// centralized handler so the app can report errors without crashing.
        /// </summary>
        /// <remarks>
        /// The UI dispatcher handler marks exceptions as handled to prevent process termination.
        /// The task scheduler handler marks exceptions as observed to avoid finalizer crashes.
        /// All captured exceptions are forwarded to <see cref="HandleException(Exception?, string)"/>.
        /// Call this early during startup.
        /// </remarks>
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
        /// Centralized exception handling: shows a user-facing error notification when DI is available,
        /// falls back to a message box otherwise, and logs the exception with structured metadata.
        /// </summary>
        /// <param name="ex">
        /// The exception instance to handle. If <c>null</c>, the method returns without action.
        /// </param>
        /// <param name="source">
        /// A short source label used in the user message and as the <c>{Source}</c> property in logs
        /// (e.g., <see cref="ErrorMessages.UiDispatcherException"/>).
        /// </param>
        /// <remarks>
        /// When possible, the method uses <see cref="INotificationService"/> to display the error; if the host
        /// is not available, a modal message box is shown. The exception is logged via Serilog using
        /// <c>LogMessages.UnhandledExceptionInSource</c> and the supplied source label. Any failures that occur
        /// while reporting the original exception are swallowed after logging to avoid secondary crashes.
        /// The method never throws.
        /// </remarks>
        private void HandleException(Exception? ex, string source)
        {
            if (ex is null) return;

            // Log full technical details first (stack trace, etc.)
            Log.Error(ex, LogMessages.UnhandledExceptionInSource, source);

            // Build a concise, user-facing message (no stack traces / internals)
            var userMessage = BuildUserMessage(ex, source);

            // Try to show the message to the user; do not let notification failures crash the app
            try
            {
                // Prefer DI notification service if available; do not throw if DI is not ready
                var notification = _host?.Services?.GetService<INotificationService>();
                if (notification is not null)
                {
                    // Ensure UI-thread invocation if the service requires it
                    Current.Dispatcher.Invoke(() =>
                        notification.ShowError(userMessage));
                }
                else
                {
                    // Fallback if DI is unavailable
                    MessageBox.Show(
                        userMessage,
                        ErrorMessages.UnexpectedError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception notifyEx)
            {
                // Secondary errors while notifying should not bring down the app
                Log.Warning(notifyEx, ErrorMessages.FailedToDisplayErrorNotification);
            }
        }

        /// <summary>
        /// Produces a short, human-friendly message for the user based on the exception type.
        /// Avoids leaking internal details; logs already contain full diagnostics.
        /// </summary>
        private static string BuildUserMessage(Exception ex, string source)
        {
            // Unwrap aggregates and use the root cause
            if (ex is AggregateException ag && ag.InnerExceptions.Count > 0)
                ex = ag.Flatten().InnerExceptions[0];

            ex = ex.GetBaseException();

            // Map common cases to clear phrasing; keep generic for everything else
            return ex switch
            {
                OperationCanceledException => ExceptionMessages.OperationCanceled(source),
                TimeoutException => ExceptionMessages.Timeout(source),
                HttpRequestException => ExceptionMessages.Network(source),
                // Heuristic for database connectivity without referencing provider types in UI
                _ when ex.GetType().FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                                                 => ExceptionMessages.DatabaseConnection(source),
                _ => ExceptionMessages.Unexpected(source)
            };
        }
    }
}

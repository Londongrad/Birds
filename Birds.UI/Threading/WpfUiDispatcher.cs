using Birds.UI.Threading.Abstractions;

namespace Birds.UI.Threading
{
    /// <summary>
    /// Production implementation of <see cref="IUiDispatcher"/> that targets the WPF UI thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Obtains <see cref="System.Windows.Application.Current"/> and uses its <see cref="System.Windows.Threading.Dispatcher"/>
    /// to marshal work to the UI thread. If no current application/dispatcher exists (e.g., during early startup,
    /// shutdown, or in a headless test), the action is executed inline as a best-effort fallback.
    /// </para>
    /// <para>
    /// Exceptions thrown by the action propagate to the returned <see cref="Task"/> (or synchronously if executed inline).
    /// </para>
    /// </remarks>
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        /// <summary>
        /// Invokes <paramref name="action"/> on the WPF UI thread if available; otherwise executes it inline.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="ct">
        /// Optional cancellation token. This implementation does not cancel an action once scheduled on the dispatcher,
        /// but callers may use the token to stop awaiting the task if their operation is canceled upstream.
        /// </param>
        /// <returns>A task that completes when the action has finished executing.</returns>
        /// <remarks>
        /// Uses <see cref="Dispatcher.InvokeOnUiAsync(System.Windows.Threading.Dispatcher, Action)"/>
        /// under the hood when a dispatcher is present.
        /// </remarks>
        public Task InvokeAsync(Action action, CancellationToken ct = default)
        {
            var d = System.Windows.Application.Current?.Dispatcher;
            if (d is null) { action(); return Task.CompletedTask; }
            return d.InvokeOnUiAsync(action);
        }
    }
}
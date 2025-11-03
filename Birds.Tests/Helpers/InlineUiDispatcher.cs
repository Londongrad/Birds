using Birds.UI.Threading.Abstractions;

namespace Birds.Tests.Helpers
{
    /// <summary>
    /// Test implementation of IUiDispatcher that runs actions inline on the calling thread.
    /// </summary>
    public sealed class InlineUiDispatcher : IUiDispatcher
    {
        /// <summary>
        /// Executes <paramref name="action"/> immediately on the calling thread.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="ct">
        /// Optional cancellation token (not observed; provided for API compatibility with other implementations).
        /// </param>
        /// <returns>A completed task.</returns>
        public Task InvokeAsync(Action action, CancellationToken ct = default)
        {
            action();
            return Task.CompletedTask;
        }
    }
}
namespace Birds.UI.Threading.Abstractions
{
    /// <summary>
    /// Abstraction that schedules work to be executed on a UI (dispatcher) context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="IUiDispatcher"/> to decouple your application logic from WPF-specific
    /// dependencies. This makes code easier to test and reuse in environments where a real
    /// WPF dispatcher may not be available.
    /// </para>
    /// <para>
    /// Typical usage is to inject an implementation (<see cref="WpfUiDispatcher"/> at runtime,
    /// <see cref="InlineUiDispatcher"/> in unit tests) into view models or services that need to
    /// update UI-bound state.
    /// </para>
    /// </remarks>
    public interface IUiDispatcher
    {
        /// <summary>
        /// Schedules the specified <paramref name="action"/> for execution on the UI context.
        /// </summary>
        /// <param name="action">The work to execute.</param>
        /// <param name="ct">
        /// A cancellation token for cooperative cancellation at the call site. Current implementations
        /// do not cancel an action already executing on the UI thread; the token is intended to allow
        /// callers to abandon awaiting the returned task if needed.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the action has executed. The task may fault if the action throws.
        /// </returns>
        Task InvokeAsync(Action action, CancellationToken ct = default);
    }
}
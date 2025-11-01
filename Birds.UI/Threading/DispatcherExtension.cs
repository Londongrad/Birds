namespace Birds.UI.Threading
{
    /// <summary>
    /// Provides extensions and abstractions that help marshal work onto the WPF UI thread.
    /// </summary>
    /// <remarks>
    /// WPF enforces thread affinity for most UI elements: they must be accessed only from the thread
    /// that created them (typically the main UI thread). These helpers make it easy to safely execute
    /// code on that UI thread from background or worker threads.
    /// </remarks>
    internal static class Dispatcher
    {
        /// <summary>
        /// Invokes the specified <see cref="Action"/> on the WPF UI thread asynchronously.
        /// </summary>
        /// <param name="dispatcher">
        /// The WPF <see cref="System.Windows.Threading.Dispatcher"/> associated with the UI thread.
        /// Typically this is <see cref="System.Windows.Application.Dispatcher"/> or an element's dispatcher.
        /// </param>
        /// <param name="action">The <see cref="Action"/> to execute on the UI thread.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when <paramref name="action"/> has finished executing on the UI thread.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the current thread already has access to the UI thread
        /// (checked via <see cref="System.Windows.Threading.Dispatcher.CheckAccess"/>), the action is executed
        /// immediately on the current thread; otherwise the action is queued to the dispatcher using
        /// <see cref="System.Windows.Threading.Dispatcher.InvokeAsync(Action)"/> and awaited.
        /// </para>
        /// <para>
        /// This pattern ensures thread-safety when interacting with WPF controls from background threads,
        /// while avoiding an unnecessary context switch when already on the UI thread.
        /// </para>
        /// <para>
        /// Exception behavior:
        /// if <paramref name="action"/> throws and you already had access to the UI thread, the exception is thrown
        /// synchronously to the caller. If the action was dispatched, the exception is propagated when the returned
        /// <see cref="Task"/> is awaited (it will fault with the original exception).
        /// </para>
        /// </remarks>
        public static async Task InvokeOnUiAsync(this System.Windows.Threading.Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
                action();
            else
                await dispatcher.InvokeAsync(action);
        }
    }
}
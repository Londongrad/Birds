namespace Birds.UI.Extensions
{
    /// <summary>
    /// Provides extension methods for safely invoking actions on the UI thread.
    /// </summary>
    public static class Dispatcher
    {
        /// <summary>
        /// Invokes the specified <see cref="Action"/> on the UI thread asynchronously.
        /// </summary>
        /// <param name="dispatcher">The WPF <see cref="System.Windows.Threading.Dispatcher"/> associated with the UI thread.</param>
        /// <param name="action">The <see cref="Action"/> to be executed on the UI thread.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method checks whether the current thread has access to the UI thread via <see cref="System.Windows.Threading.Dispatcher.CheckAccess"/>.
        /// If access is granted, the action is executed immediately on the current thread.
        /// Otherwise, the action is dispatched asynchronously to the UI thread using <see cref="System.Windows.Threading.Dispatcher.InvokeAsync"/>.
        /// 
        /// This approach helps ensure thread safety when updating UI elements from background threads.
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

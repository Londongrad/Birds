namespace Birds.UI.Extensions
{
    public static class Dispatcher
    {
        public static async Task InvokeOnUiAsync(this System.Windows.Threading.Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
                action();
            else
                await dispatcher.InvokeAsync(action);
        }
    }
}

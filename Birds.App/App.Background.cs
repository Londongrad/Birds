using Birds.Shared.Constants;
using Birds.UI.Services.Stores.BirdStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Birds.App;

public partial class App
{
    /// <summary>
    /// Starts, on a background thread, the bird data bootstrap for the application:
    /// resolves and invokes <see cref="BirdStoreInitializer"/> to load the shared bird
    /// collection and, on application start, perform a JSON data export.
    /// </summary>
    /// <param name="host">
    /// The fully built <see cref="IHost"/> used to resolve services required by the
    /// initializer and to obtain <see cref="IHostApplicationLifetime"/> for cancellation.
    /// </param>
    /// <remarks>
    /// Fire-and-forget: returns immediately while work continues in the background.
    /// Errors are not thrown to the caller; they are logged via Serilog. Cancellation
    /// is observed through <see cref="IHostApplicationLifetime.ApplicationStopping"/>.
    /// </remarks>
    internal void StartBackgroundInitialization(IHost host)
    {
        _ = Task.Run(async () =>
        {
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var initializer = host.Services.GetRequiredService<BirdStoreInitializer>();
            try
            {
                await initializer.StartAsync(lifetime.ApplicationStopping);
            }
            catch (OperationCanceledException)
            {
                Log.Warning(LogMessages.InitializerStopped);
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogMessages.UnhandledExceptionInSource, initializer.GetType().Name);
            }
        });
    }
}

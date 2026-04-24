using Birds.App.Services;
using Birds.UI.Services.Background;
using Birds.UI.Services.Stores.BirdStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Birds.App;

public partial class App
{
    /// <summary>
    ///     Starts the full data bootstrap for the application as an observed background operation:
    ///     prepares the database and then loads the shared bird collection for the UI.
    /// </summary>
    /// <param name="host">
    ///     The fully built <see cref="IHost" /> used to resolve services required by the
    ///     initializer and to obtain <see cref="IHostApplicationLifetime" /> for cancellation.
    /// </param>
    /// <remarks>
    ///     Returns immediately while work continues in the background. Errors are observed by
    ///     <see cref="IBackgroundTaskRunner" /> and cancellation is tied to
    ///     <see cref="IHostApplicationLifetime.ApplicationStopping" />.
    /// </remarks>
    internal void StartBackgroundInitialization(IHost host)
    {
        var birdStore = host.Services.GetRequiredService<IBirdStore>();
        birdStore.BeginLoading();

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var coordinator = host.Services.GetRequiredService<StartupDataCoordinator>();
        var backgroundTasks = host.Services.GetRequiredService<IBackgroundTaskRunner>();

        backgroundTasks.Run(
            token => coordinator.InitializeAsync(token),
            new BackgroundTaskOptions("Startup data initialization"),
            lifetime.ApplicationStopping);
    }
}

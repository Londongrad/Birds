using Birds.UI.Converters;
using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.ViewModels;
using Birds.UI.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Birds.App;

public partial class App
{
    /// <summary>
    ///     Bootstraps the UI: configures the Bird DTO→ViewModel converter,
    ///     registers navigation mappings, then resolves <see cref="MainViewModel" />
    ///     and opens the main window via <see cref="INavigationService" />.
    /// </summary>
    /// <param name="host">
    ///     A started <see cref="IHost" /> used to resolve UI services (converter cache,
    ///     navigation service, main view model).
    /// </param>
    /// <remarks>
    ///     Must be called after the host has started. The method awaits window opening
    ///     and surfaces DI resolution errors immediately.
    /// </remarks>
    internal async Task BootstrapUiAsync(IHost host)
    {
        ConfigureConverter(host);
        ConfigureStartupNavigation(host);

        var mainVm = host.Services.GetRequiredService<MainViewModel>();
        var nav = host.Services.GetRequiredService<INavigationService>();
        await nav.OpenWindow(mainVm);
    }

    /// <summary>
    ///     Wires the XAML resource <see cref="BirdVmConverter" /> with the
    ///     bird list's bounded item view-model cache.
    /// </summary>
    /// <param name="host">
    ///     Host used to resolve <see cref="BirdListViewModel" />.
    /// </param>
    private void ConfigureConverter(IHost host)
    {
        var converter = (BirdVmConverter)Resources["BirdVmConverter"];
        converter.Cache = host.Services.GetRequiredService<BirdListViewModel>().BirdViewModelCache;
    }

    /// <summary>
    ///     Registers startup navigation mapping between <see cref="MainViewModel" /> and
    ///     <see cref="MainWindow" /> in <see cref="INavigationService" />.
    /// </summary>
    /// <param name="host">
    ///     Host used to resolve <see cref="INavigationService" />.
    /// </param>
    private void ConfigureStartupNavigation(IHost host)
    {
        var nav = host.Services.GetRequiredService<INavigationService>();
        nav.AddWindow<MainViewModel>(() => new MainWindow());
    }
}

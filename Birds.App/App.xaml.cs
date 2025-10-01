using Birds.Application;
using Birds.Infrastructure;
using Birds.UI;
using Birds.UI.Services.Navigation;
using Birds.UI.ViewModels;
using Birds.UI.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Birds.App
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddApplication();

                    var connectionString = "Data Source=birds.db";
                    services.AddInfrastructure(connectionString);

                    services.AddUI();
                })
                .Build();

            await _host.StartAsync();

            var nav = _host.Services.GetRequiredService<INavigationService>();

            // Регистрируем, как открывать MainWindow
            nav.AddWindow<MainViewModel>(() => new MainWindow());

            // Стартуем приложение с главного окна
            var mainVm = _host.Services.GetRequiredService<MainViewModel>();
            await nav.OpenWindow(mainVm);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
                await _host.StopAsync();

            base.OnExit(e);
        }
    }
}
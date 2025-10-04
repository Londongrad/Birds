using Birds.Application;
using Birds.Infrastructure;
using Birds.UI;
using Birds.UI.Converters;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Navigation;
using Birds.UI.ViewModels;
using Birds.UI.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
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

                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new ConfigurationErrorsException("Не найдена строка подключения 'DefaultConnection' в appsettings.json");

                    services.AddInfrastructure(connectionString);

                    services.AddUI();
                })
                .Build();

            await _host.StartAsync();

            // Задаем фабрику для конвертера
            var converter = (BirdVmConverter)Resources["BirdVmConverter"];
            converter.Factory = _host.Services.GetRequiredService<IBirdViewModelFactory>();

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
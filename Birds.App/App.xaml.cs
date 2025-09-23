using Birds.Application;
using Birds.Infrastructure;
using Birds.UI;
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

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Application слой
                    services.AddApplication();

                    // Infrastructure слой (с подключением к SQLite)
                    var connectionString = "Data Source=birds.db";
                    services.AddInfrastructure(connectionString);

                    services.AddUI();


                })
                .Build();

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();

            MainWindow = mainWindow;
            MainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
                await _host.StopAsync();

            base.OnExit(e);
        }
    }

}

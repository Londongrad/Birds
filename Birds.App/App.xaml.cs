using Birds.Infrastructure;
using Birds.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Birds.App
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
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

                    // UI/ViewModels
                    //services.AddSingleton<MainWindow>();
                    //services.AddSingleton<MainViewModel>();
                })
                .Build();

            _host.Start();

            //var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            //mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
                await _host.StopAsync();

            base.OnExit(e);
        }
    }

}

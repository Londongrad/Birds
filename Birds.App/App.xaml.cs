using Birds.Application;
using Birds.Infrastructure;
using Birds.UI;
using Birds.UI.Converters;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Navigation;
using Birds.UI.Services.Stores.BirdStore;
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

            try
            {
                // Создание и конфигурация Host
                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        services.AddApplication();

                        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ConfigurationErrorsException(
                                "Не найдена строка подключения 'DefaultConnection' в appsettings.json");

                        services.AddInfrastructure(connectionString);
                        services.AddUI();
                    })
                    .Build();

                // Запуск хоста
                await _host.StartAsync();

                // Инициализация данных (BirdStoreInitializer)
                var initializer = _host.Services.GetRequiredService<BirdStoreInitializer>();
                await initializer.StartAsync(CancellationToken.None);

                // Конфигурация конвертера и навигации
                ConfigureConverter();
                ConfigureNavigation();

                // Запуск главного окна
                var mainVm = _host.Services.GetRequiredService<MainViewModel>();
                var nav = _host.Services.GetRequiredService<INavigationService>();
                await nav.OpenWindow(mainVm);
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                MessageBox.Show(
                    $"Ошибка при запуске приложения:\n{ex.Message}",
                    "Ошибка запуска",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                try
                {
                    await _host.StopAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при завершении приложения:\n{ex.Message}",
                        "Ошибка выхода",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                finally
                {
                    _host.Dispose();
                }
            }

            base.OnExit(e);
        }

        // Настраивает конвертер BirdVmConverter, чтобы он знал, как создавать BirdViewModel.
        private void ConfigureConverter()
        {
            var converter = (BirdVmConverter)Resources["BirdVmConverter"];
            converter.Factory = _host!.Services.GetRequiredService<IBirdViewModelFactory>();
        }

        // Регистрирует окна и соответствующие ViewModel в навигационном сервисе.
        private void ConfigureNavigation()
        {
            var nav = _host!.Services.GetRequiredService<INavigationService>();
            nav.AddWindow<MainViewModel>(() => new MainWindow());
        }
    }
}

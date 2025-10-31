#region [ Using Directives ]

using Birds.Application;
using Birds.Infrastructure;
using Birds.Shared.Constants;
using Birds.UI;
using Birds.UI.Converters;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Navigation.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.ViewModels;
using Birds.UI.Views.Windows;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

#endregion [ Using Directives ]

namespace Birds.App
{
    /// <summary>
    /// Main entry point for the Birds WPF application.
    /// Handles dependency injection setup, single-instance protection, and application startup logic.
    /// </summary>
    public partial class App : System.Windows.Application
    {
        #region [ Fields ]

        private IHost? _host;
        private static Mutex? _mutex;

        #endregion [ Fields ]

        #region [ Startup / Initialization ]

        /// <summary>
        /// Called when the WPF application starts.
        /// Ensures only one instance of the app is running, configures dependency injection,
        /// opens the main window, and starts background initialization.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            const string appName = "BirdsAppSingleton"; // Unique name for the mutex to ensure single instance.

            // Create a mutex to prevent multiple instances of the application.
            _mutex = new Mutex(true, appName, out bool isNewInstance);
            if (!isNewInstance)
            {
                // Another instance is already running → bring it to front and exit.
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            // Initialize Serilog before building the Host
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();

            base.OnStartup(e);

            #region [ Global Exception Handlers ]

            // Global exception handlers to catch unhandled exceptions from various contexts.
            DispatcherUnhandledException += (sender, args) =>
            {
                HandleException(args.Exception, "UI Dispatcher Exception");
                args.Handled = true; // Prevent application crash
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                HandleException(args.ExceptionObject as Exception, "AppDomain Exception");
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                HandleException(args.Exception, "Unobserved Task Exception");
                args.SetObserved();
            };

            #endregion [ Global Exception Handlers ]

            try
            {
                // Create and configure the .NET Generic Host (Dependency Injection container).
                _host = Host.CreateDefaultBuilder()
                    // Integrates Serilog into the host pipeline
                    .UseSerilog((context, services, configuration) =>
                        configuration.ReadFrom.Configuration(context.Configuration))
                    // Configuration loading
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        // Load .env file from the root of the solution
                        Env.TraversePath().Load();

                        // Add environment variables (so ${DB_PASSWORD}, etc. can be used inside appsettings.json)
                        config.AddEnvironmentVariables();
                    })

                    // Configure Dependency Injection
                    .ConfigureServices((context, services) =>
                    {
                        // Register Application Layer (CQRS, Mediator, Validators, etc.)
                        services.AddApplication();

                        // Get raw connection string (with ${...} placeholders)
                        var rawConnection = context.Configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ConfigurationErrorsException(
                                ErrorMessages.ConnectionStringNotFound);

                        // Replace ${VARIABLE} placeholders with actual environment variable values
                        var connectionString = Regex.Replace(rawConnection, @"\$\{(\w+)\}", match =>
                        {
                            var key = match.Groups[1].Value;
                            return Environment.GetEnvironmentVariable(key) ?? match.Value;
                        });

                        // Register Infrastructure Layer (EF Core, DbContext, Repositories, etc.)
                        services.AddInfrastructure(connectionString);

                        // Register UI Layer (ViewModels, Stores, Navigation, etc.)
                        services.AddUI();
                    })
                    .Build();

                Log.Information(LogMessages.AppStarting);

                // Start the host and its background services (if any).
                await _host.StartAsync();

                Log.Information(LogMessages.HostStarted);

                // Configure converters and navigation services.
                ConfigureConverter();
                ConfigureNavigation();

                // Create and show the main application window.
                var mainVm = _host.Services.GetRequiredService<MainViewModel>();
                var nav = _host.Services.GetRequiredService<INavigationService>();
                await nav.OpenWindow(mainVm);

                // Configure application lifetime events.
                var lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();

                // Run BirdStore initialization in the background (async, non-blocking).
                InitializeBackgroundServices(_host, lifetime);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, LogMessages.AppFailed);

                // Display a message box if startup fails.
                MessageBox.Show(
                    $"Error during application startup:\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        #endregion [ Startup / Initialization ]

        #region [ Shutdown / Cleanup ]

        /// <summary>
        /// Called when the WPF application exits.
        /// Ensures graceful host shutdown and mutex release.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is null)
                return;

            try
            {
                // Stop the host and release resources.
                await _host.StopAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during application shutdown:\n{ex.Message}",
                    "Shutdown Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                try
                {
                    _host.Dispose();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Connecting"))
                {
                    Log.Debug(LogMessages.EFCoreException, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, LogMessages.DisposeError);
                }
            }

            // Release the mutex so the app can be launched again.
            _mutex?.ReleaseMutex();

            Log.Information(LogMessages.AppExited);
            Log.CloseAndFlush(); // Ensures all logs are written to file

            base.OnExit(e);
        }

        #endregion [ Shutdown / Cleanup ]

        #region [ Application Setup Helpers ]

        private void InitializeBackgroundServices(IHost host, IHostApplicationLifetime lifetime)
        {
            _ = Task.Run(async () =>
            {
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
                    Log.Error($"Unhandled exception in BirdStoreInitializer: {ex}");
                }
            });
        }

        /// <summary>
        /// Configures the BirdVmConverter instance to use the dependency-injected ViewModel factory.
        /// </summary>
        private void ConfigureConverter()
        {
            var converter = (BirdVmConverter)Resources["BirdVmConverter"];
            converter.Factory = _host!.Services.GetRequiredService<IBirdViewModelFactory>();
        }

        /// <summary>
        /// Registers all windows and their corresponding ViewModels in the navigation service.
        /// </summary>
        private void ConfigureNavigation()
        {
            var nav = _host!.Services.GetRequiredService<INavigationService>();
            nav.AddWindow<MainViewModel>(() => new MainWindow());
        }

        #endregion [ Application Setup Helpers ]

        #region [ Single Instance Management ]

        /// <summary>
        /// Brings an already running instance of the application to the foreground.
        /// </summary>
        private static void BringExistingInstanceToFront()
        {
            try
            {
                // Find the current process and locate an existing instance.
                var current = Process.GetCurrentProcess();
                var existing = Process.GetProcessesByName(current.ProcessName)
                                      .FirstOrDefault(p => p.Id != current.Id);

                // If another instance exists, restore and activate its window.
                if (existing != null)
                {
                    var handle = existing.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(handle);
                    }
                }
            }
            catch
            {
                // Ignore any interop or permission errors.
            }
        }

        #endregion [ Single Instance Management ]

        #region [ Exception Handling ]

        /// <summary>
        /// Handles unexpected exceptions that occur during the application's runtime.
        /// Displays an error message to the user and optionally logs diagnostic details.
        /// </summary>
        /// <param name="context">A short description of where the exception occurred (e.g., "UI Exception", "Startup Error").</param>
        /// <param name="ex">The exception instance containing details about the error.</param>
        /// <remarks>
        /// This method is used by global exception handlers such as
        /// <see cref="System.Windows.Application.DispatcherUnhandledException"/>,
        /// <see cref="AppDomain.UnhandledException"/>, and
        /// <see cref="TaskScheduler.UnobservedTaskException"/> to provide
        /// centralized error reporting and prevent the application from crashing unexpectedly.
        /// </remarks>

        private void HandleException(Exception? ex, string source)
        {
            if (ex is null)
                return;

            try
            {
                // If the host and services are available, use the notification service.
                if (_host?.Services != null)
                {
                    var notification = _host.Services.GetRequiredService<INotificationService>();
                    notification.ShowError($"{source}: {ex.Message}");
                }
                else
                {
                    // Fallback to a message box if DI is not available.
                    MessageBox.Show(
                        $"{source}:\n{ex.Message}",
                        "Unexpected Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                Log.Error(ex, LogMessages.UnhandledExceptionInSource, source);
            }
            catch (Exception)
            {
                Log.Error(ex, LogMessages.UnhandledExceptionInSource, source);
            }
        }

        #endregion [ Exception Handling ]
    }

    /// <summary>
    /// Contains native Win32 API methods used to bring an existing application window to the front.
    /// </summary>
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        internal const int SW_RESTORE = 9;
    }
}
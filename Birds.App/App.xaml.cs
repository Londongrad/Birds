using Birds.Shared.Constants;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;

namespace Birds.App
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 1) Enforce single instance. If another instance exists, bring it to front and exit.
            if (!AcquireSingleInstanceGuard("BirdsAppSingleton"))
            {
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            // 2) Bootstrap logger 
            SerilogSetup.InitBootstrapLogger();

            base.OnStartup(e);

            // 3) Register global exception handlers (log + user-friendly notification).
            RegisterGlobalExceptionHandlers();

            try
            {
                // 4) Build the Host (configuration, DI, Serilog from appsettings).
                _host = BuildHost();

                Log.Information(LogMessages.AppStarting);

                // 5) Start the Host (background services etc.).
                await _host.StartAsync();

                Log.Information(LogMessages.HostStarted);

                // 6) Bootstrap UI: configure converter & navigation, open main window via INavigationService.
                await BootstrapUiAsync(_host);

                // 7) Kick off background initialization (BirdStoreInitializer) without blocking UI.
                StartBackgroundInitialization(_host);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, LogMessages.AppFailed);
                MessageBox.Show(
                    ErrorMessages.StartupError(ex.Message),
                    ErrorMessages.StartupErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Gracefully stop and dispose the Host if it was created.
            if (_host is not null)
            {
                try
                {
                    await _host.StopAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ErrorMessages.ShotdownError(ex.Message),
                        ErrorMessages.ShotdownWarningTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                finally
                {
                    try { _host.Dispose(); }
                    // EF Core may throw when connection is mid-flight; we downgrade this to Debug.
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Connecting"))
                    {
                        Log.Debug(LogMessages.EFCoreException, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, LogMessages.DisposeError);
                    }
                }
            }

            // Release the single-instance mutex (only if we own it).
            ReleaseSingleInstanceGuard();

            Log.Information(LogMessages.AppExited);
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}

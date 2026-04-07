using Birds.Infrastructure.Services;
using Birds.Shared.Constants;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Threading.Abstractions;

namespace Birds.App.Services
{
    public sealed class StartupDataCoordinator(
        IDatabaseInitializer databaseInitializer,
        BirdStoreInitializer birdStoreInitializer,
        IBirdStore birdStore,
        INotificationService notificationService,
        IUiDispatcher uiDispatcher)
    {
        private readonly IDatabaseInitializer _databaseInitializer = databaseInitializer;
        private readonly BirdStoreInitializer _birdStoreInitializer = birdStoreInitializer;
        private readonly IBirdStore _birdStore = birdStore;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IUiDispatcher _uiDispatcher = uiDispatcher;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _uiDispatcher.InvokeAsync(_birdStore.BeginLoading, cancellationToken);

            try
            {
                await _databaseInitializer.InitializeAsync(cancellationToken);
                await _birdStoreInitializer.StartAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                await _uiDispatcher.InvokeAsync(() =>
                {
                    _birdStore.FailLoading();
                    _notificationService.ShowLocalized(
                        "Error.BirdLoadFailed",
                        new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(7)));
                }, CancellationToken.None);

                throw;
            }
        }
    }
}

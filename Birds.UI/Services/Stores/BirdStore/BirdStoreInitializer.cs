using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.Shared.Constants;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Threading.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Service responsible for initializing the bird store at application startup.
    ///
    /// <para>
    /// The main purpose is to load the list of birds from the database using <see cref="IMediator"/>
    /// and populate the shared <see cref="IBirdStore"/> collection for use across
    /// all ViewModels.
    /// </para>
    ///
    /// <para>
    /// Includes a retry policy that attempts to load bird data multiple times in case of temporary failures.
    /// The operation is retried up to four times, with delays of 2, 4, 6, and 8 seconds between attempts.
    /// </para>
    ///
    /// <para>
    /// If all attempts fail, an error message is logged and displayed to the user.
    /// </para>
    /// </summary>
    public class BirdStoreInitializer(
        IBirdStore birdStore,
        IMediator mediator,
        ILogger<BirdStoreInitializer> logger,
        INotificationService notificationService,
        IUiDispatcher uiDispatcher,
        // This parameter is optional and mainly for testing purposes in unit tests
        IAsyncPolicy<Result<IReadOnlyList<BirdDTO>>>? retryPolicy = null)
    {
        private readonly IBirdStore _birdStore = birdStore;
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<BirdStoreInitializer> _logger = logger;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IUiDispatcher _ui = uiDispatcher;

        // Define a retry policy: retry if the Result is not successful
        private readonly IAsyncPolicy<Result<IReadOnlyList<BirdDTO>>> _policy =
            retryPolicy
            ?? Policy
                .HandleResult<Result<IReadOnlyList<BirdDTO>>>(r => !r.IsSuccess)
                .WaitAndRetryAsync(
                    retryCount: 4,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt * 2), // 2, 4, 6, 8 seconds
                    onRetry: (outcome, delay, attempt, context) =>
                    {
                        logger.LogWarning(
                            LogMessages.LoadFailed,
                            attempt,
                            delay.TotalSeconds,
                            outcome.Result?.Error ?? ErrorMessages.UnknownError);
                    });

        /// <summary>
        /// Called when the application starts.
        /// Loads all birds from the database and populates the <see cref="IBirdStore"/>.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            _birdStore.BeginLoading();

            _notificationService.ShowInfo(InfoMessages.LoadingBirdData);

            // Execute the query through the retry policy
            var result = await _policy.ExecuteAsync(async ct =>
                            await _mediator.Send(new GetAllBirdsQuery(), ct), cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError(LogMessages.Error, result.Error);

                _birdStore.FailLoading(); // mark as failed

                _notificationService.Show(
                    ErrorMessages.BirdLoadFailed,
                    new NotificationOptions(NotificationType.Error, TimeSpan.FromSeconds(7)));

                return;
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            await _ui.InvokeAsync(() =>
            {
                _birdStore.Birds.Clear();
                foreach (BirdDTO bird in result.Value)
                    _birdStore.Birds.Add(bird);
            }, cancellationToken);

            _notificationService.ShowInfo(InfoMessages.LoadedSuccessfully);
            _birdStore.CompleteLoading(); // mark as successfully loaded and fire an event
            _logger.LogInformation(LogMessages.LoadedSuccessfully, _birdStore.Birds.Count);
        }
    }
}
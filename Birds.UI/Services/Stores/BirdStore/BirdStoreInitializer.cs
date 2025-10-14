using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.UI.Enums;
using Birds.UI.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using System.Windows;

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
        ILogger<BirdStoreInitializer> logger)
    {
        private readonly IBirdStore _birdStore = birdStore;
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<BirdStoreInitializer> _logger = logger;

        /// <summary>
        /// Called when the application starts.
        /// Loads all birds from the database and populates the <see cref="IBirdStore"/>.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _birdStore.LoadState = LoadState.Loading;

            // Define a retry policy: retry if the Result is not successful
            var retryPolicy = Policy
                .HandleResult<Result<IReadOnlyList<BirdDTO>>>(r => !r.IsSuccess)
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt * 2), // 2, 4, 6, 8 seconds
                    onRetry: (outcome, delay, attempt, context) =>
                    {
                        _logger.LogWarning(
                            "Attempt {Attempt} to load birds failed. Retrying in {Delay}s. Error: {Error}",
                            attempt,
                            delay.TotalSeconds,
                            outcome.Result?.Error ?? "Unknown error");
                    });

            // Execute the query through the retry policy
            var result = await retryPolicy.ExecuteAsync(() => _mediator.Send(new GetAllBirdsQuery(), cancellationToken));

            if (!result.IsSuccess)
            {
                _logger.LogError("{Error}", result.Error);

                _birdStore.LoadState = LoadState.Failed; // mark as failed

                MessageBox.Show(
                    "Failed to load bird data from the database. The application cannot continue without this data.\n\nPlease check your connection or restart the application.",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(() =>
            {
                _birdStore.Birds.Clear();
                foreach (BirdDTO bird in result.Value)
                    _birdStore.Birds.Add(bird);
            });

            _birdStore.LoadState = LoadState.Loaded; // mark as successfully loaded
        }

        /// <summary>
        /// Called when the application is shutting down.
        /// No logic is required in this case.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

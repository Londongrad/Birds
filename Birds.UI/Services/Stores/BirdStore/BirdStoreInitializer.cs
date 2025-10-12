using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using MediatR;

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
    /// </summary>
    public class BirdStoreInitializer
    {
        private readonly IBirdStore _birdStore;
        private readonly IMediator _mediator;

        /// <summary>
        /// Creates a new instance of the bird store initialization service.
        /// </summary>
        /// <param name="birdStore">The shared bird collection store.</param>
        /// <param name="mediator">The MediatR instance used for executing database queries.</param>
        public BirdStoreInitializer(IBirdStore birdStore, IMediator mediator)
        {
            _birdStore = birdStore;
            _mediator = mediator;
        }

        /// <summary>
        /// Called when the application starts.
        /// Loads all birds from the database and populates the <see cref="IBirdStore"/>.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var birds = await _mediator.Send(new GetAllBirdsQuery(), cancellationToken);

            foreach (BirdDTO bird in birds)
                _birdStore.Birds.Add(bird);
        }

        /// <summary>
        /// Called when the application is shutting down.
        /// No logic is required in this case.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

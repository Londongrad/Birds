using Birds.Application.Commands.CreateBird;
using Birds.Application.Commands.DeleteBird;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.UI.Enums;
using Birds.UI.Extensions;
using Birds.UI.Services.Notification;
using Birds.UI.Services.Stores.BirdStore;
using MediatR;

namespace Birds.UI.Services.Managers.Bird
{
    /// <inheritdoc/>
    public class BirdManager(
                       IBirdStore store,
                       BirdStoreInitializer initializer,
                       IMediator mediator,
                       INotificationService notification
        ) : IBirdManager
    {
        private readonly IBirdStore _store = store;
        private readonly BirdStoreInitializer _initializer = initializer;
        private readonly IMediator _mediator = mediator;
        private readonly INotificationService _notification = notification;

        public IBirdStore Store => _store;

        /// <inheritdoc/>
        public async Task ReloadAsync(CancellationToken cancellationToken)
        {
            await _initializer.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<BirdDTO>> AddAsync(BirdCreateDTO newBird, CancellationToken cancellationToken)
        {
            Result<BirdDTO> result = await _mediator.Send(new CreateBirdCommand(newBird.Name, newBird.Description, newBird.Arrival), cancellationToken);

            if (result.IsSuccess)
            {
                if (_store.LoadState == LoadState.Loaded)
                {
                    await AddBirdToStore(result.Value);
                }
                else
                {
                    _notification.ShowInfo("Connection restored. Reloading bird data...");
                    await ReloadAsync(cancellationToken); // Reload data
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<Result> UpdateAsync(BirdDTO updatedBird, CancellationToken cancellationToken)
        {
            var command = new UpdateBirdCommand(
                updatedBird.Id,
                BirdEnumHelper.ParseBirdName(updatedBird.Name) ?? default,
                updatedBird.Description,
                updatedBird.Arrival,
                updatedBird.Departure,
                updatedBird.IsAlive);

            Result result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
                await UpdateBirdInStore(updatedBird);

            return result;
        }

        /// <inheritdoc/>
        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteBirdCommand(id), cancellationToken);

            if (result.IsSuccess)
                await RemoveBirdFromStore(id);

            return result;
        }

        /// <summary>
        /// Executes the specified UI-related action on the main thread.
        /// </summary>
        /// <param name="action">The action to execute on the UI thread.</param>
        private async Task ExecuteOnUiAsync(Action action)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeOnUiAsync(action);
        }

        /// <summary>
        /// Safely adds a new bird to the shared <see cref="IBirdStore"/> collection.
        /// </summary>
        /// <param name="bird">The bird to add.</param>
        private async Task AddBirdToStore(BirdDTO bird)
        {
            await ExecuteOnUiAsync(() => _store.Birds.Add(bird));
        }

        /// <summary>
        /// Safely updates an existing bird within the <see cref="IBirdStore"/> collection.
        /// If the bird does not exist, it will be added.
        /// </summary>
        /// <param name="bird">The bird with updated data.</param>
        private async Task UpdateBirdInStore(BirdDTO bird)
        {
            await ExecuteOnUiAsync(() =>
                _store.Birds.ReplaceOrAdd(b => b.Id == bird.Id, bird));
        }

        /// <summary>
        /// Safely removes a bird from the <see cref="IBirdStore"/> collection by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the bird to remove.</param>
        private async Task RemoveBirdFromStore(Guid id)
        {
            await ExecuteOnUiAsync(() =>
            {
                var toRemove = _store.Birds.FirstOrDefault(b => b.Id == id);
                if (toRemove != null)
                    _store.Birds.Remove(toRemove);
            });
        }
    }
}

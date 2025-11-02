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
    public class BirdManager(
                       IBirdStore store,
                       BirdStoreInitializer initializer,
                       IMediator mediator,
                       INotificationService notification
        ) : IBirdManager
    {
        #region [ Fields ]

        private readonly IBirdStore _store = store;
        private readonly BirdStoreInitializer _initializer = initializer;
        private readonly IMediator _mediator = mediator;
        private readonly INotificationService _notification = notification;

        #endregion [ Fields ]

        #region [ Properties ]

        public IBirdStore Store => _store;

        #endregion [ Properties ]

        #region [ Interface methods ]

        /// <inheritdoc/>
        public async Task ReloadAsync(CancellationToken cancellationToken)
        {
            await _initializer.StartAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<BirdDTO>> AddAsync(BirdCreateDTO newBird, CancellationToken cancellationToken)
        {
            // Handle "uninitialized" or "failed" states
            if (_store.LoadState == LoadState.Uninitialized || _store.LoadState == LoadState.Failed)
            {
                _notification.ShowInfo("Connection restored. Reloading bird data...");
                await ReloadAsync(cancellationToken);   // Force reload
                await WaitUntilStoreLoadedAsync(cancellationToken); // Wait for successful reload
            }
            else if (_store.LoadState == LoadState.Loading)
            {
                // Just wait for the initial load to complete
                await WaitUntilStoreLoadedAsync(cancellationToken);
            }

            Result<BirdDTO> result = await _mediator.Send(
                new CreateBirdCommand(
                    newBird.Name,
                    newBird.Description,
                    newBird.Arrival,
                    newBird.Departure,
                    newBird.IsAlive
                    ), cancellationToken);

            if (!result.IsSuccess)
                return result;

            await AddBirdToStore(result.Value);
            return result;
        }

        /// <inheritdoc/>
        public async Task<Result<BirdDTO>> UpdateAsync(BirdUpdateDTO updatedBird, CancellationToken cancellationToken)
        {
            var command = new UpdateBirdCommand(
                updatedBird.Id,
                BirdEnumHelper.ParseBirdName(updatedBird.Name) ?? default,
                updatedBird.Description,
                updatedBird.Arrival,
                updatedBird.Departure,
                updatedBird.IsAlive);

            Result<BirdDTO> result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
                await UpdateBirdInStore(result.Value);

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

        #endregion [ Interface methods ]

        #region [ Private Helper Methods ]

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

        /// <summary>
        /// Asynchronously waits until the bird store reaches a terminal load state
        /// (<see cref="LoadState.Loaded"/> or <see cref="LoadState.Failed"/>).
        /// <para>
        /// Uses <c>INotifyPropertyChanged</c> (via <c>ObservableObject</c>) to observe
        /// <see cref="IBirdStore.LoadState"/> changes through the <c>PropertyChanged</c> event.
        /// </para>
        /// <para>
        /// Fast path: if the store is already in a terminal state, returns a completed task immediately.
        /// Otherwise, subscribes to <c>PropertyChanged</c> and completes when <see cref="IBirdStore.LoadState"/>
        /// becomes <see cref="LoadState.Loaded"/> or <see cref="LoadState.Failed"/>.
        /// </para>
        /// <para>
        /// Race handling: right after subscribing, performs an extra state check to cover the window
        /// where the state might have changed between the initial check and the subscription (and no event will arrive).
        /// </para>
        /// <para>
        /// Cancellation: if <paramref name="cancellationToken"/> is canceled, unsubscribes and completes the task as canceled.
        /// Continuations run asynchronously due to <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>.
        /// </para>
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to abort waiting.</param>
        /// <returns>
        /// A task that completes when the store reaches <see cref="LoadState.Loaded"/> or <see cref="LoadState.Failed"/>.
        /// </returns>
        private Task WaitUntilLoadedOrFailedAsync(CancellationToken cancellationToken)
        {
            if (_store.LoadState is LoadState.Loaded or LoadState.Failed)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnChanged(object? s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName is nameof(IBirdStore.LoadState) or null)
                {
                    if (_store.LoadState is LoadState.Loaded or LoadState.Failed)
            {
                        _store.PropertyChanged -= OnChanged;
                tcs.TrySetResult();
            }
                }
            }

            _store.PropertyChanged += OnChanged;

            if (_store.LoadState is LoadState.Loaded or LoadState.Failed)
            {
                _store.PropertyChanged -= OnChanged;
                tcs.TrySetResult();
            }

            if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() =>
            {
                    _store.PropertyChanged -= OnChanged;
                    tcs.TrySetCanceled(cancellationToken);
            });

            return tcs.Task;
        }

        #endregion [ Private Helper Methods ]
    }
}
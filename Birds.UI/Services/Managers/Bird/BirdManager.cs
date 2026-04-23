using System.ComponentModel;
using System.Diagnostics;
using Birds.Application.Commands.CreateBird;
using Birds.Application.Commands.DeleteBird;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.UI.Enums;
using Birds.UI.Extensions;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Threading.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;

namespace Birds.UI.Services.Managers.Bird;

public partial class BirdManager(
    IBirdStore store,
    BirdStoreInitializer initializer,
    IMediator mediator,
    IUiDispatcher uiDispatcher,
    INotificationService notificationService,
    IAutoExportCoordinator autoExportCoordinator,
    TimeSpan? pendingDeleteUndoDuration = null)
    : ObservableObject, IBirdManager
{
    private sealed record PendingDeleteContext(BirdDTO Bird, int OriginalIndex)
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new();
    }

    #region [ Fields ]

    private readonly BirdStoreInitializer _initializer = initializer;
    private readonly IMediator _mediator = mediator;
    private readonly IUiDispatcher _uiDispatcher = uiDispatcher;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAutoExportCoordinator _autoExportCoordinator = autoExportCoordinator;
    private PendingDeleteContext? _pendingDelete;

    #endregion [ Fields ]

    #region [ Properties ]

    public IBirdStore Store { get; } = store;

    public bool HasPendingDeleteUndo => _pendingDelete is not null;

    public TimeSpan PendingDeleteUndoDuration { get; } = pendingDeleteUndoDuration ?? TimeSpan.FromSeconds(5);

    [ObservableProperty] private int pendingDeleteUndoVersion;

    #endregion [ Properties ]

    #region [ Interface methods ]

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await _initializer.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<BirdDTO>> AddAsync(BirdCreateDTO newBird, CancellationToken cancellationToken)
    {
        if (Store.LoadState is LoadState.Uninitialized or LoadState.Failed)
            await ReloadAsync(cancellationToken);
        else if (Store.LoadState is LoadState.Loading) await WaitUntilLoadedOrFailedAsync(cancellationToken);

        if (Store.LoadState is not LoadState.Loaded)
            return Result<BirdDTO>.Failure("Bird store cannot be loaded.");

        var result = await _mediator.Send(
            new CreateBirdCommand(
                newBird.Name,
                newBird.Description,
                newBird.Arrival,
                newBird.Departure,
                newBird.IsAlive
            ), cancellationToken);

        if (result.IsSuccess)
        {
            await AddBirdToStore(result.Value);
            _autoExportCoordinator.MarkDirty();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<BirdDTO>> UpdateAsync(BirdUpdateDTO updatedBird, CancellationToken cancellationToken)
    {
        var command = new UpdateBirdCommand(
            updatedBird.Id,
            updatedBird.Species,
            updatedBird.Description,
            updatedBird.Arrival,
            updatedBird.Departure,
            updatedBird.IsAlive);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            await UpdateBirdInStore(result.Value);
            _autoExportCoordinator.MarkDirty();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await CommitPendingDeleteIfAnyAsync(cancellationToken);

        BirdDTO? deletedBird = null;
        var originalIndex = -1;

        await ExecuteOnUiAsync(() =>
        {
            originalIndex = Store.Birds
                .Select((bird, index) => new { bird, index })
                .FirstOrDefault(item => item.bird.Id == id)
                ?.index ?? -1;

            if (originalIndex < 0)
                return;

            deletedBird = Store.Birds[originalIndex];
            Store.Birds.RemoveAt(originalIndex);
        });

        if (deletedBird is null)
        {
            var immediateResult = await _mediator.Send(new DeleteBirdCommand(id), cancellationToken);
            if (immediateResult.IsSuccess)
            {
                _autoExportCoordinator.MarkDirty();
                _notificationService.ShowSuccessLocalized("Info.DeletedBird");
            }

            return immediateResult;
        }

        var context = new PendingDeleteContext(deletedBird, originalIndex);
        await _uiDispatcher.InvokeAsync(() => SetPendingDelete(context), cancellationToken);
        _ = FinalizePendingDeleteAfterDelayAsync(context);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task UndoPendingDeleteAsync(CancellationToken cancellationToken)
    {
        var context = _pendingDelete;
        if (context is null)
            return;

        await _uiDispatcher.InvokeAsync(() => ClearPendingDelete(context), cancellationToken);
        context.CancellationTokenSource.Cancel();
        context.CancellationTokenSource.Dispose();

        await RestoreBirdToStore(context, cancellationToken);
        _notificationService.ShowInfoLocalized("Info.DeleteRestored");
    }

    /// <inheritdoc />
    public async Task FlushPendingOperationsAsync(CancellationToken cancellationToken)
    {
        await CommitPendingDeleteIfAnyAsync(cancellationToken);
    }

    #endregion [ Interface methods ]

    #region [ Private Helper Methods ]

    /// <summary>
    ///     Executes the specified UI-related action on the main thread.
    /// </summary>
    private async Task ExecuteOnUiAsync(Action action)
    {
        await _uiDispatcher.InvokeAsync(action);
    }

    /// <summary>
    ///     Safely adds a new bird to the shared <see cref="IBirdStore" /> collection.
    /// </summary>
    private async Task AddBirdToStore(BirdDTO bird)
    {
        await ExecuteOnUiAsync(() => Store.Birds.Add(bird));
    }

    /// <summary>
    ///     Safely updates an existing bird within the <see cref="IBirdStore" /> collection.
    ///     If the bird does not exist, it will be added.
    /// </summary>
    private async Task UpdateBirdInStore(BirdDTO bird)
    {
        await ExecuteOnUiAsync(() =>
            Store.Birds.ReplaceOrAdd(b => b.Id == bird.Id, bird));
    }

    private async Task RestoreBirdToStore(PendingDeleteContext context, CancellationToken cancellationToken)
    {
        await _uiDispatcher.InvokeAsync(() =>
        {
            if (Store.Birds.Any(b => b.Id == context.Bird.Id))
                return;

            var insertIndex = Math.Clamp(context.OriginalIndex, 0, Store.Birds.Count);
            Store.Birds.Insert(insertIndex, context.Bird);
        }, cancellationToken);
    }

    private void SetPendingDelete(PendingDeleteContext context)
    {
        _pendingDelete = context;
        PendingDeleteUndoVersion++;
        OnPropertyChanged(nameof(HasPendingDeleteUndo));
    }

    private void ClearPendingDelete(PendingDeleteContext context)
    {
        if (!ReferenceEquals(_pendingDelete, context))
            return;

        _pendingDelete = null;
        OnPropertyChanged(nameof(HasPendingDeleteUndo));
    }

    private async Task FinalizePendingDeleteAfterDelayAsync(PendingDeleteContext context)
    {
        try
        {
            await Task.Delay(PendingDeleteUndoDuration, context.CancellationTokenSource.Token);
            await CommitPendingDeleteAsync(context, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Delete was undone or replaced by a new pending operation.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Deferred bird deletion failed unexpectedly: {ex}");
        }
    }

    private async Task CommitPendingDeleteIfAnyAsync(CancellationToken cancellationToken)
    {
        var context = _pendingDelete;
        if (context is null)
            return;

        context.CancellationTokenSource.Cancel();
        await CommitPendingDeleteAsync(context, cancellationToken);
    }

    private async Task CommitPendingDeleteAsync(PendingDeleteContext context, CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(_pendingDelete, context))
            return;

        await _uiDispatcher.InvokeAsync(() => ClearPendingDelete(context), cancellationToken);

        var result = await _mediator.Send(new DeleteBirdCommand(context.Bird.Id), cancellationToken);
        context.CancellationTokenSource.Dispose();

        if (result.IsSuccess)
        {
            _autoExportCoordinator.MarkDirty();
            _notificationService.ShowSuccessLocalized("Info.DeletedBird");
            return;
        }

        await RestoreBirdToStore(context, CancellationToken.None);
        _notificationService.ShowErrorLocalized("Error.CannotDeleteBird");
    }

    /// <summary>
    ///     Asynchronously waits until the bird store reaches a terminal load state.
    /// </summary>
    private Task WaitUntilLoadedOrFailedAsync(CancellationToken cancellationToken)
    {
        if (Store.LoadState is LoadState.Loaded or LoadState.Failed)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IBirdStore.LoadState) or null)
                if (Store.LoadState is LoadState.Loaded or LoadState.Failed)
                {
                    Store.PropertyChanged -= OnChanged;
                    tcs.TrySetResult();
                }
        }

        Store.PropertyChanged += OnChanged;

        if (Store.LoadState is LoadState.Loaded or LoadState.Failed)
        {
            Store.PropertyChanged -= OnChanged;
            tcs.TrySetResult();
        }

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() =>
            {
                Store.PropertyChanged -= OnChanged;
                tcs.TrySetCanceled(cancellationToken);
            });

        return tcs.Task;
    }

    #endregion [ Private Helper Methods ]
}

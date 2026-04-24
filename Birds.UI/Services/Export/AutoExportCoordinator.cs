using System.ComponentModel;
using Birds.Application.DTOs;
using Birds.Shared.Constants;
using Birds.UI.Services.Background;
using Birds.UI.Services.Export.Interfaces;
using Birds.UI.Services.Preferences.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using Birds.UI.Threading.Abstractions;
using Microsoft.Extensions.Logging;

namespace Birds.UI.Services.Export;

public sealed class AutoExportCoordinator : IAutoExportCoordinator, IDisposable
{
    private readonly IBirdStore _birdStore;
    private readonly TimeSpan _debounceDelay;
    private readonly IBackgroundTaskRunner _backgroundTaskRunner;
    private readonly object _debounceSync = new();
    private readonly IExportPathProvider _exportPathProvider;
    private readonly IExportService _exportService;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly ILogger<AutoExportCoordinator> _logger;
    private readonly IAppPreferencesService _preferences;
    private readonly IUiDispatcher _uiDispatcher;

    private CancellationTokenSource? _debounceCancellation;
    private int _dirtyVersion;
    private bool _disposed;
    private int _exportedVersion;

    public AutoExportCoordinator(
        IBirdStore birdStore,
        IExportService exportService,
        IExportPathProvider exportPathProvider,
        IAppPreferencesService preferences,
        IUiDispatcher uiDispatcher,
        ILogger<AutoExportCoordinator> logger,
        IBackgroundTaskRunner backgroundTaskRunner,
        TimeSpan? debounceDelay = null)
    {
        _birdStore = birdStore;
        _exportService = exportService;
        _exportPathProvider = exportPathProvider;
        _preferences = preferences;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        _backgroundTaskRunner = backgroundTaskRunner;
        _debounceDelay = debounceDelay ?? TimeSpan.FromSeconds(2);
        _preferences.PropertyChanged += OnPreferencesChanged;
    }

    public void MarkDirty()
    {
        ThrowIfDisposed();

        if (!_preferences.AutoExportEnabled)
            return;

        Interlocked.Increment(ref _dirtyVersion);
        ScheduleDebouncedFlush();
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        CancelPendingDebounce();

        if (!_preferences.AutoExportEnabled)
            return;

        await FlushCoreAsync(cancellationToken, true).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _preferences.PropertyChanged -= OnPreferencesChanged;
        CancelPendingDebounce();
        _flushLock.Dispose();
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAppPreferencesService.AutoExportEnabled))
        {
            if (_preferences.AutoExportEnabled)
                MarkDirty();
            else
                CancelPendingDebounce();

            return;
        }

        if (e.PropertyName == nameof(IAppPreferencesService.CustomExportPath)
            && _preferences.AutoExportEnabled)
            MarkDirty();
    }

    private void ScheduleDebouncedFlush()
    {
        CancellationTokenSource debounceCancellation;

        lock (_debounceSync)
        {
            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = new CancellationTokenSource();
            debounceCancellation = _debounceCancellation;
        }

        _backgroundTaskRunner.Run(
            _ => DebouncedFlushAsync(debounceCancellation),
            new BackgroundTaskOptions("Debounced auto-export"),
            debounceCancellation.Token);
    }

    private async Task DebouncedFlushAsync(CancellationTokenSource debounceCancellation)
    {
        try
        {
            await Task.Delay(_debounceDelay, debounceCancellation.Token).ConfigureAwait(false);
            await FlushCoreAsync(debounceCancellation.Token, false).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (debounceCancellation.IsCancellationRequested)
        {
            // Newer export request replaced this one.
        }
    }

    private async Task FlushCoreAsync(CancellationToken cancellationToken, bool throwOnFailure)
    {
        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (true)
            {
                var targetVersion = Volatile.Read(ref _dirtyVersion);
                if (targetVersion <= Volatile.Read(ref _exportedVersion))
                    return;

                var snapshot = await CaptureSnapshotAsync(cancellationToken).ConfigureAwait(false);
                var exportPath = ResolveExportPath();

                await _exportService.ExportAsync(snapshot, exportPath, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(LogMessages.AutoExportSucceeded, exportPath);

                Interlocked.Exchange(ref _exportedVersion, targetVersion);

                if (Volatile.Read(ref _dirtyVersion) == targetVersion)
                    return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LogMessages.AutoExportFailed);
            if (throwOnFailure)
                throw;
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private async Task<IReadOnlyList<BirdDTO>> CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        BirdDTO[] snapshot = [];
        await _uiDispatcher.InvokeAsync(() => { snapshot = _birdStore.Birds.ToArray(); }, cancellationToken)
            .ConfigureAwait(false);

        return snapshot;
    }

    private string ResolveExportPath()
    {
        return string.IsNullOrWhiteSpace(_preferences.CustomExportPath)
            ? _exportPathProvider.GetLatestPath("birds")
            : _preferences.CustomExportPath;
    }

    private void CancelPendingDebounce()
    {
        CancellationTokenSource? debounceCancellation;
        lock (_debounceSync)
        {
            debounceCancellation = _debounceCancellation;
            _debounceCancellation = null;
        }

        debounceCancellation?.Cancel();
        debounceCancellation?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(AutoExportCoordinator));
    }
}

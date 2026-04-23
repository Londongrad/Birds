using Birds.Application.DTOs;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Caching;

public sealed class BirdViewModelCache : IBirdViewModelCache
{
    public const int DefaultMaxSize = 300;

    private readonly Dictionary<Guid, CacheEntry> _cache = [];
    private readonly IBirdViewModelFactory _factory;
    private readonly LinkedList<Guid> _lru = [];
    private bool _disposed;

    public BirdViewModelCache(IBirdViewModelFactory factory, int maxSize = DefaultMaxSize)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "Cache size must be positive.");

        MaxSize = maxSize;
    }

    public int MaxSize { get; }

    public int Count => _cache.Count;

    public BirdViewModel GetOrCreate(BirdDTO dto)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(dto);

        if (_cache.TryGetValue(dto.Id, out var entry))
        {
            Touch(entry);
            Refresh(entry.ViewModel, dto);
            return entry.ViewModel;
        }

        var viewModel = _factory.Create(dto);
        var node = _lru.AddLast(dto.Id);
        _cache.Add(dto.Id, new CacheEntry(viewModel, node));
        EvictIfNeeded(dto.Id);

        return viewModel;
    }

    public void Refresh(BirdDTO dto)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(dto);

        if (_cache.TryGetValue(dto.Id, out var entry))
            Refresh(entry.ViewModel, dto);
    }

    public void Remove(Guid birdId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_cache.Remove(birdId, out var entry))
            return;

        _lru.Remove(entry.Node);
        entry.ViewModel.Dispose();
    }

    public void Clear()
    {
        foreach (var entry in _cache.Values)
            entry.ViewModel.Dispose();

        _cache.Clear();
        _lru.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Clear();
        _disposed = true;
    }

    private static void Refresh(BirdViewModel viewModel, BirdDTO dto)
    {
        if (!viewModel.IsEditing)
            viewModel.UpdateFromDto(dto);
    }

    private void Touch(CacheEntry entry)
    {
        _lru.Remove(entry.Node);
        _lru.AddLast(entry.Node);
    }

    private void EvictIfNeeded(Guid protectedBirdId)
    {
        while (_cache.Count > MaxSize)
        {
            var evictableNode = FindLeastRecentlyUsedEvictableNode(protectedBirdId);
            if (evictableNode is null)
                return;

            Remove(evictableNode.Value);
        }
    }

    private LinkedListNode<Guid>? FindLeastRecentlyUsedEvictableNode(Guid protectedBirdId)
    {
        for (var node = _lru.First; node is not null; node = node.Next)
            if (node.Value != protectedBirdId
                && _cache.TryGetValue(node.Value, out var entry)
                && !entry.ViewModel.IsEditing)
                return node;

        return null;
    }

    private sealed record CacheEntry(BirdViewModel ViewModel, LinkedListNode<Guid> Node);
}

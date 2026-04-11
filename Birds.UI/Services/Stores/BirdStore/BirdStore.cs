using System.Collections.ObjectModel;
using Birds.Application.DTOs;
using Birds.UI.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Stores.BirdStore;

/// <summary>
///     Centralized store that holds the current collection of birds
///     and represents the overall data loading state.
/// </summary>
public partial class BirdStore : ObservableObject, IBirdStore
{
    private readonly RangeObservableCollection<BirdDTO> _birds = [];

    [ObservableProperty] private LoadState loadState = LoadState.Uninitialized;

    /// <inheritdoc />
    public ObservableCollection<BirdDTO> Birds => _birds;

    /// <inheritdoc />
    public void BeginLoading()
    {
        LoadState = LoadState.Loading;
    }

    /// <inheritdoc />
    public void ReplaceBirds(IEnumerable<BirdDTO> birds)
    {
        _birds.ReplaceAll(birds);
    }

    /// <inheritdoc />
    public void CompleteLoading()
    {
        LoadState = LoadState.Loaded;
    }

    /// <inheritdoc />
    public void FailLoading()
    {
        LoadState = LoadState.Failed;
    }
}
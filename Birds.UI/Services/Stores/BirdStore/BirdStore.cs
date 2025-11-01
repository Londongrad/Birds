using Birds.Application.DTOs;
using Birds.UI.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Centralized store that holds the current collection of birds
    /// and represents the overall data loading state.
    /// </summary>
    public partial class BirdStore : ObservableObject, IBirdStore
    {
        [ObservableProperty]
        private LoadState loadState = LoadState.Uninitialized;

        /// <inheritdoc/>
        public ObservableCollection<BirdDTO> Birds { get; } = [];

        /// <inheritdoc/>
        public void BeginLoading() => LoadState = LoadState.Loading;

        /// <inheritdoc/>
        public void CompleteLoading() => LoadState = LoadState.Loaded;

        /// <inheritdoc/>
        public void FailLoading() => LoadState = LoadState.Failed;
    }
}
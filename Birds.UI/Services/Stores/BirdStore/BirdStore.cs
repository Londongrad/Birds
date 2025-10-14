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

        /// <summary>
        /// Gets the shared collection of all loaded birds.
        /// </summary>
        public ObservableCollection<BirdDTO> Birds { get; } = [];

        /// <summary>
        /// Updates the store state when a new loading operation begins.
        /// </summary>
        public void BeginLoading() => LoadState = LoadState.Loading;

        /// <summary>
        /// Updates the store state when loading completes successfully.
        /// </summary>
        public void CompleteLoading() => LoadState = LoadState.Loaded;

        /// <summary>
        /// Updates the store state when a loading error occurs.
        /// </summary>
        public void FailLoading() => LoadState = LoadState.Failed;
    }
}

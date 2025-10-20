using Birds.Application.DTOs;
using Birds.UI.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Interface for the shared bird data store.
    /// Provides access to the <see cref="BirdDTO"/> collection,
    /// which can be used across all ViewModels in the application.
    /// <para>Has observable loading state.</para>
    /// </summary>
    public interface IBirdStore : INotifyPropertyChanged
    {
        /// <summary>
        /// The shared collection of birds, available for reading and modification.
        /// Used to synchronize data between different views.
        /// </summary>
        ObservableCollection<BirdDTO> Birds { get; }

        /// <summary>
        /// Gets or sets the current loading state of the bird store.
        /// </summary>
        LoadState LoadState { get; set; }

        /// <summary>
        /// Occurs when the bird store has finished loading all data
        /// and is ready for use.
        /// </summary>
        event Action? StoreLoaded;

        /// <summary>
        /// Updates the store state when a new loading operation begins.
        /// </summary>
        void BeginLoading();

        /// <summary>
        /// Updates the store state when loading completes successfully.
        /// </summary>
        void CompleteLoading();

        /// <summary>
        /// Updates the store state when a loading error occurs.
        /// </summary>
        void FailLoading();
    }
}

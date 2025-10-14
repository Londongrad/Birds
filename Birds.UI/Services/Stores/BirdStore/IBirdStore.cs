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

        void BeginLoading();
        void CompleteLoading();
        void FailLoading();
    }
}

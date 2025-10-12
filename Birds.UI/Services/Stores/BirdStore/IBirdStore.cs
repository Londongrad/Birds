using Birds.Application.DTOs;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Interface for the shared bird data store.
    /// Provides access to the <see cref="BirdDTO"/> collection,
    /// which can be used across all ViewModels in the application.
    /// </summary>
    public interface IBirdStore
    {
        /// <summary>
        /// The shared collection of birds, available for reading and modification.
        /// Used to synchronize data between different views.
        /// </summary>
        ObservableCollection<BirdDTO> Birds { get; }
    }
}

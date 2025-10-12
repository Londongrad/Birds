using Birds.Application.DTOs;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Implementation of a shared data store for bird information.
    /// Contains a single <see cref="BirdDTO"/> collection
    /// that persists throughout the entire application lifetime.
    /// </summary>
    public class BirdStore : IBirdStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BirdStore"/> class.
        /// During initialization, an empty <see cref="BirdDTO"/> collection is created.
        /// </summary>
        public BirdStore()
        {
            Birds = new ObservableCollection<BirdDTO>();
        }

        /// <summary>
        /// Общая коллекция птиц, разделяемая всеми ViewModel.
        /// Добавление, удаление или изменение элементов коллекции 
        /// будет отражаться во всех местах, где используется <see cref="BirdStore"/>.
        /// <summary>
        /// The shared collection of birds accessible to all ViewModels.
        /// Adding, removing, or modifying elements in this collection
        /// will be reflected in all parts of the application using <see cref="BirdStore"/>.
        /// </summary>
        public ObservableCollection<BirdDTO> Birds { get; }
    }
}

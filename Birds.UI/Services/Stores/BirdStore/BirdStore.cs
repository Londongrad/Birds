using Birds.Application.DTOs;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Реализация общего хранилища данных о птицах.
    /// Содержит одну коллекцию <see cref="BirdDTO"/>, 
    /// которая живёт в течение всего времени работы приложения.
    /// </summary>
    public class BirdStore : IBirdStore
    {
        /// <summary>
        /// Создаёт новый экземпляр <see cref="BirdStore"/>.
        /// При инициализации формируется пустая коллекция <see cref="BirdDTO"/>.
        /// </summary>
        public BirdStore()
        {
            Birds = new ObservableCollection<BirdDTO>();
        }

        /// <summary>
        /// Общая коллекция птиц, разделяемая всеми ViewModel.
        /// Добавление, удаление или изменение элементов коллекции 
        /// будет отражаться во всех местах, где используется <see cref="BirdStore"/>.
        /// </summary>
        public ObservableCollection<BirdDTO> Birds { get; }
    }
}

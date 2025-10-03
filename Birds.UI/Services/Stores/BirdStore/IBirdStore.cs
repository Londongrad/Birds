using Birds.Application.DTOs;
using System.Collections.ObjectModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    /// <summary>
    /// Интерфейс общего хранилища данных о птицах.
    /// Предоставляет доступ к коллекции <see cref="BirdDTO"/>, 
    /// которая может использоваться во всех ViewModel приложения.
    /// </summary>
    public interface IBirdStore
    {
        /// <summary>
        /// Общая коллекция птиц, доступная для чтения и изменения.
        /// Используется для синхронизации данных между разными представлениями.
        /// </summary>
        ObservableCollection<BirdDTO> Birds { get; }
    }
}

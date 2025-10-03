using Birds.Application.DTOs;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory
{
    public interface IBirdViewModelFactory
    {
        /// <summary>
        /// Создаёт новый экземпляр <see cref="BirdViewModel"/> на основе переданного <see cref="BirdDTO"/>.
        /// </summary>
        /// <param name="dto">Объект передачи данных, содержащий информацию о птице.</param>
        /// <returns>
        /// Новый экземпляр <see cref="BirdViewModel"/>, инициализированный данными из <paramref name="dto"/>
        /// и подключённый к механизму медиатора приложения.
        /// </returns>
        BirdViewModel Create(BirdDTO dto);
    }
}

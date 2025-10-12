using Birds.Application.DTOs;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory
{
    public interface IBirdViewModelFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="BirdViewModel"/> based on the provided <see cref="BirdDTO"/>.
        /// </summary>
        /// <param name="dto">The data transfer object containing information about the bird.</param>
        /// <returns>
        /// A new instance of <see cref="BirdViewModel"/>, initialized with the data from <paramref name="dto"/>
        /// and connected to the application's mediator mechanism.
        /// </returns>
        BirdViewModel Create(BirdDTO dto);
    }
}

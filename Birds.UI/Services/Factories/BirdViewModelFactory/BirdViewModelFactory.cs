using Birds.Application.DTOs;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory
{
    /// <summary>
    /// Factory responsible for creating instances of <see cref="BirdViewModel"/>
    /// from data transfer objects (<see cref="BirdDTO"/>).
    ///
    /// <para>
    /// This class ensures that each created <see cref="BirdViewModel"/> is
    /// properly initialized with required dependencies such as
    /// <see cref="IBirdManager"/> and <see cref="INotificationService"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <see cref="IBirdManager"/> provides access to shared data operations
    /// and the centralized bird store, while <see cref="INotificationService"/>
    /// is used to display messages and feedback to the user.
    /// </remarks>
    public class BirdViewModelFactory(
        IBirdManager birdManager, 
        INotificationService notificationService) : IBirdViewModelFactory
    {
        private readonly IBirdManager _birdManager = birdManager 
            ?? throw new ArgumentNullException(nameof(birdManager));

        private readonly INotificationService _notificationService = notificationService 
            ?? throw new ArgumentNullException(nameof(notificationService));

        /// <inheritdoc/>
        public BirdViewModel Create(BirdDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return new(dto, _birdManager, _notificationService);
        }
    }
}

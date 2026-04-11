using Birds.Application.DTOs;
using Birds.UI.Services.Localization.Interfaces;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;

namespace Birds.UI.Services.Factories.BirdViewModelFactory;

/// <summary>
///     Factory responsible for creating instances of <see cref="BirdViewModel" />
///     from data transfer objects (<see cref="BirdDTO" />).
/// </summary>
public class BirdViewModelFactory(
    IBirdManager birdManager,
    ILocalizationService localization,
    INotificationService notificationService) : IBirdViewModelFactory
{
    private readonly IBirdManager _birdManager = birdManager
                                                 ?? throw new ArgumentNullException(nameof(birdManager));

    private readonly ILocalizationService _localization = localization
                                                          ?? throw new ArgumentNullException(nameof(localization));

    private readonly INotificationService _notificationService = notificationService
                                                                 ?? throw new ArgumentNullException(
                                                                     nameof(notificationService));

    /// <inheritdoc />
    public BirdViewModel Create(BirdDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new BirdViewModel(dto, _birdManager, _localization, _notificationService);
    }
}
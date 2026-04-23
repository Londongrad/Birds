using Birds.Application.DTOs.Helpers;
using Birds.Domain.Enums;
using Birds.UI.Services.Localization.Interfaces;

namespace Birds.UI.Services.BirdNames;

public sealed class BirdNameDisplayService(ILocalizationService localization) : IBirdNameDisplayService
{
    private readonly ILocalizationService _localization = localization;

    public string GetDisplayName(BirdSpecies bird)
    {
        return BirdNameDisplayNames.GetDisplayName(bird, _localization.CurrentCulture);
    }
}

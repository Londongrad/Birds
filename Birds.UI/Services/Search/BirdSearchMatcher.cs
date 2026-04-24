using System.Globalization;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization.Interfaces;

namespace Birds.UI.Services.Search;

public sealed class BirdSearchMatcher(
    ILocalizationService localization,
    IBirdNameDisplayService birdNameDisplay) : IBirdSearchMatcher
{
    private readonly IBirdNameDisplayService _birdNameDisplay = birdNameDisplay;
    private readonly ILocalizationService _localization = localization;

    public string NormalizeQuery(string? query)
    {
        return query?.Trim() ?? string.Empty;
    }

    public bool Matches(BirdDTO bird, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return true;

        var species = bird.ResolveSpecies();
        var localizedName = species.HasValue
            ? _birdNameDisplay.GetDisplayName(species.Value)
            : bird.Name;

        return Contains(localizedName, normalizedQuery)
               || Contains(bird.Name, normalizedQuery)
               || Contains(_localization.FormatDate(bird.Arrival), normalizedQuery)
               || (bird.Departure is { } departure && Contains(_localization.FormatDate(departure), normalizedQuery))
               || Contains(bird.Description, normalizedQuery);
    }

    private bool Contains(string? value, string query)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return _localization.CurrentCulture.CompareInfo.IndexOf(
            value,
            query,
            CompareOptions.IgnoreCase) >= 0;
    }
}

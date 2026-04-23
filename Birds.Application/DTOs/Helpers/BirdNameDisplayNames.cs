using System.Globalization;
using Birds.Domain.Enums;
using Birds.Shared.Localization;

namespace Birds.Application.DTOs.Helpers;

public static class BirdNameDisplayNames
{
    public static string GetDisplayName(BirdSpecies bird, CultureInfo? culture = null)
    {
        return Enum.IsDefined(bird)
            ? AppText.Get($"BirdName.{bird}", culture)
            : AppText.Get("BirdName.Unknown", culture);
    }
}

using System.Globalization;
using Birds.Domain.Enums;
using Birds.Shared.Localization;

namespace Birds.Application.DTOs.Helpers;

/// <summary>
///     Provides utility methods for safely converting between string values
///     and <see cref="BirdSpecies" /> enumeration members.
/// </summary>
/// <remarks>
///     New application paths should use stable <see cref="BirdSpecies" /> values directly.
///     String parsing remains for legacy imports and compatibility with older DTO payloads.
/// </remarks>
public static class BirdEnumHelper
{
    /// <summary>
    ///     Attempts to parse a string into a <see cref="BirdSpecies" /> value.
    /// </summary>
    /// <param name="name">The string representation of the bird's name.</param>
    /// <returns>
    ///     The corresponding <see cref="BirdSpecies" /> if the conversion succeeds;
    ///     otherwise, <see langword="null" />.
    /// </returns>
    public static BirdSpecies? ParseBirdName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (BirdSpeciesCodes.TryParse(name, out var result))
            return result;

        foreach (var bird in Enum.GetValues<BirdSpecies>())
            if (string.Equals(name, BirdNameDisplayNames.GetDisplayName(
                    bird,
                    CultureInfo.GetCultureInfo(AppLanguages.Russian)),
                    StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(name, BirdNameDisplayNames.GetDisplayName(
                        bird,
                        CultureInfo.GetCultureInfo(AppLanguages.English)),
                    StringComparison.CurrentCultureIgnoreCase))
                return bird;

        return null;
    }
}

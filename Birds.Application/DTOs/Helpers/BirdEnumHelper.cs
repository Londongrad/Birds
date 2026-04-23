using System.Globalization;
using Birds.Domain.Enums;
using Birds.Shared.Localization;

namespace Birds.Application.DTOs.Helpers;

/// <summary>
///     Provides utility methods for safely converting between string values
///     and <see cref="BirdsName" /> enumeration members.
/// </summary>
/// <remarks>
///     This helper is primarily used when mapping between <c>BirdDTO</c> objects
///     (which store the bird name as a <see cref="string" />) and domain models
///     (which use the strongly typed <see cref="BirdsName" /> enum).
/// </remarks>
public static class BirdEnumHelper
{
    /// <summary>
    ///     Attempts to parse a string into a <see cref="BirdsName" /> value.
    /// </summary>
    /// <param name="name">The string representation of the bird's name.</param>
    /// <returns>
    ///     The corresponding <see cref="BirdsName" /> if the conversion succeeds;
    ///     otherwise, <see langword="null" />.
    /// </returns>
    public static BirdsName? ParseBirdName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (Enum.TryParse<BirdsName>(name, out var result))
            return result;

        foreach (var bird in Enum.GetValues<BirdsName>())
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

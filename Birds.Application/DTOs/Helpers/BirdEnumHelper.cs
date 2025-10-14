using Birds.Domain.Enums;

namespace Birds.Application.DTOs.Helpers
{
    /// <summary>
    /// Provides utility methods for safely converting between string values
    /// and <see cref="BirdsName"/> enumeration members.
    /// </summary>
    /// <remarks>
    /// This helper is primarily used when mapping between <c>BirdDTO</c> objects
    /// (which store the bird name as a <see cref="string"/>) and domain models
    /// (which use the strongly typed <see cref="BirdsName"/> enum).
    /// </remarks>
    public static class BirdEnumHelper
    {
        /// <summary>
        /// Attempts to parse a string into a <see cref="BirdsName"/> value.
        /// </summary>
        /// <param name="name">The string representation of the bird's name.</param>
        /// <returns>
        /// The corresponding <see cref="BirdsName"/> if the conversion succeeds;
        /// otherwise, <see langword="null"/>.
        /// </returns>
        public static BirdsName? ParseBirdName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return Enum.TryParse<BirdsName>(name, out var result) ? result : null;
        }
    }
}
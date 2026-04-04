using Birds.Domain.Enums;
using Birds.Domain.Extensions;

namespace Birds.UI.Enums
{
    public enum BirdFilter
    {
        All = 0,
        Alive = 1,
        DepartedButAlive = 2,
        BySpecies = 3,
        Dead = 4
    }

    public record FilterOption(BirdFilter Filter, string Display, BirdsName? Species = null)
    {
        public static FilterOption SpeciesFilter(BirdsName species) =>
            new(BirdFilter.BySpecies, species.ToDisplayName(), species);

        public override string ToString() => Display;
    }
}

using Birds.Domain.Enums;

namespace Birds.UI.Enums;

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
    public override string ToString()
    {
        return Display;
    }
}

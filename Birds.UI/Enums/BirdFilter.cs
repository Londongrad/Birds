namespace Birds.UI.Enums
{
    public enum BirdFilter
    {
        All = 0,
        Alive = 1,
        DepartedButAlive = 2,
        Amadin = 3,
        Sparrow = 4,
        GreatTit = 5,
        Chickadee = 6,
        Goldfinch = 7,
        Nuthatch = 8,
        Grosbeak = 9,
        Dead = 10
    }

    public record FilterOption(BirdFilter Filter, string Display)
    {
        public override string ToString() => Display;
    }
}

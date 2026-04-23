namespace Birds.Domain.Enums;

public enum BirdSpecies
{
    Sparrow = 1,
    Goldfinch = 2,
    Amadin = 3,
    Hawfinch = 4,
    GreatTit = 5,
    BlackCappedChickadee = 6,
    Nuthatch = 7
}

public static class BirdSpeciesCodes
{
    private static readonly IReadOnlyDictionary<string, BirdSpecies> LegacyCodes =
        new Dictionary<string, BirdSpecies>(StringComparer.OrdinalIgnoreCase)
        {
            ["Воробей"] = BirdSpecies.Sparrow,
            ["Щегол"] = BirdSpecies.Goldfinch,
            ["Амадин"] = BirdSpecies.Amadin,
            ["Дубонос"] = BirdSpecies.Hawfinch,
            ["Большак"] = BirdSpecies.GreatTit,
            ["Гайка"] = BirdSpecies.BlackCappedChickadee,
            ["Поползень"] = BirdSpecies.Nuthatch
        };

    public static string ToCode(BirdSpecies species)
    {
        return species.ToString();
    }

    public static BirdSpecies? Parse(string? value)
    {
        return TryParse(value, out var species)
            ? species
            : null;
    }

    public static bool TryParse(string? value, out BirdSpecies species)
    {
        species = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (Enum.TryParse<BirdSpecies>(trimmed, true, out var parsed) && Enum.IsDefined(parsed))
        {
            species = parsed;
            return true;
        }

        if (int.TryParse(trimmed, out var numeric) && Enum.IsDefined(typeof(BirdSpecies), numeric))
        {
            species = (BirdSpecies)numeric;
            return true;
        }

        return LegacyCodes.TryGetValue(trimmed, out species);
    }
}

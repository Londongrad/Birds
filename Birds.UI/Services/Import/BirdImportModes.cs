using Birds.Application.Commands.ImportBirds;

namespace Birds.UI.Services.Import;

public static class BirdImportModes
{
    public const string Merge = "Merge";
    public const string Replace = "Replace";

    public static IReadOnlyList<string> SupportedModes { get; } =
    [
        Merge,
        Replace
    ];

    public static string Normalize(string? value)
    {
        return string.Equals(value, Replace, StringComparison.OrdinalIgnoreCase)
            ? Replace
            : Merge;
    }

    public static BirdImportMode ToCommandMode(string? value)
    {
        return Normalize(value) == Replace
            ? BirdImportMode.Replace
            : BirdImportMode.Merge;
    }
}
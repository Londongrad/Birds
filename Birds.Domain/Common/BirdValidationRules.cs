namespace Birds.Domain.Common;

public static class BirdValidationRules
{
    public const int DescriptionMaxLength = 200;
    public const int MinimumArrivalYear = 2020;

    public static readonly DateOnly MinimumArrivalDate = new(MinimumArrivalYear, 1, 1);
    public static readonly DateTime MinimumTimestamp = new(MinimumArrivalYear, 1, 1);
}

namespace Birds.Domain.Common;

using Birds.Domain.Enums;

public static class BirdValidationRules
{
    public const int DescriptionMaxLength = 200;
    public const int MinimumArrivalYear = 2020;
    public const long MinimumVersion = 1;

    public static readonly DateOnly MinimumArrivalDate = new(MinimumArrivalYear, 1, 1);
    public static readonly DateTime MinimumTimestamp = new(MinimumArrivalYear, 1, 1);

    public static DateOnly CurrentLocalDate()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    public static bool IsDefinedSpecies(BirdSpecies species)
    {
        return IsDefinedEnum(species);
    }

    public static bool IsDefinedEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return Enum.IsDefined(value);
    }

    public static bool IsDescriptionLengthValid(string? description)
    {
        return description is null || description.Length <= DescriptionMaxLength;
    }

    public static bool IsDateInAllowedRange(DateOnly value, bool allowFuture = false, DateOnly? today = null)
    {
        if (value == default)
            return false;

        if (value < MinimumArrivalDate)
            return false;

        return allowFuture || value <= (today ?? CurrentLocalDate());
    }

    public static bool IsOptionalDateInAllowedRange(DateOnly? value, bool allowFuture = false, DateOnly? today = null)
    {
        return value is null || IsDateInAllowedRange(value.Value, allowFuture, today);
    }

    public static bool IsTimestampInAllowedRange(DateTime value, bool allowFuture = false, DateTime? now = null)
    {
        if (value == default)
            return false;

        if (value < MinimumTimestamp)
            return false;

        return allowFuture || value <= (now ?? DateTime.Now);
    }

    public static bool IsDateRangeValid(DateOnly arrival, DateOnly? departure)
    {
        return departure is null || arrival <= departure;
    }

    public static bool HasRequiredDeparture(DateOnly? departure, bool isAlive)
    {
        return isAlive || departure is not null;
    }

    public static bool IsDepartureStateValid(DateOnly? departure, bool isAlive)
    {
        return IsOptionalDateInAllowedRange(departure) && HasRequiredDeparture(departure, isAlive);
    }

    public static bool IsVersionValid(long version)
    {
        return version >= MinimumVersion;
    }
}

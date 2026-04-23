namespace Birds.Infrastructure.Persistence.Models;

internal static class UtcDateTimeStorage
{
    public static DateTime Normalize(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime NormalizeForStorage(DateTime value)
    {
        return DateTime.SpecifyKind(Normalize(value), DateTimeKind.Unspecified);
    }

    public static DateTime? NormalizeForStorage(DateTime? value)
    {
        return value.HasValue ? NormalizeForStorage(value.Value) : null;
    }

    public static DateTime FromStorage(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static DateTime? FromStorage(DateTime? value)
    {
        return value.HasValue ? FromStorage(value.Value) : null;
    }
}

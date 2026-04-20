namespace Birds.Shared.Sync;

public static class RemoteSyncIntervalPresets
{
    public const string FiveSeconds = "5";
    public const string TenSeconds = "10";
    public const string ThirtySeconds = "30";
    public const string OneMinute = "60";
    public const string Default = TenSeconds;

    private static readonly HashSet<string> SupportedValues =
    [
        FiveSeconds,
        TenSeconds,
        ThirtySeconds,
        OneMinute
    ];

    public static string Normalize(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && SupportedValues.Contains(value)
            ? value
            : Default;
    }

    public static TimeSpan ToTimeSpan(string? value)
    {
        return Normalize(value) switch
        {
            FiveSeconds => TimeSpan.FromSeconds(5),
            ThirtySeconds => TimeSpan.FromSeconds(30),
            OneMinute => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromSeconds(10)
        };
    }
}

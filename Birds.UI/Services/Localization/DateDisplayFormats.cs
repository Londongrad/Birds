using System.Globalization;

namespace Birds.UI.Services.Localization
{
    public enum DateDisplayStyle
    {
        Short,
        Medium,
        Long,
        MonthYearShort,
        MonthYearLong
    }

    public static class DateDisplayFormats
    {
        public const string DayMonthYear = "DMY";
        public const string MonthDayYear = "MDY";
        public const string YearMonthDay = "YMD";
        public const string Default = DayMonthYear;

        public static IReadOnlyList<string> SupportedFormats { get; } =
        [
            DayMonthYear,
            MonthDayYear,
            YearMonthDay
        ];

        public static string Normalize(string? value)
            => value?.Trim().ToUpperInvariant() switch
            {
                MonthDayYear => MonthDayYear,
                YearMonthDay => YearMonthDay,
                _ => DayMonthYear
            };

        public static string GetShortPattern(string? format)
            => Normalize(format) switch
            {
                MonthDayYear => "MM/dd/yyyy",
                YearMonthDay => "yyyy-MM-dd",
                _ => "dd.MM.yyyy"
            };

        public static string GetLongDatePattern(string? format)
            => Normalize(format) switch
            {
                MonthDayYear => "MMMM dd yyyy",
                YearMonthDay => "yyyy MMMM dd",
                _ => "dd MMMM yyyy"
            };

        public static string FormatDate(DateOnly value, CultureInfo culture, string? format, DateDisplayStyle style = DateDisplayStyle.Short)
        {
            var dateTime = value.ToDateTime(TimeOnly.MinValue);
            return dateTime.ToString(GetPattern(format, style), culture);
        }

        public static string FormatDateTime(DateTime value, CultureInfo culture, string? format)
        {
            var pattern = $"{GetShortPattern(format)} {culture.DateTimeFormat.ShortTimePattern}";
            return value.ToString(pattern, culture);
        }

        public static void ApplyToCulture(CultureInfo culture, string? format)
        {
            culture.DateTimeFormat.ShortDatePattern = GetShortPattern(format);
            culture.DateTimeFormat.LongDatePattern = GetLongDatePattern(format);
        }

        private static string GetPattern(string? format, DateDisplayStyle style)
        {
            var normalized = Normalize(format);

            return style switch
            {
                DateDisplayStyle.Medium => normalized switch
                {
                    MonthDayYear => "MMM dd yyyy",
                    YearMonthDay => "yyyy MMM dd",
                    _ => "dd MMM yyyy"
                },
                DateDisplayStyle.Long => GetLongDatePattern(normalized),
                DateDisplayStyle.MonthYearShort => normalized switch
                {
                    YearMonthDay => "yyyy MMM",
                    _ => "MMM yyyy"
                },
                DateDisplayStyle.MonthYearLong => normalized switch
                {
                    YearMonthDay => "yyyy MMMM",
                    _ => "MMMM yyyy"
                },
                _ => GetShortPattern(normalized)
            };
        }
    }
}

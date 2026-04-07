using System.Globalization;

namespace Birds.Shared.Localization
{
    public static class AppLanguages
    {
        public const string Russian = "ru-RU";
        public const string English = "en-US";

        public static IReadOnlyList<string> SupportedLanguages { get; } =
            [Russian, English];

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Russian;

            return value.Trim() switch
            {
                Russian or "ru" or "Русский" => Russian,
                English or "en" or "English" => English,
                _ => Russian
            };
        }

        public static CultureInfo ToCulture(string? value)
            => CultureInfo.GetCultureInfo(Normalize(value));
    }
}

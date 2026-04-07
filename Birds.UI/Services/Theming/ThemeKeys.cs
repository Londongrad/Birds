namespace Birds.UI.Services.Theming
{
    public static class ThemeKeys
    {
        public const string Graphite = "Graphite";
        public const string Steel = "Steel";

        public static readonly IReadOnlyList<string> SupportedThemes =
        [
            Graphite,
            Steel
        ];

        public static string Normalize(string? theme)
        {
            if (string.Equals(theme, Steel, StringComparison.OrdinalIgnoreCase)
                || string.Equals(theme, "Сталь", StringComparison.OrdinalIgnoreCase))
            {
                return Steel;
            }

            if (string.Equals(theme, Graphite, StringComparison.OrdinalIgnoreCase)
                || string.Equals(theme, "Графит", StringComparison.OrdinalIgnoreCase))
            {
                return Graphite;
            }

            return Graphite;
        }
    }
}

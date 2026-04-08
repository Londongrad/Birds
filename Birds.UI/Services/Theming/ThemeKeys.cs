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
                || string.Equals(theme, "Сталь", StringComparison.OrdinalIgnoreCase)
                || string.Equals(theme, "РЎС‚Р°Р»СЊ", StringComparison.OrdinalIgnoreCase))
            {
                return Steel;
            }

            if (string.Equals(theme, Graphite, StringComparison.OrdinalIgnoreCase)
                || string.Equals(theme, "Графит", StringComparison.OrdinalIgnoreCase)
                || string.Equals(theme, "Р“СЂР°С„РёС‚", StringComparison.OrdinalIgnoreCase))
            {
                return Graphite;
            }

            return Graphite;
        }
    }
}

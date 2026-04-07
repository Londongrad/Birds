namespace Birds.UI.Services.Localization
{
    public sealed record ThemeOption(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}

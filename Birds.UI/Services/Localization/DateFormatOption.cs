namespace Birds.UI.Services.Localization
{
    public sealed record DateFormatOption(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}

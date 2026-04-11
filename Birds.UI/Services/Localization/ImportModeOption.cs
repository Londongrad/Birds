namespace Birds.UI.Services.Localization;

public sealed record ImportModeOption(string Code, string DisplayName)
{
    public override string ToString()
    {
        return DisplayName;
    }
}
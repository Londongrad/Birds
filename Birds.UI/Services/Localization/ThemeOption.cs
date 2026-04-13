using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Localization;

public sealed partial class ThemeOption : ObservableObject
{
    public ThemeOption(string code, string displayName)
    {
        Code = code;
        this.displayName = displayName;
    }

    public string Code { get; }

    [ObservableProperty] private string displayName;

    public override string ToString()
    {
        return DisplayName;
    }
}

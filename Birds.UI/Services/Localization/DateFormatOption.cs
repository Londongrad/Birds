using CommunityToolkit.Mvvm.ComponentModel;

namespace Birds.UI.Services.Localization;

public sealed partial class DateFormatOption : ObservableObject
{
    public DateFormatOption(string code, string displayName)
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

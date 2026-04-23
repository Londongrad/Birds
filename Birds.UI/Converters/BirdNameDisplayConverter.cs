using System.Globalization;
using System.Windows.Data;
using Birds.Domain.Enums;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization;

namespace Birds.UI.Converters;

public sealed class BirdNameDisplayConverter : IValueConverter
{
    private readonly IBirdNameDisplayService _birdNameDisplay = new BirdNameDisplayService(LocalizationService.Instance);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is BirdsName birdName
            // The edit ComboBox should follow the app language, not WPF's binding culture.
            ? _birdNameDisplay.GetDisplayName(birdName)
            : Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

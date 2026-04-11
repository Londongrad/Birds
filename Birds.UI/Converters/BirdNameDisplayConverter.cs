using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.UI.Services.Localization;
using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    public sealed class BirdNameDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is BirdsName birdName
                // The edit ComboBox should follow the app language, not WPF's binding culture.
                ? birdName.ToDisplayName(LocalizationService.Instance.CurrentCulture)
                : Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}

using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    public sealed class BirdNameDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is BirdsName birdName
                ? birdName.ToDisplayName(culture)
                : Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            // Consider only boolean values
            foreach (var v in values)
            {
                if (v is bool b && b)
                    return Visibility.Collapsed;
            }

            // If no boolean value is true, return Visible
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

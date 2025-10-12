using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    public class DateOnlyToDateTimeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
                return dateOnly.ToDateTime(TimeOnly.MinValue);

            // If the source value is null, return null to clear the DatePicker
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            // If user clears the DatePicker, the value will be null
            if (value is null)
                return null;

            return Binding.DoNothing;
        }
    }
}
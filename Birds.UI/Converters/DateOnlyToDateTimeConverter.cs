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

            // если значение null (например Departure)
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            // если пользователь очистил дату в DatePicker
            if (value is null)
                return null;

            return Binding.DoNothing;
        }
    }
}
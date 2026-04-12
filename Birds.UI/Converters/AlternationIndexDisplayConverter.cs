using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;

namespace Birds.UI.Converters;

public sealed class AlternationIndexDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Binding.DoNothing;

        var item = values[0];
        if (item is null)
            return Binding.DoNothing;

        if (values[1] is not ItemsControl itemsControl)
            return Binding.DoNothing;

        var index = itemsControl.Items.IndexOf(item);
        if (index < 0)
            return Binding.DoNothing;

        return $"#{(index + 1).ToString(culture)}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

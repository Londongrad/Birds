using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Преобразует булево значение <see cref="bool"/> в цвет (кисть) для визуального отображения состояния.
    /// </summary>
    /// <remarks>
    /// Используется для элементов, отражающих состояние птицы:
    /// - <c>true</c> (жива) → зелёная кисть <see cref="Brushes.ForestGreen"/>.
    /// - <c>false</c> (мертва) → красная кисть <see cref="Brushes.IndianRed"/>.
    /// 
    /// Применяется в <c>Ellipse.Fill</c> или <c>ToggleButton.Background</c>.
    /// </remarks>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAlive = value is bool b && b;
            return isAlive ? Brushes.ForestGreen : Brushes.IndianRed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обычно обратное преобразование не нужно
            return Binding.DoNothing;
        }
    }
}

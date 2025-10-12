using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Converts a boolean value (<see cref="bool"/>) into a color (brush) for visual state representation.
    /// </summary>
    /// <remarks>
    /// Used for elements that reflect the bird's state:
    /// - <c>true</c> (alive) → green brush <see cref="Brushes.ForestGreen"/>.
    /// - <c>false</c> (dead) → red brush <see cref="Brushes.IndianRed"/>.
    /// 
    /// Applied in <c>Ellipse.Fill</c> or <c>ToggleButton.Background</c>.
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
            // Reverse conversion is usually not needed
            return Binding.DoNothing;
        }
    }
}

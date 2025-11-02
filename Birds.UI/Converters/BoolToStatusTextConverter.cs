using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Converts a boolean value into the status text "Alive" or "Dead".
    /// </summary>
    /// <remarks>
    /// Used for displaying text in <c>TextBlock</c> or <c>ToggleButton.Content</c>.
    ///
    /// - <c>true</c> → "Alive"
    /// - <c>false</c> → "Dead"
    /// </remarks>
    public class BoolToStatusTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="bool"/> value into a textual representation of the state.
        /// </summary>
        /// <param name="value">Boolean value indicating whether the bird is alive.</param>
        /// <param name="targetType">The type of the target binding property.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Culture information for the conversion.</param>
        /// <returns>The string "Alive" or "Dead".</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAlive = value is bool b && b;
            return isAlive ? "Alive" : "Dead";
        }

        /// <summary>
        /// Converts the text "Alive"/"Dead" back to a <see cref="bool"/>.
        /// </summary>
        /// <param name="value">Textual representation of the state.</param>
        /// <param name="targetType">The type of the target binding property.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Culture information for the conversion.</param>
        /// <returns><c>true</c> if "Alive"; <c>false</c> if "Dead".</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value as string;
            if (string.Equals(text, "Alive", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(text, "Dead", StringComparison.OrdinalIgnoreCase))
                return false;
            return Binding.DoNothing;
        }
    }
}

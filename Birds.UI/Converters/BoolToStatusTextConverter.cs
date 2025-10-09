using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Преобразует булево значение в строку состояния "Жива" или "Мертва".
    /// </summary>
    /// <remarks>
    /// Используется для отображения текста в <c>TextBlock</c> или <c>ToggleButton.Content</c>.
    /// 
    /// - <c>true</c> → "Жива"
    /// - <c>false</c> → "Мертва"
    /// </remarks>
    public class BoolToStatusTextConverter : IValueConverter
    {
        /// <summary>
        /// Преобразует <see cref="bool"/> в текстовое представление состояния.
        /// </summary>
        /// <param name="value">Булево значение, указывающее, жива ли птица.</param>
        /// <param name="targetType">Тип целевого свойства привязки.</param>
        /// <param name="parameter">Не используется.</param>
        /// <param name="culture">Культура преобразования.</param>
        /// <returns>Строка "Жива" или "Мертва".</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAlive = value is bool b && b;
            return isAlive ? "Жива" : "Мертва";
        }

        /// <summary>
        /// Преобразует текст "Жива"/"Мертва" обратно в <see cref="bool"/>.
        /// </summary>
        /// <param name="value">Текстовое значение состояния.</param>
        /// <param name="targetType">Тип целевого свойства привязки.</param>
        /// <param name="parameter">Не используется.</param>
        /// <param name="culture">Культура преобразования.</param>
        /// <returns><c>true</c>, если "Жива"; <c>false</c>, если "Мертва".</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value as string;
            if (string.Equals(text, "Жива", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(text, "Мертва", StringComparison.OrdinalIgnoreCase))
                return false;
            return Binding.DoNothing;
        }
    }
}

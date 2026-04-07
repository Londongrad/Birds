using Birds.Shared.Localization;
using System.Globalization;
using System.Windows.Data;

namespace Birds.UI.Converters
{
    /// <summary>
    /// Converts a boolean value into the localized status text for alive/dead birds.
    /// </summary>
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAlive = value is bool b && b;
            return AppText.Get(isAlive ? "Bird.StatusAlive" : "Bird.StatusDead");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value as string;
            if (string.Equals(text, AppText.Get("Bird.StatusAlive", culture), StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (string.Equals(text, AppText.Get("Bird.StatusDead", culture), StringComparison.CurrentCultureIgnoreCase))
                return false;

            if (string.Equals(text, AppText.Get("Bird.StatusAlive", CultureInfo.GetCultureInfo(AppLanguages.English)), StringComparison.CurrentCultureIgnoreCase))
                return true;

            if (string.Equals(text, AppText.Get("Bird.StatusDead", CultureInfo.GetCultureInfo(AppLanguages.English)), StringComparison.CurrentCultureIgnoreCase))
                return false;

            return Binding.DoNothing;
        }
    }
}

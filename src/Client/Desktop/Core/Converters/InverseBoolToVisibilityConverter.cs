using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 反向布尔到可见性转换器（true时隐藏，false时显示）
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }

            return false;
        }
    }
}

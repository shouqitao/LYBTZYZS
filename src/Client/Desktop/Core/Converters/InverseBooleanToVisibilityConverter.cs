using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 反转布尔值到可见性转换器
    /// 将 true 转换为 Collapsed，false 转换为 Visible
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            // 默认情况下返回 Visible
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Visibility visibility) {
                return visibility == Visibility.Collapsed;
            }

            return false;
        }
    }
}

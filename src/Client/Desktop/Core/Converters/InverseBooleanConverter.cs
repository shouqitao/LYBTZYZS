using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 反向布尔转换器 - true转false，false转true
    /// </summary>
    public class InverseBooleanConverter : IValueConverter {
        public static readonly InverseBooleanConverter Instance = new InverseBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return !boolValue;
            }

            return true; // 默认值
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return !boolValue;
            }

            return false; // 默认值
        }
    }
}

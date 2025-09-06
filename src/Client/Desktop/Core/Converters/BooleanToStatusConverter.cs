using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 布尔值转换为状态文本转换器
    /// true -> "启用", false -> "禁用"
    /// </summary>
    public class BooleanToStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return boolValue ? "启用" : "禁用";
            }

            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string stringValue) {
                return stringValue == "启用";
            }

            return false;
        }
    }
}

using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 布尔值转换为共享状态文本转换器
    /// true -> "共享", false -> "私有"
    /// </summary>
    public class BooleanToSharedStatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue) {
                return boolValue ? "共享" : "私有";
            }

            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string stringValue) {
                return stringValue == "共享";
            }

            return false;
        }
    }
}

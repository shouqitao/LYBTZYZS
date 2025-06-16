using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Module.Common.Converters {
    /// <summary>
    /// 布尔值转“启用/禁用”状态中文显示的转换器
    /// </summary>
    public class BoolToActiveStatusConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value is bool b && b) ? "启用" : "禁用";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value?.ToString() == "启用";
        }
    }
}

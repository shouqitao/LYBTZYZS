using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 零值到可见性转换器
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isZero = false;

            // 检查不同类型的零值
            if (value == null) {
                isZero = true;
            } else if (value is int intValue) {
                isZero = intValue == 0;
            } else if (value is double doubleValue) {
                isZero = Math.Abs(doubleValue) < double.Epsilon;
            } else if (value is float floatValue) {
                isZero = Math.Abs(floatValue) < float.Epsilon;
            } else if (value is decimal decimalValue) {
                isZero = decimalValue == 0;
            } else if (value is long longValue) {
                isZero = longValue == 0;
            } else {
                // 尝试转换为数字
                if (double.TryParse(value.ToString(), out double parsedValue)) {
                    isZero = Math.Abs(parsedValue) < double.Epsilon;
                }
            }

            // 如果参数是 "Inverse"，则反转逻辑
            bool inverse = parameter?.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (inverse) {
                return isZero ? Visibility.Collapsed : Visibility.Visible;
            } else {
                return isZero ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
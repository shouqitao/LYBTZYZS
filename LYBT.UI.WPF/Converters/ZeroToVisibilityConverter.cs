using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// Convert zero to Visible, non-zero to Collapsed.
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null)
                return Visibility.Collapsed;

            bool isZero = false;
            if (value is int intValue)
                isZero = intValue == 0;
            else if (value is double doubleValue)
                isZero = Math.Abs(doubleValue) < 0.001;
            else if (value is float floatValue)
                isZero = Math.Abs(floatValue) < 0.001f;
            else if (value is decimal decimalValue)
                isZero = decimalValue == 0;

            // 如果参数是 "Inverse"，则反转逻辑
            bool inverse = parameter?.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (inverse)
                return isZero ? Visibility.Collapsed : Visibility.Visible;
            else
                return isZero ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}

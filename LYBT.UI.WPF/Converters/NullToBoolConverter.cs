using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// Null转布尔值转换器
    /// </summary>
    [ValueConversion(typeof(object), typeof(bool))]
    public class NullToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("NullToBoolConverter does not support ConvertBack");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 布尔值转可见性转换器
    /// </summary>
    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool boolValue && boolValue)
                return System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is System.Windows.Visibility visibility)
                return visibility == System.Windows.Visibility.Visible;
            return false;
        }
    }
}

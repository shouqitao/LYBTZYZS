using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 字符串为空到可见性转换器
    /// </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isEmpty = string.IsNullOrWhiteSpace(value?.ToString());

            // 如果参数是 "Inverse"，则反转逻辑
            bool inverse = parameter?.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (inverse)
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            else
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}

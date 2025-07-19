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
    /// 空值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isNull = value == null;

            // 如果参数是 "Inverse"，则反转逻辑
            bool inverse = parameter?.ToString().Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;

            if (inverse)
                return isNull ? Visibility.Collapsed : Visibility.Visible;
            else
                return isNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}

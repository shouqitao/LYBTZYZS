using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 布尔转可见性（WPF常用）
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Visibility v && v == Visibility.Visible);
    }
}

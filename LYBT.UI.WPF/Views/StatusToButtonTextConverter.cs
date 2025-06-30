using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.UI.WPF.Views {
    public class StatusToButtonTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool active) {
                return active ? "禁用" : "启用";
            }
            return "切换";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}

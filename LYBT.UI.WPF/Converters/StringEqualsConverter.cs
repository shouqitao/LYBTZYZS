using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// Compare a bound string with converter parameter.
    /// </summary>
    public class StringEqualsConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? parameter : Binding.DoNothing;
    }
}

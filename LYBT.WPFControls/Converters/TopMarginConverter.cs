using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.WPFControls.Converters {
    /// <summary>
    /// Converts a double value to a top margin thickness. Used to offset the
    /// suggestion list below the herb textbox.
    /// </summary>
    public class TopMarginConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is double d) {
                return new Thickness(0, d, 0, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}

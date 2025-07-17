using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.UI.WPF.Converters {
    public class SelectedNavButtonBackgroundConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isSelected = value is bool b && b;
            // Use theme primary color (Deep Purple 500) when selected
            return isSelected ? new SolidColorBrush(Color.FromRgb(103, 58, 183)) : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}

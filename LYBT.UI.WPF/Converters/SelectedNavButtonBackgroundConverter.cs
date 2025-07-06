using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.UI.WPF.Converters {
    public class SelectedNavButtonBackgroundConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isSelected = value is bool b && b;
            return isSelected ? new SolidColorBrush(Color.FromRgb(33, 150, 243)) : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}

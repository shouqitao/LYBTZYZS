using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.UI.WPF.Converters
{
    public class SelectedNavButtonForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = value is bool b && b;
            return isSelected ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(33, 33, 33));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

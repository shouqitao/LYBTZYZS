using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Controls.Converters
{
    public class BoolToMarginConverter : IValueConverter
    {
        public Thickness TrueValue { get; set; } = new Thickness(12, 8, 0, 8);

        public Thickness FalseValue { get; set; } = new Thickness(0, 8, 0, 8);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thickness)
            {
                return Math.Abs(thickness.Left - TrueValue.Left) < 0.001;
            }
            return false;
        }
    }
}

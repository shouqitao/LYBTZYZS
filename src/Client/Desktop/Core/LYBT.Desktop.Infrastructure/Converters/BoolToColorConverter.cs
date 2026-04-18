using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 布尔到颜色转换器 - 支持 ConverterParameter 自定义颜色
    /// 用法: ConverterParameter="Green|Amber" (true颜色|false颜色)
    /// 无参数时默认: true=绿色, false=灰色
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new BoolToColorConverter();

        private static readonly SolidColorBrush DefaultTrue = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // Green
        private static readonly SolidColorBrush DefaultFalse = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // Amber

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
                return DefaultFalse;

            if (parameter is string param && param.Contains('|'))
            {
                var parts = param.Split('|', 2);
                var colorName = boolValue ? parts[0].Trim() : parts[1].Trim();
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
            }

            return boolValue ? DefaultTrue : DefaultFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

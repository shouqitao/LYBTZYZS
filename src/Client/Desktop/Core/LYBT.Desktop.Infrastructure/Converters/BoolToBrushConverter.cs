using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 布尔到画刷转换器 - true转绿色，false转红色
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public static readonly BoolToBrushConverter Instance = new BoolToBrushConverter();

        private static readonly SolidColorBrush TrueBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)); // 绿色
        private static readonly SolidColorBrush FalseBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45)); // 红色

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }

            return FalseBrush; // 默认红色
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToBrushConverter does not support ConvertBack");
        }
    }
}

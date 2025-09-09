using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 布尔值转换为在线状态画刷转换器
    /// true -> 绿色 (在线), false -> 灰色 (离线)
    /// </summary>
    public class BooleanToOnlineBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush(Color.FromRgb(40, 167, 69)) // 在线绿色 #28a745
                    : new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 离线灰色 #6c757d
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToOnlineBrushConverter does not support ConvertBack");
        }
    }
}

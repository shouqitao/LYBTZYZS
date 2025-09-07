using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 共享状态转换为画刷颜色转换器
    /// true -> 蓝色 (共享), false -> 橙色 (私有)
    /// </summary>
    public class SharedStatusToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush(Color.FromRgb(0, 123, 255)) // 共享蓝色 #007bff
                    : new SolidColorBrush(Color.FromRgb(253, 126, 20)); // 私有橙色 #fd7e14
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SharedStatusToBrushConverter does not support ConvertBack");
        }
    }
}

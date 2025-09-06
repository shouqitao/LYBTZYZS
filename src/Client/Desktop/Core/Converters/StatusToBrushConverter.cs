using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters {

    /// <summary>
    /// 状态转换为画刷颜色转换器
    /// 支持布尔值和状态枚举
    /// </summary>
    public class StatusToBrushConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            // 处理布尔值
            if (value is bool boolValue) {
                return boolValue
                    ? new SolidColorBrush(Color.FromRgb(40, 167, 69))   // 启用绿色 #28a745
                    : new SolidColorBrush(Color.FromRgb(220, 53, 69));  // 禁用红色 #dc3545
            }

            // 处理整数状态
            if (value is int intValue) {
                return intValue switch {
                    1 => new SolidColorBrush(Color.FromRgb(40, 167, 69)),   // 启用绿色
                    0 => new SolidColorBrush(Color.FromRgb(220, 53, 69)),   // 禁用红色
                    2 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),   // 警告黄色 #ffc107
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))  // 默认灰色
                };
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 默认灰色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("StatusToBrushConverter does not support ConvertBack");
        }
    }
}

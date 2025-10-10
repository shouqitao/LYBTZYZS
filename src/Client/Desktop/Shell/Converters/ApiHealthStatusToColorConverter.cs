using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYBT.Desktop.Foundation.HealthCheck;

namespace LYBT.Desktop.Shell.Converters
{
    /// <summary>
    /// API 健康状态到颜色转换器
    /// 用于将 ApiHealthStatus 枚举值转换为对应的颜色
    /// </summary>
    public class ApiHealthStatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// 将 ApiHealthStatus 转换为 Brush 颜色
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ApiHealthStatus status)
            {
                return Brushes.Gray;
            }

            return status switch
            {
                ApiHealthStatus.Healthy => new SolidColorBrush(Color.FromRgb(34, 197, 94)),      // 绿色 #22C55E
                ApiHealthStatus.Unhealthy => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // 红色 #EF4444
                ApiHealthStatus.Checking => new SolidColorBrush(Color.FromRgb(251, 191, 36)),    // 黄色 #FBBF24
                _ => Brushes.Gray
            };
        }

        /// <summary>
        /// 反向转换（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ApiHealthStatusToColorConverter 不支持反向转换");
        }
    }
}

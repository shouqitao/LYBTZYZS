using System.Globalization;
using System.Windows.Data;
using LYBT.Desktop.Services.Interfaces;

namespace LYBT.Desktop.Shell.Converters
{
    /// <summary>
    /// API 健康状态到文本转换器
    /// 用于将 ApiHealthStatus 枚举值转换为用户友好的文本
    /// </summary>
    public class ApiHealthStatusToTextConverter : IValueConverter
    {
        /// <summary>
        /// 将 ApiHealthStatus 转换为文本
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ApiHealthStatus status)
            {
                return "未知";
            }

            return status switch
            {
                ApiHealthStatus.Healthy => "已连接",
                ApiHealthStatus.Unhealthy => "连接失败",
                ApiHealthStatus.Checking => "连接中...",
                _ => "未知"
            };
        }

        /// <summary>
        /// 反向转换（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ApiHealthStatusToTextConverter 不支持反向转换");
        }
    }
}

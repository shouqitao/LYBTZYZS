using System.Globalization;
using System.Windows.Data;
using LYBT.Desktop.Foundation.HealthCheck;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// ApiHealthStatus 枚举转文本
/// </summary>
public class ApiHealthStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ApiHealthStatus status)
        {
            return status switch
            {
                ApiHealthStatus.Healthy => "在线",
                ApiHealthStatus.Checking => "检测中",
                ApiHealthStatus.Unhealthy => "离线",
                _ => status.ToString()
            };
        }

        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

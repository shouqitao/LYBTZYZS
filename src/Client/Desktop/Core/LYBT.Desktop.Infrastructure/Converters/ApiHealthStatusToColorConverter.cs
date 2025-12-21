using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYBT.Desktop.Foundation.HealthCheck;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 将 ApiHealthStatus 转换为对应的颜色
/// OpenSpec: consolidate-wpf-converters - 统一使用Fluent Design标准色
/// </summary>
public class ApiHealthStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ApiHealthStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            ApiHealthStatus.Healthy => new SolidColorBrush(Color.FromRgb(34, 197, 94)),    // #22C55E 绿色
            ApiHealthStatus.Checking => new SolidColorBrush(Color.FromRgb(251, 191, 36)),  // #FBBF24 黄色
            ApiHealthStatus.Unhealthy => new SolidColorBrush(Color.FromRgb(239, 68, 68)),  // #EF4444 红色
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

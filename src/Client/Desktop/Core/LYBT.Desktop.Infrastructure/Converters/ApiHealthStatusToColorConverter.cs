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

    /// <summary>
    /// 反向转换颜色到 API 健康状态
    /// 由于状态到颜色的映射是多对一的，此转换不可逆
    /// </summary>
    /// <returns>返回 Binding.DoNothing 表示不支持反向转换</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 颜色到状态的映射是不可逆的（无法区分不同状态可能使用的相同颜色）
        // 因此返回 Binding.DoNothing 告诉 WPF 绑定系统不要更新源
        return Binding.DoNothing;
    }
}

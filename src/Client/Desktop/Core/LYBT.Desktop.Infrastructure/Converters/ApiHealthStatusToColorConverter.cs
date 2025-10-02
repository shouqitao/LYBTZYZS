using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYBT.Desktop.Services.Interfaces;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 将 ApiHealthStatus 转换为对应的颜色
/// </summary>
public class ApiHealthStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ApiHealthStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            ApiHealthStatus.Healthy => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
            ApiHealthStatus.Checking => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Yellow/Orange
            ApiHealthStatus.Unhealthy => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

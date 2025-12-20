using System.Globalization;
using System.Windows;
using System.Windows.Data;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 煎法到可见性转换器
/// 当煎法不是Default时显示，否则隐藏
/// </summary>
public class DecocteMethodToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DecocteMethod method)
        {
            return method != DecocteMethod.Default ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

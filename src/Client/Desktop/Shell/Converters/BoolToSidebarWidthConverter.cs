using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Shell.Converters;

/// <summary>
/// poc-drawer-layout: 将布尔值转换为侧边栏宽度
/// true (展开) -> 200, false (收缩) -> 56
/// </summary>
public class BoolToSidebarWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isExpanded && isExpanded)
            return 200.0;
        return 56.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

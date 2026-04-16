using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 零值到可见性转换器
/// 用于集合计数等场景：0 = Visible（显示空状态），非0 = Collapsed（隐藏空状态）
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is long longCount)
        {
            return longCount == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <summary>
    /// 反向转换可见性到计数数值
    /// Visible -> 0（空状态）, Collapsed -> 1（非零，表示有内容）
    /// 注意：无法恢复原始计数的确切值，只能区分零和非零
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            // 根据目标类型返回相应的零值或非零值
            if (targetType == typeof(int))
            {
                return visibility == Visibility.Visible ? 0 : 1;
            }

            if (targetType == typeof(long))
            {
                return visibility == Visibility.Visible ? 0L : 1L;
            }
        }

        return DependencyProperty.UnsetValue;
    }
}

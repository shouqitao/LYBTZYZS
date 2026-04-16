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

    /// <summary>
    /// 反向转换可见性到煎法枚举
    /// Visible -> 非 Default 值（无法确定具体值）, Collapsed -> Default
    /// 注意：由于无法确定具体的非默认值，此转换是单向的
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility && targetType == typeof(DecocteMethod))
        {
            // Collapsed 对应 Default
            if (visibility == Visibility.Collapsed)
                return DecocteMethod.Default;

            // Visible 对应非默认值，但无法确定是哪个
            // 返回 UnsetValue 表示无法准确转换
        }

        return DependencyProperty.UnsetValue;
    }
}

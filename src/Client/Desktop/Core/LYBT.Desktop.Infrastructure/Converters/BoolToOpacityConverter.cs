using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 将布尔值转换为透明度
/// true -> 1.0, false -> 0.0
/// OpenSpec: standardize-converter-organization
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOpen && isOpen)
            return 1.0;
        return 0.0;
    }

    /// <summary>
    /// 反向转换透明度到布尔值
    /// 1.0 -> true, 0.0 -> false, 其他值 -> Binding.DoNothing
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double opacity)
        {
            // 只有明确的 1.0 和 0.0 才进行转换
            if (opacity == 1.0)
                return true;
            if (opacity == 0.0)
                return false;
        }

        // 其他情况不更新源值
        return Binding.DoNothing;
    }
}

using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Shell.Converters;

/// <summary>
/// poc-drawer-layout: 将布尔值转换为TranslateX偏移量
/// true -> 0 (显示), false -> -260 (隐藏)
/// </summary>
public class BoolToTranslateXConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOpen && isOpen)
            return 0.0;
        return -260.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// poc-drawer-layout: 将布尔值转换为透明度
/// true -> 1.0, false -> 0.0
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOpen && isOpen)
            return 1.0;
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

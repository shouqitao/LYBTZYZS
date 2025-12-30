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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

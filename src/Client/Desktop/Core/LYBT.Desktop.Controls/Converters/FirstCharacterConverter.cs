using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Controls.Converters;

/// <summary>
/// 提取字符串首字符
/// </summary>
public class FirstCharacterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
        {
            return s[..1];
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

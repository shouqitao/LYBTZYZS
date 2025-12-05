using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Shell.Converters;

/// <summary>提取字符串首字符的转换器（用于用户头像显示）</summary>
public class FirstCharConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
            return str[0].ToString();
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

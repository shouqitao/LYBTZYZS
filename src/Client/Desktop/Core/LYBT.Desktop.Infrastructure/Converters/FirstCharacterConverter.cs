using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 提取字符串首字符转换器
    /// 用于显示用户头像中的首字母
    /// </summary>
    public class FirstCharacterConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrWhiteSpace(text))
            {
                // 返回首字符的大写形式
                return text[0].ToString().ToUpper(culture);
            }

            // 如果输入为空，返回默认占位符
            return "?";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 不支持反向转换
            return DependencyProperty.UnsetValue;
        }
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{

    /// <summary>
    /// 字符串到可见性转换器
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 反向转换可见性到字符串
        /// Visible -> string.Empty（非空字符串，但无法确定原字符串内容）, Collapsed -> null
        /// 注意：此转换是单向的，无法恢复原始字符串内容
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Visible 对应非空字符串（但无法确定原内容）
                if (visibility == Visibility.Visible)
                    return string.Empty;

                // Collapsed 对应 null
                if (visibility == Visibility.Collapsed)
                    return null!;
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

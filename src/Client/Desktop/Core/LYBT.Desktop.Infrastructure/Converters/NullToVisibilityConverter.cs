using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 空值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 反向转换可见性到对象引用
        /// Visible -> null, Collapsed -> DependencyProperty.UnsetValue
        /// 注意：此转换是单向的，无法恢复原始对象
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Visible 对应 null（无对象）
                if (visibility == Visibility.Visible)
                    return null!;

                // Collapsed 对应有对象（但无法确定是哪个对象）
                // 返回 UnsetValue 表示无法转换
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

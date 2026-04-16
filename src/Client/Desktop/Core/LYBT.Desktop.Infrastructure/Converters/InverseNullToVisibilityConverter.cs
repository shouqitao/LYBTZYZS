using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 反向空值到可见性转换器
    /// 当值为null时返回Visible,不为null时返回Collapsed
    /// 用于空状态提示显示
    /// </summary>
    public class InverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换可见性到对象引用
        /// Visible -> null（空状态）, Collapsed -> 非 null（有对象，但无法确定具体对象）
        /// 注意：此转换是单向的，无法恢复原始对象
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Visible 对应 null（空状态）
                if (visibility == Visibility.Visible)
                    return null;

                // Collapsed 对应有对象（但无法确定是哪个对象）
                // 返回 UnsetValue 表示无法转换
            }

            return DependencyProperty.UnsetValue;
        }
    }
}

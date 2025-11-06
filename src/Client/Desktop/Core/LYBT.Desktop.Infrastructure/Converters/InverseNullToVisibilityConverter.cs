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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

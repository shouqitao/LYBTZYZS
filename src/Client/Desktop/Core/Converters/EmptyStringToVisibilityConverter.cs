using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 空字符串转可见性转换器
    /// 当字符串为空或null时返回Visible，否则返回Collapsed
    /// 用于显示占位提示文本
    /// </summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            
            // 如果参数为"Inverse"，则反转逻辑
            bool inverse = parameter as string == "Inverse";
            
            bool isEmpty = string.IsNullOrWhiteSpace(str);
            
            if (inverse)
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
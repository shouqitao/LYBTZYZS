using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.WPF.Client.Core.Converters
{
    /// <summary>
    /// 布尔值转背景色转换器
    /// </summary>
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // 如果提供了参数，使用参数作为颜色
                if (parameter is string colorString)
                {
                    try
                    {
                        return new BrushConverter().ConvertFromString(colorString) as Brush;
                    }
                    catch
                    {
                        // 如果转换失败，使用默认颜色
                    }
                }
                
                // 默认选中背景色
                return new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 布尔值转换为画刷颜色转换器
    /// true -> Green (成功绿色), false -> Red (错误红色)
    /// </summary>
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue
                    ? new SolidColorBrush(Color.FromRgb(40, 167, 69))   // 成功绿色 #28a745
                    : new SolidColorBrush(Color.FromRgb(220, 53, 69));  // 错误红色 #dc3545
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 灰色 #6c757d
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToBrushConverter does not support ConvertBack");
        }
    }
}

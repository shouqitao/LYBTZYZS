using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Formula.Converters
{
    /// <summary>
    /// 布尔值反转转换器
    /// Issue #2075: 用于按钮IsEnabled绑定（IsReadOnly=true时按钮禁用）
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.MedicalCase.Converters
{
    /// <summary>
    /// Bool值取反转换器（用于RadioButton绑定）
    /// Task 3.3 (#1660): 支持"是/否"RadioButton双向绑定
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
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

using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 减法转换器，用于计算两个数值的差值
    /// </summary>
    public class SubtractConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0;

            if (values[0] is decimal total && values[1] is decimal paid)
            {
                return total - paid;
            }

            // 尝试转换为 decimal
            if (decimal.TryParse(values[0]?.ToString(), out decimal totalParsed) &&
                decimal.TryParse(values[1]?.ToString(), out decimal paidParsed))
            {
                return totalParsed - paidParsed;
            }

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
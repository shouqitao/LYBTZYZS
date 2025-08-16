using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 多布尔值AND转换器 - 所有值都为true时返回true
    /// </summary>
    public class MultiBooleanAndConverter : IMultiValueConverter
    {
        public static readonly MultiBooleanAndConverter Instance = new MultiBooleanAndConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            return values.All(value => value is bool boolValue && boolValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
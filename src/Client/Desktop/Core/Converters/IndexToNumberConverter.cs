using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 索引转换为数字转换器（从0开始转换为从1开始）
    /// </summary>
    public class IndexToNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index + 1).ToString();
            }
            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
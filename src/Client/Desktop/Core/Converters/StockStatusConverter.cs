using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 库存状态转换器
    /// 根据库存数量返回状态：Normal（正常）、Low（库存不足）、OutOfStock（缺货）
    /// </summary>
    public class StockStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                if (stock <= 0)
                    return "OutOfStock";
                else if (stock < 10)
                    return "Low";
                else
                    return "Normal";
            }
            return "Normal";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
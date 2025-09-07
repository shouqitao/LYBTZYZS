using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 价格格式化转换器
    /// 将数值格式化为货币显示格式，如 12.50 -> "￥12.50"
    /// </summary>
    public class PriceFormatConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "￥0.00";
            }

            // 尝试转换为 decimal
            decimal price = 0;

            if (value is decimal decimalValue)
            {
                price = decimalValue;
            }
            else if (value is double doubleValue)
            {
                price = (decimal)doubleValue;
            }
            else if (value is float floatValue)
            {
                price = (decimal)floatValue;
            }
            else if (value is int intValue)
            {
                price = intValue;
            }
            else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
            {
                price = parsedValue;
            }

            // 格式化为货币格式
            return $"￥{price:F2}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // 移除货币符号和空格
                var cleanValue = stringValue.Replace("￥", string.Empty).Replace("¥", string.Empty).Trim();

                if (decimal.TryParse(cleanValue, out decimal result))
                {
                    return result;
                }
            }

            return 0m;
        }
    }
}

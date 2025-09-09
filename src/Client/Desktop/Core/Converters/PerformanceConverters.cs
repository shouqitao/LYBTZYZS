using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 库存状态到颜色转换器
    /// 根据当前库存、最小库存、最大库存计算状态颜色
    /// </summary>
    public class StockStatusToColorConverter : IMultiValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 3 &&
                values[0] is decimal currentStock &&
                values[1] is decimal minStock &&
                values[2] is decimal maxStock)
            {
                // 库存充足 (绿色)
                if (currentStock >= minStock * 1.5m)
                {
                    return Color.FromRgb(76, 175, 80); // #4CAF50 - 绿色
                }

                // 库存不足 (橙色)
                else if (currentStock >= minStock)
                {
                    return Color.FromRgb(255, 152, 0); // #FF9800 - 橙色
                }

                // 库存严重不足 (红色)
                else
                {
                    return Color.FromRgb(244, 67, 54); // #F44336 - 红色
                }
            }

            // 默认灰色 (未知状态)
            return Color.FromRgb(158, 158, 158); // #9E9E9E - 灰色
        }

        /// <inheritdoc/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StockStatusToColorConverter 不支持反向转换");
        }
    }

    /// <summary>
    /// 价格到颜色转换器
    /// 根据价格区间显示不同颜色
    /// </summary>
    public class PriceToColorConverter : IValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal price)
            {
                // 高价格 (深蓝色)
                if (price >= 100m)
                {
                    return Color.FromRgb(63, 81, 181); // #3F51B5 - 深蓝色
                }

                // 中等价格 (蓝色)
                else if (price >= 20m)
                {
                    return Color.FromRgb(33, 150, 243); // #2196F3 - 蓝色
                }

                // 低价格 (绿色)
                else
                {
                    return Color.FromRgb(76, 175, 80); // #4CAF50 - 绿色
                }
            }

            // 默认黑色
            return Color.FromRgb(33, 37, 41); // #212529 - 深灰色
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("PriceToColorConverter 不支持反向转换");
        }
    }

    /// <summary>
    /// 布尔值到颜色转换器
    /// 根据布尔值和参数返回不同颜色
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorPair)
            {
                var colors = colorPair.Split('|');
                if (colors.Length == 2)
                {
                    var colorString = boolValue ? colors[0] : colors[1];

                    // 移除 # 前缀
                    if (colorString.StartsWith("#"))
                    {
                        colorString = colorString.Substring(1);
                    }

                    if (colorString.Length == 6 &&
                        byte.TryParse(colorString.Substring(0, 2), NumberStyles.HexNumber, null, out byte r) &&
                        byte.TryParse(colorString.Substring(2, 2), NumberStyles.HexNumber, null, out byte g) &&
                        byte.TryParse(colorString.Substring(4, 2), NumberStyles.HexNumber, null, out byte b))
                    {
                        return Color.FromRgb(r, g, b);
                    }
                }
            }

            // 默认颜色
            return Colors.Gray;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToColorConverter 不支持反向转换");
        }
    }

    /// <summary>
    /// 布尔值到字符串转换器
    /// 根据布尔值和参数返回不同字符串
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string stringPair)
            {
                var strings = stringPair.Split('|');
                if (strings.Length == 2)
                {
                    return boolValue ? strings[0] : strings[1];
                }
            }

            return value?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string stringPair)
            {
                var strings = stringPair.Split('|');
                if (strings.Length == 2)
                {
                    return stringValue == strings[0];
                }
            }

            return false;
        }
    }
}

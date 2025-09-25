using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 集合数量到可见性转换器
    /// 合并了原有的：
    /// - EmptyCollectionToVisibilityConverter
    /// - CollectionHasItemsToVisibilityConverter
    /// - CountToVisibilityConverter
    /// 参数说明：
    /// - 数字 - 当集合数量等于该数字时显示
    /// - ">数字" - 当集合数量大于该数字时显示
    /// - "<数字" - 当集合数量小于该数字时显示
    /// - ">=数字" - 当集合数量大于等于该数字时显示
    /// - "<=数字" - 当集合数量小于等于该数字时显示
    /// - "Inverse" - 反转逻辑
    /// - "Hidden" - 使用Hidden而非Collapsed
    /// </summary>
    public class CollectionCountToVisibilityConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int count = GetCount(value);
            var paramStr = parameter as string ?? string.Empty;
            
            bool shouldShow = EvaluateCondition(count, paramStr);
            bool inverse = paramStr.Contains("Inverse", StringComparison.OrdinalIgnoreCase);
            bool useHidden = paramStr.Contains("Hidden", StringComparison.OrdinalIgnoreCase);

            // 应用反转逻辑
            if (inverse)
                shouldShow = !shouldShow;

            // 返回可见性
            if (shouldShow)
            {
                return Visibility.Visible;
            }
            else
            {
                return useHidden ? Visibility.Hidden : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return Convert(null, targetType, parameter, culture);
            }

            // 多值支持：计算所有集合的总数
            int totalCount = 0;
            foreach (var value in values)
            {
                totalCount += GetCount(value);
            }

            return Convert(totalCount, targetType, parameter, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取集合或数值的计数
        /// </summary>
        private static int GetCount(object? value)
        {
            if (value == null)
                return 0;

            if (value is ICollection collection)
                return collection.Count;

            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }
                return count;
            }

            if (value is int intValue)
                return intValue;

            if (value is long longValue)
                return (int)longValue;

            if (value is decimal decimalValue)
                return (int)decimalValue;

            if (value is double doubleValue)
                return (int)doubleValue;

            if (value is float floatValue)
                return (int)floatValue;

            if (int.TryParse(value.ToString(), out int parsedValue))
                return parsedValue;

            return 0;
        }

        /// <summary>
        /// 评估条件是否满足
        /// </summary>
        private static bool EvaluateCondition(int count, string paramStr)
        {
            if (string.IsNullOrWhiteSpace(paramStr))
            {
                // 默认：有内容时显示
                return count > 0;
            }

            // 移除Inverse和Hidden关键字，只保留条件部分
            paramStr = paramStr.Replace("Inverse", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("Hidden", "", StringComparison.OrdinalIgnoreCase)
                               .Trim();

            if (string.IsNullOrWhiteSpace(paramStr))
            {
                return count > 0;
            }

            // 解析条件
            if (paramStr.StartsWith(">="))
            {
                if (int.TryParse(paramStr[2..], out int threshold))
                    return count >= threshold;
            }
            else if (paramStr.StartsWith("<="))
            {
                if (int.TryParse(paramStr[2..], out int threshold))
                    return count <= threshold;
            }
            else if (paramStr.StartsWith(">"))
            {
                if (int.TryParse(paramStr[1..], out int threshold))
                    return count > threshold;
            }
            else if (paramStr.StartsWith("<"))
            {
                if (int.TryParse(paramStr[1..], out int threshold))
                    return count < threshold;
            }
            else if (paramStr.StartsWith("=="))
            {
                if (int.TryParse(paramStr[2..], out int threshold))
                    return count == threshold;
            }
            else if (int.TryParse(paramStr, out int threshold))
            {
                // 纯数字：等于该值时显示
                return count == threshold;
            }

            // 默认条件
            return count > 0;
        }
    }
}
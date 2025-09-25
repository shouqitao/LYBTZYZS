using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的值到百分比转换器
    /// 合并了原有的：
    /// - PercentageConverter
    /// - DecimalToPercentConverter
    /// - ProgressToPercentageConverter
    /// 参数说明：
    /// - "Format:P2" - 格式化为百分比，保留2位小数
    /// - "Max:100" - 指定最大值（默认1.0）
    /// - "Display" - 转换为显示字符串（如"75%"）
    /// - "Decimal" - 保持小数形式（0.75）
    /// - "Integer" - 转换为整数百分比（75）
    /// </summary>
    public class ValueToPercentageConverter : IValueConverter, IMultiValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            // 获取数值
            double numericValue = GetNumericValue(value);
            
            // 解析参数
            var parameters = ParseParameters(parameter as string);
            
            // 应用最大值归一化
            if (parameters.TryGetValue("max", out var maxStr) && double.TryParse(maxStr, out var max))
            {
                numericValue = numericValue / max;
            }

            // 根据显示格式返回结果
            if (parameters.ContainsKey("display"))
            {
                // 显示为百分比字符串
                var format = parameters.TryGetValue("format", out var fmt) ? fmt : "P0";
                if (!format.StartsWith("P"))
                    format = "P0";
                return numericValue.ToString(format, culture);
            }
            else if (parameters.ContainsKey("integer"))
            {
                // 返回整数百分比
                return (int)(numericValue * 100);
            }
            else if (parameters.ContainsKey("decimal"))
            {
                // 返回小数形式
                return numericValue;
            }
            else
            {
                // 默认行为：根据目标类型决定
                if (targetType == typeof(string))
                {
                    return $"{numericValue * 100:F1}%";
                }
                else if (targetType == typeof(int))
                {
                    return (int)(numericValue * 100);
                }
                else
                {
                    return numericValue * 100;
                }
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            double percentValue = 0;

            if (value is string strValue)
            {
                // 移除百分号并解析
                strValue = strValue.Replace("%", "").Trim();
                if (double.TryParse(strValue, NumberStyles.Any, culture, out var parsed))
                {
                    percentValue = parsed / 100.0;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }
            else
            {
                percentValue = GetNumericValue(value) / 100.0;
            }

            // 解析参数
            var parameters = ParseParameters(parameter as string);
            
            // 应用最大值反归一化
            if (parameters.TryGetValue("max", out var maxStr) && double.TryParse(maxStr, out var max))
            {
                percentValue = percentValue * max;
            }

            // 根据目标类型返回
            if (targetType == typeof(decimal))
                return (decimal)percentValue;
            else if (targetType == typeof(float))
                return (float)percentValue;
            else if (targetType == typeof(int))
                return (int)Math.Round(percentValue);
            else
                return percentValue;
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return DependencyProperty.UnsetValue;

            // 多值支持：第一个值是当前值，第二个值是最大值
            var current = GetNumericValue(values[0]);
            
            if (values.Length > 1 && values[1] != null)
            {
                var max = GetNumericValue(values[1]);
                if (max != 0)
                {
                    current = current / max;
                }
            }

            return Convert(current, targetType, parameter, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 从对象获取数值
        /// </summary>
        private static double GetNumericValue(object? value)
        {
            if (value == null)
                return 0;

            return value switch
            {
                double d => d,
                float f => f,
                decimal dec => (double)dec,
                int i => i,
                long l => l,
                byte b => b,
                short s => s,
                string str when double.TryParse(str, out var parsed) => parsed,
                _ => 0
            };
        }

        /// <summary>
        /// 解析参数字符串
        /// </summary>
        private static Dictionary<string, string> ParseParameters(string? parameter)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(parameter))
                return result;

            var parts = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains(':'))
                {
                    var kvp = part.Split(':', 2);
                    result[kvp[0].ToLower()] = kvp[1];
                }
                else
                {
                    result[part.ToLower()] = string.Empty;
                }
            }

            return result;
        }
    }
}
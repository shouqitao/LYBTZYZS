using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的多绑定转换器
    /// 合并了原有的：
    /// - MultiValueConverter
    /// - CombineValuesConverter
    /// - LogicalAndConverter
    /// - LogicalOrConverter
    /// 参数说明：
    /// - "And" - 逻辑与运算
    /// - "Or" - 逻辑或运算
    /// - "Format:格式字符串" - 字符串格式化
    /// - "Join:分隔符" - 连接字符串
    /// - "Sum" - 数值求和
    /// - "Average" - 数值求平均
    /// - "Min" - 最小值
    /// - "Max" - 最大值
    /// - "First" - 第一个非空值
    /// - "Last" - 最后一个非空值
    /// - "Count" - 非空值计数
    /// - "AllEqual" - 所有值相等
    /// </summary>
    public class MultiBindingConverter : IMultiValueConverter
    {
        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return DependencyProperty.UnsetValue;

            // 过滤掉UnsetValue
            var validValues = values.Where(v => v != DependencyProperty.UnsetValue).ToArray();
            if (validValues.Length == 0)
                return DependencyProperty.UnsetValue;

            var paramStr = parameter as string ?? "First";
            var parameters = ParseParameters(paramStr);
            var operation = parameters.ContainsKey("operation") ? parameters["operation"] : paramStr.Split(':')[0];

            return operation.ToLowerInvariant() switch
            {
                "and" => PerformLogicalAnd(validValues),
                "or" => PerformLogicalOr(validValues),
                "format" => PerformFormat(validValues, parameters),
                "join" => PerformJoin(validValues, parameters),
                "sum" => PerformSum(validValues),
                "average" or "avg" => PerformAverage(validValues),
                "min" => PerformMin(validValues),
                "max" => PerformMax(validValues),
                "first" => PerformFirst(validValues),
                "last" => PerformLast(validValues),
                "count" => validValues.Length,
                "allequal" => PerformAllEqual(validValues),
                "any" => PerformAny(validValues),
                "all" => PerformAll(validValues),
                "concat" => PerformConcat(validValues),
                "coalesce" => PerformCoalesce(validValues),
                "switch" => PerformSwitch(validValues),
                _ => PerformFirst(validValues)
            };
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            // 不支持反向转换
            throw new NotImplementedException();
        }

        /// <summary>
        /// 逻辑与运算
        /// </summary>
        private static object PerformLogicalAnd(object?[] values)
        {
            foreach (var value in values)
            {
                if (!ConvertToBoolean(value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 逻辑或运算
        /// </summary>
        private static object PerformLogicalOr(object?[] values)
        {
            foreach (var value in values)
            {
                if (ConvertToBoolean(value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 字符串格式化
        /// </summary>
        private static object PerformFormat(object?[] values, Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("format", out var format))
            {
                // 尝试从参数中提取格式字符串
                var formatKey = parameters.Keys.FirstOrDefault(k => k.StartsWith("format:", StringComparison.OrdinalIgnoreCase));
                if (formatKey != null)
                {
                    format = formatKey[7..]; // 移除 "format:" 前缀
                }
                else
                {
                    format = "{0}";
                }
            }

            try
            {
                return string.Format(format, values);
            }
            catch (FormatException)
            {
                return string.Join(" ", values);
            }
        }

        /// <summary>
        /// 连接字符串
        /// </summary>
        private static object PerformJoin(object?[] values, Dictionary<string, string> parameters)
        {
            var separator = parameters.TryGetValue("join", out var sep) ? sep : ", ";
            
            // 特殊处理分隔符
            separator = separator switch
            {
                "space" => " ",
                "comma" => ",",
                "semicolon" => ";",
                "newline" => "\n",
                "tab" => "\t",
                _ => separator
            };

            var stringValues = values.Select(v => v?.ToString() ?? string.Empty);
            return string.Join(separator, stringValues);
        }

        /// <summary>
        /// 数值求和
        /// </summary>
        private static object PerformSum(object?[] values)
        {
            double sum = 0;
            foreach (var value in values)
            {
                sum += ConvertToDouble(value);
            }
            return sum;
        }

        /// <summary>
        /// 数值求平均
        /// </summary>
        private static object PerformAverage(object?[] values)
        {
            if (values.Length == 0)
                return 0.0;

            double sum = 0;
            int count = 0;
            
            foreach (var value in values)
            {
                var numValue = ConvertToDouble(value);
                if (!double.IsNaN(numValue))
                {
                    sum += numValue;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0.0;
        }

        /// <summary>
        /// 最小值
        /// </summary>
        private static object PerformMin(object?[] values)
        {
            double min = double.MaxValue;
            bool hasValue = false;
            
            foreach (var value in values)
            {
                var numValue = ConvertToDouble(value);
                if (!double.IsNaN(numValue))
                {
                    min = Math.Min(min, numValue);
                    hasValue = true;
                }
            }

            return hasValue ? min : 0.0;
        }

        /// <summary>
        /// 最大值
        /// </summary>
        private static object PerformMax(object?[] values)
        {
            double max = double.MinValue;
            bool hasValue = false;
            
            foreach (var value in values)
            {
                var numValue = ConvertToDouble(value);
                if (!double.IsNaN(numValue))
                {
                    max = Math.Max(max, numValue);
                    hasValue = true;
                }
            }

            return hasValue ? max : 0.0;
        }

        /// <summary>
        /// 第一个非空值
        /// </summary>
        private static object PerformFirst(object?[] values)
        {
            foreach (var value in values)
            {
                if (value != null && !IsEmpty(value))
                    return value;
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 最后一个非空值
        /// </summary>
        private static object PerformLast(object?[] values)
        {
            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (values[i] != null && !IsEmpty(values[i]))
                    return values[i]!;
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 所有值相等
        /// </summary>
        private static object PerformAllEqual(object?[] values)
        {
            if (values.Length <= 1)
                return true;

            var first = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (!Equals(first, values[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 任何一个为真
        /// </summary>
        private static object PerformAny(object?[] values)
        {
            return values.Any(v => ConvertToBoolean(v));
        }

        /// <summary>
        /// 所有都为真
        /// </summary>
        private static object PerformAll(object?[] values)
        {
            return values.All(v => ConvertToBoolean(v));
        }

        /// <summary>
        /// 连接所有值
        /// </summary>
        private static object PerformConcat(object?[] values)
        {
            return string.Concat(values.Select(v => v?.ToString() ?? string.Empty));
        }

        /// <summary>
        /// 返回第一个非空值（Coalesce）
        /// </summary>
        private static object PerformCoalesce(object?[] values)
        {
            foreach (var value in values)
            {
                if (value != null)
                    return value;
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 开关逻辑（第一个值为条件，第二个为真值，第三个为假值）
        /// </summary>
        private static object PerformSwitch(object?[] values)
        {
            if (values.Length < 3)
                return DependencyProperty.UnsetValue;

            var condition = ConvertToBoolean(values[0]);
            return condition ? values[1] ?? DependencyProperty.UnsetValue : values[2] ?? DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 转换为布尔值
        /// </summary>
        private static bool ConvertToBoolean(object? value)
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                double d => d != 0,
                string s => !string.IsNullOrWhiteSpace(s),
                null => false,
                _ => true
            };
        }

        /// <summary>
        /// 转换为双精度数
        /// </summary>
        private static double ConvertToDouble(object? value)
        {
            return value switch
            {
                double d => d,
                float f => f,
                decimal dec => (double)dec,
                int i => i,
                long l => l,
                string s when double.TryParse(s, out var result) => result,
                _ => double.NaN
            };
        }

        /// <summary>
        /// 判断值是否为空
        /// </summary>
        private static bool IsEmpty(object? value)
        {
            return value switch
            {
                string s => string.IsNullOrWhiteSpace(s),
                Guid g => g == Guid.Empty,
                _ => false
            };
        }

        /// <summary>
        /// 解析参数
        /// </summary>
        private static Dictionary<string, string> ParseParameters(string parameter)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(parameter))
                return result;

            // 检查是否包含冒号（格式参数）
            if (parameter.Contains(':'))
            {
                var colonIndex = parameter.IndexOf(':');
                var operation = parameter[..colonIndex];
                var value = parameter[(colonIndex + 1)..];
                result["operation"] = operation;
                result[operation.ToLower()] = value;
            }
            else
            {
                result["operation"] = parameter;
            }

            return result;
        }
    }
}
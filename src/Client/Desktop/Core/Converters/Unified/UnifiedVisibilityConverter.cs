using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的可见性转换器 - 第2阶段架构重构
    /// 合并StringToVisibility、EmptyStringToVisibility、ZeroToVisibility等转换器
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class UnifiedVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转可见性
        /// </summary>
        public bool Invert { get; set; }
        
        /// <summary>
        /// 隐藏时使用Hidden还是Collapsed
        /// </summary>
        public bool UseHidden { get; set; }
        
        /// <summary>
        /// 空值处理模式
        /// </summary>
        public EmptyValueMode EmptyMode { get; set; } = EmptyValueMode.Hide;
        
        public enum EmptyValueMode
        {
            Hide,   // 空值时隐藏
            Show    // 空值时显示
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = DetermineVisibility(value, parameter);
            
            if (Invert)
                isVisible = !isVisible;
                
            if (isVisible)
                return Visibility.Visible;
                
            return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("UnifiedVisibilityConverter不支持反向转换");
        }

        private bool DetermineVisibility(object? value, object? parameter)
        {
            // null值处理
            if (value == null)
            {
                return EmptyMode == EmptyValueMode.Show;
            }
            
            // 布尔值
            if (value is bool boolValue)
            {
                return boolValue;
            }
            
            // 字符串
            if (value is string strValue)
            {
                var isEmpty = string.IsNullOrWhiteSpace(strValue);
                return EmptyMode == EmptyValueMode.Show ? isEmpty : !isEmpty;
            }
            
            // 数字
            if (IsNumericType(value))
            {
                var numValue = System.Convert.ToDouble(value);
                
                // 参数可以指定比较值
                if (parameter != null && double.TryParse(parameter.ToString(), out double compareValue))
                {
                    return Math.Abs(numValue - compareValue) > 0.0001;
                }
                
                // 默认：非零显示
                return Math.Abs(numValue) > 0.0001;
            }
            
            // 集合
            if (value is ICollection collection)
            {
                var hasItems = collection.Count > 0;
                return EmptyMode == EmptyValueMode.Show ? !hasItems : hasItems;
            }
            
            if (value is IEnumerable enumerable)
            {
                var hasItems = enumerable.Cast<object>().Any();
                return EmptyMode == EmptyValueMode.Show ? !hasItems : hasItems;
            }
            
            // 其他类型：非null即显示
            return EmptyMode != EmptyValueMode.Show;
        }

        private bool IsNumericType(object value)
        {
            return value is byte || value is sbyte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal;
        }
    }

    /// <summary>
    /// 多值可见性转换器
    /// 支持多个条件的AND/OR运算
    /// </summary>
    public class MultiValueVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// 逻辑运算模式
        /// </summary>
        public LogicalMode Mode { get; set; } = LogicalMode.And;
        
        /// <summary>
        /// 是否反转结果
        /// </summary>
        public bool Invert { get; set; }
        
        public enum LogicalMode
        {
            And,    // 所有条件都满足
            Or,     // 任一条件满足
            Xor     // 仅一个条件满足
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;
                
            var boolValues = values.Select(v => ConvertToBool(v)).ToArray();
            
            bool result = Mode switch
            {
                LogicalMode.And => boolValues.All(b => b),
                LogicalMode.Or => boolValues.Any(b => b),
                LogicalMode.Xor => boolValues.Count(b => b) == 1,
                _ => false
            };
            
            if (Invert)
                result = !result;
                
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiValueVisibilityConverter不支持反向转换");
        }

        private bool ConvertToBool(object? value)
        {
            if (value == null)
                return false;
                
            if (value is bool b)
                return b;
                
            if (value is Visibility v)
                return v == Visibility.Visible;
                
            if (value is string s)
                return !string.IsNullOrWhiteSpace(s);
                
            if (value.GetType().IsValueType)
            {
                try
                {
                    return System.Convert.ToBoolean(value);
                }
                catch
                {
                    return false;
                }
            }
            
            return true; // 非null引用类型默认为true
        }
    }
}
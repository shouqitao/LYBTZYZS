using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Collections;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的空值到可见性转换器
    /// 合并了原有的：
    /// - NullToVisibilityConverter
    /// - NotNullToVisibilityConverter
    /// - EmptyToVisibilityConverter
    /// 参数说明：
    /// - "Inverse" - 反转逻辑（空值显示，非空隐藏）
    /// - "Hidden" - 使用Hidden而非Collapsed
    /// - "InverseHidden" - 反转且使用Hidden
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isEmpty = IsEmpty(value);
            var paramStr = parameter as string ?? string.Empty;
            bool inverse = paramStr.Contains("Inverse", StringComparison.OrdinalIgnoreCase);
            bool useHidden = paramStr.Contains("Hidden", StringComparison.OrdinalIgnoreCase);

            // 确定基础逻辑
            bool shouldShow = inverse ? isEmpty : !isEmpty;

            // 返回相应的可见性
            if (shouldShow)
            {
                return Visibility.Visible;
            }
            else
            {
                return useHidden ? Visibility.Hidden : Visibility.Collapsed;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var paramStr = parameter as string ?? string.Empty;
                bool inverse = paramStr.Contains("Inverse", StringComparison.OrdinalIgnoreCase);

                bool isVisible = visibility == Visibility.Visible;
                
                // 对于反转逻辑：可见=null，不可见=非null
                // 对于正常逻辑：可见=非null，不可见=null
                if (inverse)
                {
                    return isVisible ? null : new object();
                }
                else
                {
                    return isVisible ? new object() : null;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return Convert(null, targetType, parameter, culture);
            }

            // 多值逻辑：任何一个为空则认为整体为空
            bool anyEmpty = false;
            foreach (var value in values)
            {
                if (IsEmpty(value))
                {
                    anyEmpty = true;
                    break;
                }
            }

            return Convert(anyEmpty ? null : new object(), targetType, parameter, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判断值是否为空
        /// </summary>
        private static bool IsEmpty(object? value)
        {
            if (value == null)
                return true;

            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            if (value is ICollection collection)
                return collection.Count == 0;

            if (value is IEnumerable enumerable)
            {
                foreach (var _ in enumerable)
                {
                    return false;
                }
                return true;
            }

            if (value is Guid guid)
                return guid == Guid.Empty;

            if (value is int intValue)
                return intValue == 0;

            if (value is decimal decimalValue)
                return decimalValue == 0;

            if (value is double doubleValue)
                return doubleValue == 0;

            if (value is float floatValue)
                return floatValue == 0;

            if (value is DateTime dateTime)
                return dateTime == DateTime.MinValue;

            return false;
        }
    }
}
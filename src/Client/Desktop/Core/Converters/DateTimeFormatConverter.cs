using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 统一的日期时间格式转换器
    /// 合并了原有的：
    /// - DateTimeConverter
    /// - DateFormatConverter
    /// - TimeFormatConverter
    /// - RelativeDateTimeConverter
    /// 参数说明：
    /// - "Date" - 仅显示日期 (yyyy-MM-dd)
    /// - "Time" - 仅显示时间 (HH:mm:ss)
    /// - "DateTime" - 显示日期时间 (yyyy-MM-dd HH:mm:ss)
    /// - "Relative" - 相对时间（如"3分钟前"）
    /// - "Custom:格式字符串" - 自定义格式
    /// - null/空 - 默认格式 (yyyy-MM-dd HH:mm)
    /// </summary>
    public class DateTimeFormatConverter : IValueConverter, IMultiValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime dateTime)
            {
                if (value is string strDate && DateTime.TryParse(strDate, out var parsedDate))
                {
                    dateTime = parsedDate;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            var format = parameter as string;

            return format?.ToLowerInvariant() switch
            {
                "date" => dateTime.ToString("yyyy-MM-dd", culture),
                "time" => dateTime.ToString("HH:mm:ss", culture),
                "datetime" => dateTime.ToString("yyyy-MM-dd HH:mm:ss", culture),
                "relative" => GetRelativeTime(dateTime),
                _ when format?.StartsWith("Custom:", StringComparison.OrdinalIgnoreCase) == true =>
                    dateTime.ToString(format[7..], culture),
                _ => dateTime.ToString("yyyy-MM-dd HH:mm", culture)
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string strValue && DateTime.TryParse(strValue, culture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            return DependencyProperty.UnsetValue;
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values[0] is not DateTime dateTime)
            {
                return DependencyProperty.UnsetValue;
            }

            // 支持多值输入，第二个值可以是格式字符串
            var format = values.Length > 1 && values[1] is string fmt ? fmt : parameter as string;
            return Convert(dateTime, targetType, format, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string GetRelativeTime(DateTime dateTime)
        {
            var now = DateTime.Now;
            var diff = now - dateTime;

            if (diff.TotalSeconds < 60)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}小时前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}天前";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)}周前";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)}个月前";
            
            return $"{(int)(diff.TotalDays / 365)}年前";
        }
    }
}
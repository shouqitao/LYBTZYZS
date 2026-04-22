using System;
using LYBT.Desktop.Infrastructure.Navigation;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// Timestamp Format Converter - Phase 2.1: Navigation Improvements
    /// 将 DateTime 转换为相对时间字符串（例如："3 分钟前"）
    /// </summary>
    public class TimestampFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime timestamp)
            {
                return FormatTimestamp(timestamp);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 格式化时间戳为相对时间
        /// </summary>
        public static string FormatTimestamp(DateTime timestamp)
        {
            var now = DateTime.UtcNow;
            var diff = now - timestamp;

            if (diff.TotalSeconds < 60)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} 分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} 小时前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} 天前";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)} 周前";
            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} 月前";

            return timestamp.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
        }
    }

    /// <summary>
    /// Icon Converter - Phase 2.1: Navigation Improvements
    /// 根据 URI 返回对应的图标
    /// </summary>
    public class IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string uri)
            {
                return GetIconForUri(uri);
            }

            return "📄"; // Default icon
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 根据返回图标
        /// </summary>
        public static string GetIconForUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return "📄";

            // Check for specific patterns
            if (uri.Contains("MedicalCase") || uri.Contains("医案"))
                return "📋";
            if (uri.Contains("Patient") || uri.Contains("患者"))
                return "👤";
            if (uri.Contains("Prescription") || uri.Contains("处方"))
                return "💊";
            if (uri.Contains("Home") || uri.Contains("主页"))
                return "🏠";
            if (uri.Contains("Report") || uri.Contains("报告"))
                return "📊";
            if (uri.Contains("Settings") || uri.Contains("设置"))
                return "⚙️";
            if (uri.Contains("Calendar") || uri.Contains("日历"))
                return "📅";
            if (uri.Contains("Search") || uri.Contains("搜索"))
                return "🔍";

            return "📄"; // Default
        }
    }

    /// <summary>
    /// Suggestion Type Color Converter - Phase 2.1: Navigation Improvements
    /// 将 SuggestionType 枚举转换为对应的颜色
    /// </summary>
    public class SuggestionTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SuggestionType type)
            {
                return GetColorForType(type);
            }

            return "#757575"; // Default gray
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取建议类型对应的颜色
        /// </summary>
        public static string GetColorForType(SuggestionType type)
        {
            return type switch
            {
                SuggestionType.Contextual => "#2196F3", // Blue
                SuggestionType.Frequent => "#4CAF50", // Green
                SuggestionType.TimeBased => "#FF9800", // Orange
                SuggestionType.Recent => "#9C27B0", // Purple
                SuggestionType.Pinned => "#F44336", // Red
                _ => "#757575" // Gray
            };
        }
    }

    /// <summary>
    /// Suggestion Type Text Converter - Phase 2.1: Navigation Improvements
    /// 将 SuggestionType 枚举转换为对应的中文文本
    /// </summary>
    public class SuggestionTypeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SuggestionType type)
            {
                return GetTextForType(type);
            }

            return "建议";
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取建议类型对应的中文文本
        /// </summary>
        public static string GetTextForType(SuggestionType type)
        {
            return type switch
            {
                SuggestionType.Contextual => "上下文",
                SuggestionType.Frequent => "常用",
                SuggestionType.TimeBased => "时间",
                SuggestionType.Recent => "最近",
                SuggestionType.Pinned => "固定",
                _ => "建议"
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LYBT.WPF.Client.Core.Helpers
{
    /// <summary>
    /// 搜索帮助类，提供通用的搜索和筛选功能
    /// </summary>
    public static class SearchHelper
    {
        /// <summary>
        /// 对集合进行关键字搜索
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="searchProperties">要搜索的属性名称</param>
        /// <returns>搜索结果</returns>
        public static IEnumerable<T> Search<T>(
            IEnumerable<T> source, 
            string keyword, 
            params string[] searchProperties)
        {
            if (source == null || string.IsNullOrWhiteSpace(keyword))
                return source ?? Enumerable.Empty<T>();

            keyword = keyword.ToLower();
            var type = typeof(T);
            var properties = searchProperties.Select(p => type.GetProperty(p))
                                             .Where(p => p != null)
                                             .ToList();

            return source.Where(item =>
            {
                foreach (var property in properties)
                {
                    var value = property?.GetValue(item)?.ToString();
                    if (!string.IsNullOrEmpty(value) && value.ToLower().Contains(keyword))
                        return true;
                }
                return false;
            });
        }

        /// <summary>
        /// 对集合进行多条件筛选
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="filters">筛选条件</param>
        /// <returns>筛选结果</returns>
        public static IEnumerable<T> Filter<T>(
            IEnumerable<T> source, 
            params Func<T, bool>[] filters)
        {
            if (source == null || filters == null || filters.Length == 0)
                return source ?? Enumerable.Empty<T>();

            var result = source;
            foreach (var filter in filters.Where(f => f != null))
            {
                result = result.Where(filter);
            }
            return result;
        }

        /// <summary>
        /// 构建数值范围筛选条件
        /// </summary>
        public static Func<T, bool> BuildRangeFilter<T, TValue>(
            string propertyName, 
            TValue? min, 
            TValue? max) 
            where TValue : struct, IComparable<TValue>
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property == null) return _ => true;

            return item =>
            {
                var value = property.GetValue(item);
                if (value == null) return false;

                if (value is TValue typedValue)
                {
                    if (min.HasValue && typedValue.CompareTo(min.Value) < 0) return false;
                    if (max.HasValue && typedValue.CompareTo(max.Value) > 0) return false;
                    return true;
                }
                return false;
            };
        }

        /// <summary>
        /// 构建枚举值筛选条件
        /// </summary>
        public static Func<T, bool> BuildEnumFilter<T, TEnum>(
            string propertyName, 
            TEnum? value) 
            where TEnum : struct, Enum
        {
            if (!value.HasValue) return _ => true;

            var property = typeof(T).GetProperty(propertyName);
            if (property == null) return _ => true;

            return item =>
            {
                var propValue = property.GetValue(item);
                return propValue != null && propValue.Equals(value.Value);
            };
        }

        /// <summary>
        /// 构建布尔值筛选条件
        /// </summary>
        public static Func<T, bool> BuildBooleanFilter<T>(
            string propertyName, 
            bool? value)
        {
            if (!value.HasValue) return _ => true;

            var property = typeof(T).GetProperty(propertyName);
            if (property == null) return _ => true;

            return item =>
            {
                var propValue = property.GetValue(item);
                return propValue is bool boolValue && boolValue == value.Value;
            };
        }

        /// <summary>
        /// 高亮显示搜索关键字
        /// </summary>
        public static string HighlightKeyword(string text, string keyword, string highlightTag = "**")
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
                return text;

            var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return text;

            var before = text.Substring(0, index);
            var match = text.Substring(index, keyword.Length);
            var after = text.Substring(index + keyword.Length);

            return $"{before}{highlightTag}{match}{highlightTag}{after}";
        }
    }
}
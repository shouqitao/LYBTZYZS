using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace LYBT.Common.Extensions {

    /// <summary>
    /// Enum 扩展方法 - 性能优化版本
    /// </summary>
    public static class EnumExtensions {
        // 使用缓存避免反射性能开销
        private static readonly ConcurrentDictionary<Enum, string> _descriptionCache = new();

        /// <summary>
        /// 获取枚举值的描述（带缓存）
        /// </summary>
        public static string GetDescription(this Enum value) {
            return _descriptionCache.GetOrAdd(value, static enumValue => {
                var field = enumValue.GetType().GetField(enumValue.ToString());
                var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
                return attribute?.Description ?? enumValue.ToString();
            });
        }

        /// <summary>
        /// 批量获取枚举值描述（用于下拉列表等场景）
        /// </summary>
        public static Dictionary<T, string> GetAllDescriptions<T>() where T : struct, Enum {
            var result = new Dictionary<T, string>();
            foreach (T value in Enum.GetValues<T>()) {
                result[value] = value.GetDescription();
            }
            return result;
        }

        /// <summary>
        /// 根据描述获取枚举值
        /// </summary>
        public static T? GetEnumByDescription<T>(string description) where T : struct, Enum {
            foreach (T value in Enum.GetValues<T>()) {
                if (value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase)) {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// Alias for GetDescription to align with README wording.
        /// </summary>
        public static string ToChinese(this Enum value) => value.GetDescription();

        /// <summary>
        /// 检查枚举值是否有效
        /// </summary>
        public static bool IsValidEnumValue<T>(this T value) where T : struct, Enum {
            return Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举的所有值
        /// </summary>
        public static IEnumerable<T> GetAllValues<T>() where T : struct, Enum {
            return Enum.GetValues<T>();
        }
    }
}
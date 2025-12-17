using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace LYBT.Shared.Models.Extensions
{

    /// <summary>
    /// 枚举扩展方法 - 前后端共享（性能优化版本）
    /// </summary>
    public static class EnumExtensions
    {

        // 使用缓存避免反射性能开销
        private static readonly ConcurrentDictionary<Enum, string> _descriptionCache = new();

        /// <summary>
        /// 获取枚举值的描述（带缓存）
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>描述文本</returns>
        public static string GetDescription(this Enum value)
        {
            return _descriptionCache.GetOrAdd(value, static enumValue =>
            {
                var field = enumValue.GetType().GetField(enumValue.ToString());
                var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
                return attribute?.Description ?? enumValue.ToString();
            });
        }

        /// <summary>
        /// 批量获取枚举值描述（用于下拉列表等场景）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值和描述的字典</returns>
        public static Dictionary<T, string> GetAllDescriptions<T>() where T : struct, Enum
        {
            var result = new Dictionary<T, string>();
            foreach (T value in Enum.GetValues<T>())
            {
                result[value] = value.GetDescription();
            }

            return result;
        }

        /// <summary>
        /// 根据描述获取枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="description">描述文本</param>
        /// <returns>对应的枚举值，未找到则返回null</returns>
        public static T? GetEnumByDescription<T>(string description) where T : struct, Enum
        {
            foreach (T value in Enum.GetValues<T>())
            {
                if (value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查枚举值是否有效
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举值</param>
        /// <returns>是否有效</returns>
        public static bool IsValidEnumValue<T>(this T value) where T : struct, Enum
        {
            return Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举的所有值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>所有枚举值</returns>
        public static IEnumerable<T> GetAllValues<T>() where T : struct, Enum
        {
            return Enum.GetValues<T>();
        }

        /// <summary>
        /// 将枚举转换为键值对列表（用于前端下拉框等）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>键值对列表</returns>
        public static List<KeyValuePair<int, string>> ToKeyValueList<T>() where T : struct, Enum
        {
            var result = new List<KeyValuePair<int, string>>();
            foreach (T value in Enum.GetValues<T>())
            {
                var intValue = Convert.ToInt32(value);
                var description = value.GetDescription();
                result.Add(new KeyValuePair<int, string>(intValue, description));
            }

            return result;
        }
    }
}

using System.ComponentModel;
using LYBT.Shared.Models.Extensions;

namespace LYBT.Shared.Utilities.Helpers {

    /// <summary>
    /// 枚举工具类 - 前后端共享版本
    /// 提供枚举的通用操作方法，不依赖特定UI框架
    /// </summary>
    [Description("枚举工具类")]
    public static class EnumHelper {

        /// <summary>
        /// 获取枚举值的显示名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>显示名称</returns>
        public static string GetDescription<T>(T enumValue) where T : Enum {
            return enumValue.GetDescription();
        }

        /// <summary>
        /// 获取枚举类型的所有值和描述
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值和描述的字典</returns>
        public static Dictionary<T, string> GetEnumDescriptions<T>() where T : Enum {
            var result = new Dictionary<T, string>();

            foreach (T value in Enum.GetValues(typeof(T))) {
                result[value] = value.GetDescription();
            }

            return result;
        }

        /// <summary>
        /// 根据描述获取枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="description">描述文本</param>
        /// <returns>匹配的枚举值，如果没找到则返回默认值</returns>
        public static T GetEnumByDescription<T>(string description) where T : Enum {
            foreach (T value in Enum.GetValues(typeof(T))) {
                if (value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase)) {
                    return value;
                }
            }

            return default(T)!;
        }

        /// <summary>
        /// 枚举值转换为整数
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>整数值</returns>
        public static int ToInt<T>(T enumValue) where T : Enum {
            return Convert.ToInt32(enumValue);
        }

        /// <summary>
        /// 整数转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">整数值</param>
        /// <returns>枚举值</returns>
        public static T FromInt<T>(int value) where T : Enum {
            return (T)Enum.ToObject(typeof(T), value);
        }

        /// <summary>
        /// 字符串转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">字符串值</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>枚举值</returns>
        public static T Parse<T>(string value, bool ignoreCase = true) where T : Enum {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        /// <summary>
        /// 尝试将字符串转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">字符串值</param>
        /// <param name="result">转换结果</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>转换是否成功</returns>
        public static bool TryParse<T>(string value, out T result, bool ignoreCase = true) where T : struct, Enum {
            return Enum.TryParse(value, ignoreCase, out result);
        }

        /// <summary>
        /// 检查枚举值是否已定义
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">要检查的值</param>
        /// <returns>是否已定义</returns>
        public static bool IsDefined<T>(object value) where T : Enum {
            return Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举类型的所有值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值列表</returns>
        public static List<T> GetValues<T>() where T : Enum {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        /// <summary>
        /// 获取枚举类型的所有名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举名称列表</returns>
        public static List<string> GetNames<T>() where T : Enum {
            return Enum.GetNames(typeof(T)).ToList();
        }

        /// <summary>
        /// 获取枚举的键值对列表（用于下拉框等）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>键值对列表</returns>
        public static List<KeyValuePair<T, string>> GetKeyValuePairs<T>() where T : Enum {
            var result = new List<KeyValuePair<T, string>>();

            foreach (T value in Enum.GetValues(typeof(T))) {
                result.Add(new KeyValuePair<T, string>(value, value.GetDescription()));
            }

            return result;
        }

        /// <summary>
        /// 获取枚举的整数值和描述的键值对列表
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>整数值-描述键值对列表</returns>
        public static List<KeyValuePair<int, string>> GetIntKeyValuePairs<T>() where T : Enum {
            var result = new List<KeyValuePair<int, string>>();

            foreach (T value in Enum.GetValues(typeof(T))) {
                result.Add(new KeyValuePair<int, string>(ToInt(value), value.GetDescription()));
            }

            return result;
        }

        /// <summary>
        /// 获取枚举的字符串值和描述的键值对列表
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>字符串值-描述键值对列表</returns>
        public static List<KeyValuePair<string, string>> GetStringKeyValuePairs<T>() where T : Enum {
            var result = new List<KeyValuePair<string, string>>();

            foreach (T value in Enum.GetValues(typeof(T))) {
                result.Add(new KeyValuePair<string, string>(value.ToString(), value.GetDescription()));
            }

            return result;
        }

        /// <summary>
        /// 获取枚举的所有显示名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>显示名称列表</returns>
        public static List<string> GetDescriptions<T>() where T : Enum {
            return Enum.GetValues(typeof(T))
                      .Cast<T>()
                      .Select(value => value.GetDescription())
                      .ToList();
        }

        /// <summary>
        /// 根据整数值获取枚举，包含验证
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">整数值</param>
        /// <param name="result">转换结果</param>
        /// <returns>转换是否成功</returns>
        public static bool TryFromInt<T>(int value, out T result) where T : struct, Enum {
            result = default;

            if (Enum.IsDefined(typeof(T), value)) {
                result = (T)Enum.ToObject(typeof(T), value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取枚举值的索引位置
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>索引位置（从0开始），如果未找到返回-1</returns>
        public static int GetIndex<T>(T enumValue) where T : Enum {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            return Array.IndexOf(values, enumValue);
        }

        /// <summary>
        /// 根据索引获取枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="index">索引位置</param>
        /// <returns>枚举值，如果索引无效返回默认值</returns>
        public static T FromIndex<T>(int index) where T : Enum {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            return index >= 0 && index < values.Length ? values[index] : default(T)!;
        }

        /// <summary>
        /// 获取枚举的最大值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>最大值</returns>
        public static T GetMaxValue<T>() where T : Enum {
            return Enum.GetValues(typeof(T))
                      .Cast<T>()
                      .Max()!;
        }

        /// <summary>
        /// 获取枚举的最小值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>最小值</returns>
        public static T GetMinValue<T>() where T : Enum {
            return Enum.GetValues(typeof(T))
                      .Cast<T>()
                      .Min()!;
        }

        /// <summary>
        /// 检查枚举是否有指定的Description属性值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="description">要检查的描述</param>
        /// <returns>是否存在该描述</returns>
        public static bool HasDescription<T>(string description) where T : Enum {
            return Enum.GetValues(typeof(T))
                      .Cast<T>()
                      .Any(value => value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取枚举值的下一个值（循环）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">当前枚举值</param>
        /// <returns>下一个枚举值</returns>
        public static T GetNext<T>(T enumValue) where T : Enum {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var index = Array.IndexOf(values, enumValue);
            return values[(index + 1) % values.Length];
        }

        /// <summary>
        /// 获取枚举值的上一个值（循环）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">当前枚举值</param>
        /// <returns>上一个枚举值</returns>
        public static T GetPrevious<T>(T enumValue) where T : Enum {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var index = Array.IndexOf(values, enumValue);
            return values[(index - 1 + values.Length) % values.Length];
        }

        /// <summary>
        /// 获取枚举值的数量
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值的数量</returns>
        public static int GetCount<T>() where T : Enum {
            return Enum.GetValues(typeof(T)).Length;
        }

        /// <summary>
        /// 随机获取一个枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>随机的枚举值</returns>
        public static T GetRandom<T>() where T : Enum {
            var values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var randomIndex = Random.Shared.Next(values.Length);
            return values[randomIndex];
        }
    }
}

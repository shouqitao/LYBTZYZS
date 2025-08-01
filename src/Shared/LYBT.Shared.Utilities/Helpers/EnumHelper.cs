using LYBT.Shared.Models.Extensions;
using System.ComponentModel;

namespace LYBT.Shared.Utilities.Helpers
{
    /// <summary>
    /// 枚举工具类 - 前后端共享版本
    /// 提供枚举的通用操作方法，不依赖特定UI框架
    /// </summary>
    [Description("枚举工具类")]
    public static class EnumHelper
    {
        /// <summary>
        /// 获取枚举值的显示名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="enumValue">枚举值</param>
        /// <returns>显示名称</returns>
        public static string GetDescription<T>(T enumValue) where T : Enum
        {
            return enumValue.GetDescription();
        }

        /// <summary>
        /// 获取枚举类型的所有值和描述
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值和描述的字典</returns>
        public static Dictionary<T, string> GetEnumDescriptions<T>() where T : Enum
        {
            var result = new Dictionary<T, string>();
            
            foreach (T value in Enum.GetValues(typeof(T)))
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
        /// <returns>匹配的枚举值，如果没找到则返回默认值</returns>
        public static T GetEnumByDescription<T>(string description) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (value.GetDescription().Equals(description, StringComparison.OrdinalIgnoreCase))
                {
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
        public static int ToInt<T>(T enumValue) where T : Enum
        {
            return Convert.ToInt32(enumValue);
        }

        /// <summary>
        /// 整数转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">整数值</param>
        /// <returns>枚举值</returns>
        public static T FromInt<T>(int value) where T : Enum
        {
            return (T)Enum.ToObject(typeof(T), value);
        }

        /// <summary>
        /// 字符串转换为枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">字符串值</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>枚举值</returns>
        public static T Parse<T>(string value, bool ignoreCase = true) where T : Enum
        {
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
        public static bool TryParse<T>(string value, out T result, bool ignoreCase = true) where T : struct, Enum
        {
            return Enum.TryParse(value, ignoreCase, out result);
        }

        /// <summary>
        /// 检查枚举值是否已定义
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">要检查的值</param>
        /// <returns>是否已定义</returns>
        public static bool IsDefined<T>(object value) where T : Enum
        {
            return Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// 获取枚举类型的所有值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举值列表</returns>
        public static List<T> GetValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }

        /// <summary>
        /// 获取枚举类型的所有名称
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>枚举名称列表</returns>
        public static List<string> GetNames<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).ToList();
        }

        /// <summary>
        /// 获取枚举的键值对列表（用于下拉框等）
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>键值对列表</returns>
        public static List<KeyValuePair<T, string>> GetKeyValuePairs<T>() where T : Enum
        {
            var result = new List<KeyValuePair<T, string>>();
            
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                result.Add(new KeyValuePair<T, string>(value, value.GetDescription()));
            }
            
            return result;
        }

        /// <summary>
        /// 获取枚举的整数值和描述的键值对列表
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>整数值-描述键值对列表</returns>
        public static List<KeyValuePair<int, string>> GetIntKeyValuePairs<T>() where T : Enum
        {
            var result = new List<KeyValuePair<int, string>>();
            
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                result.Add(new KeyValuePair<int, string>(ToInt(value), value.GetDescription()));
            }
            
            return result;
        }
    }
}
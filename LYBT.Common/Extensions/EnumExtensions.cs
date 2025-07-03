using System;
using System.ComponentModel;
using System.Reflection;

namespace LYBT.Common.Extensions {

    /// <summary>
    /// Enum 扩展方法
    /// </summary>
    public static class EnumExtensions {

        /// <summary>
        /// 获取枚举值的描述
        /// </summary>
        public static string GetDescription(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }
}

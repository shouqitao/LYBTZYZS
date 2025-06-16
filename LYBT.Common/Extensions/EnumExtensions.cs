using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.Common.Extensions {
    /// <summary>
    /// 枚举扩展方法，支持将枚举值转换为中文描述
    /// </summary>
    public static class EnumExtensions {
        /// <summary>
        /// 获取枚举值的Description描述（用于界面展示中文）
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>中文描述或枚举名称</returns>
        public static string ToDescription(this Enum value) {
            // 获取枚举字段
            FieldInfo? field = value.GetType().GetField(value.ToString());
            // 尝试获取Description特性
            DescriptionAttribute? attr = field?.GetCustomAttribute<DescriptionAttribute>();
            // 返回中文描述或枚举名称
            return attr?.Description ?? value.ToString();
        }
    }
}

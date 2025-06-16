using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LYBT.Common.Extensions {
    /// <summary>
    /// 枚举辅助类：将枚举转换为中文描述
    /// </summary>
    public static class EnumHelper {
        public static string ToChinese(this Enum value) {
            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();
            var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attr?.Description ?? value.ToString();
        }

        public static List<KeyValuePair<string, string>> BuildComboBoxSource<TEnum>() where TEnum : Enum {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Select(e => new KeyValuePair<string, string>(e.ToString(), e.ToChinese()))
                       .ToList();
        }
    }
}

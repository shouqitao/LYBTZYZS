using LYBT.Common.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace LYBT.Common.Helpers;

/// <summary>
/// 枚举帮助类：生成键值对列表用于绑定 ComboBox
/// </summary>
public static class EnumHelper {
    /// <summary>
    /// 构建 ComboBox 可用的键值对集合（Key=枚举值，Value=中文描述）
    /// </summary>
    /// <typeparam name="TEnum">目标枚举类型</typeparam>
    public static List<KeyValuePair<TEnum, string>> BuildComboBoxSource<TEnum>() where TEnum : Enum {
        return [.. Enum.GetValues(typeof(TEnum))
                   .Cast<TEnum>()
                   .Select(e => new KeyValuePair<TEnum, string>(
                       e,
                       (e as Enum)!.ToChinese() // 调用扩展方法
                   ))];
    }
}

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using LYBT.Entities.Attributes;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// 敏感数据脱敏处理器
/// </summary>
public static class SensitiveDataMasker
{
    /// <summary>
    /// 根据脱敏模式处理字符串值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="mode">脱敏模式</param>
    /// <param name="dataType">数据类型（用于智能脱敏）</param>
    /// <returns>脱敏后的值</returns>
    public static string Mask(string? value, MaskingMode mode, SensitiveDataType dataType = SensitiveDataType.PersonalInfo)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return mode switch
        {
            MaskingMode.Partial => MaskPartial(value, dataType),
            MaskingMode.Full => "[已隐藏]",
            MaskingMode.Hash => MaskHash(value),
            MaskingMode.Default => MaskDefault(value),
            _ => MaskDefault(value)
        };
    }

    /// <summary>
    /// 部分隐藏（显示前后几位）
    /// </summary>
    private static string MaskPartial(string value, SensitiveDataType dataType)
    {
        // 根据数据类型智能处理
        return dataType switch
        {
            // 手机号：138****1234
            SensitiveDataType.ContactInfo when value.Length >= 7 =>
                $"{value[..3]}****{value[^4..]}",

            // 身份证号：110***********1234
            SensitiveDataType.IdentityInfo when value.Length >= 8 =>
                $"{value[..3]}{"*".PadRight(value.Length - 7, '*')}{value[^4..]}",

            // 其他：显示前2后2
            _ when value.Length > 4 =>
                $"{value[..2]}{"*".PadRight(value.Length - 4, '*')}{value[^2..]}",

            // 太短的直接全部隐藏
            _ => "****"
        };
    }

    /// <summary>
    /// 哈希脱敏（适用于病史等长文本）
    /// </summary>
    private static string MaskHash(string value)
    {
        // 使用SHA256生成短哈希标识
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var shortHash = Convert.ToHexString(hash)[..8];
        return $"[REDACTED:{shortHash}]";
    }

    /// <summary>
    /// 默认脱敏（中间用*替代）
    /// </summary>
    private static string MaskDefault(string value)
    {
        if (value.Length <= 2)
            return "**";

        if (value.Length <= 6)
            return $"{value[0]}{"*".PadRight(value.Length - 2, '*')}{value[^1]}";

        // 长文本：显示前3后3
        return $"{value[..3]}{"*".PadRight(Math.Min(value.Length - 6, 10), '*')}{value[^3..]}";
    }

    /// <summary>
    /// 检查属性是否标记为敏感数据
    /// </summary>
    public static SensitiveDataAttribute? GetSensitiveDataAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<SensitiveDataAttribute>();
    }

    /// <summary>
    /// 对对象的所有敏感字段进行脱敏处理
    /// </summary>
    /// <param name="obj">原始对象</param>
    /// <returns>脱敏后的字典表示</returns>
    public static Dictionary<string, object?> MaskObject(object obj)
    {
        var result = new Dictionary<string, object?>();
        var type = obj.GetType();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(obj);
            var sensitiveAttr = GetSensitiveDataAttribute(property);

            if (sensitiveAttr != null && sensitiveAttr.RequireLogMasking && value is string strValue)
            {
                result[property.Name] = Mask(strValue, sensitiveAttr.MaskingMode, sensitiveAttr.DataType);
            }
            else
            {
                result[property.Name] = value;
            }
        }

        return result;
    }
}

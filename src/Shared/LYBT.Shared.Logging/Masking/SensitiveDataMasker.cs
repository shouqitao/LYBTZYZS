using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LYBT.Shared.Logging.Masking;

/// <summary>
/// 敏感数据脱敏处理器
/// 统一的敏感数据脱敏入口，整合属性级和文本级脱敏
/// </summary>
public static partial class SensitiveDataMasker
{
    #region 文本脱敏正则模式

    /// <summary>
    /// 密码相关字段模式
    /// 匹配: password=xxx, token=xxx, secret=xxx 等
    /// </summary>
    [GeneratedRegex(@"(?i)(password|pwd|pass|passwd|secret|token|key|authorization|bearer)\s*[:=]\s*[""']?([^""'\s;]+)[""']?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PasswordPattern();

    /// <summary>
    /// 连接字符串敏感字段模式
    /// 匹配: password=xxx; user id=xxx; 等
    /// </summary>
    [GeneratedRegex(@"(?i)(password|pwd|user id|uid)=([^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ConnectionStringPattern();

    /// <summary>
    /// Bearer Token模式
    /// </summary>
    [GeneratedRegex(@"(?i)bearer\s+[\w\-._~+/]+(\.?[\w\-._~+/]+)*=*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BearerTokenPattern();

    /// <summary>
    /// 敏感字段名列表
    /// </summary>
    private static readonly string[] SensitiveFieldNames =
    [
        "Password", "NewPassword", "OldPassword", "CurrentPassword",
        "Token", "AccessToken", "RefreshToken", "BearerToken",
        "Secret", "SecretKey", "ApiKey", "ClientSecret",
        "ConnectionString", "ConnStr", "DatabaseConnection",
        "Authorization", "AuthToken", "SessionId",
        "CreditCard", "CardNumber", "CVV", "SSN",
        "PrivateKey", "PublicKey", "Certificate",
        "Salt", "Hash", "Nonce", "IV"
    ];

    #endregion

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

    #region 文本级脱敏方法

    /// <summary>
    /// URI敏感参数模式
    /// 匹配: password=xxx&token=xxx 等查询参数
    /// LOG-016: URI敏感数据脱敏
    /// </summary>
    [GeneratedRegex(@"(?i)(password|token|key|secret|credential|apikey|access_token|refresh_token|auth)=([^&\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UriSensitiveParamPattern();

    /// <summary>
    /// 对URI进行敏感参数脱敏处理
    /// LOG-016: URI敏感数据脱敏
    /// </summary>
    /// <param name="uri">原始URI字符串</param>
    /// <returns>脱敏后的URI字符串</returns>
    public static string MaskUri(string? uri)
    {
        if (string.IsNullOrEmpty(uri))
            return uri ?? string.Empty;

        // 替换敏感查询参数: password=xxx -> password=***
        return UriSensitiveParamPattern().Replace(uri, match =>
        {
            var paramName = match.Groups[1].Value;
            return $"{paramName}=***";
        });
    }

    /// <summary>
    /// 对字符串进行文本级脱敏处理
    /// 适用于日志消息、连接字符串等文本
    /// </summary>
    /// <param name="input">原始字符串</param>
    /// <returns>脱敏后的字符串</returns>
    public static string SanitizeText(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var sanitized = input;

        // 替换Bearer Token（必须在PasswordPattern之前，避免bearer被PasswordPattern部分匹配）
        sanitized = BearerTokenPattern().Replace(sanitized, "Bearer [REDACTED]");

        // 替换密码模式: password=xxx -> password=[REDACTED]
        sanitized = PasswordPattern().Replace(sanitized, match =>
        {
            var field = match.Groups[1].Value;
            return $"{field}=[REDACTED]";
        });

        // 替换连接字符串中的敏感信息
        sanitized = ConnectionStringPattern().Replace(sanitized, match =>
        {
            var key = match.Groups[1].Value;
            return $"{key}=[REDACTED]";
        });

        return sanitized;
    }

    /// <summary>
    /// 检查字段名是否为敏感字段
    /// </summary>
    /// <param name="fieldName">字段名</param>
    /// <returns>是否敏感字段</returns>
    public static bool IsSensitiveFieldName(string? fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return false;

        return SensitiveFieldNames.Any(sensitive =>
            fieldName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 对对象进行脱敏序列化
    /// </summary>
    /// <param name="obj">原始对象</param>
    /// <returns>脱敏后的JSON字符串</returns>
    public static string? SerializeWithSanitization(object? obj)
    {
        if (obj == null)
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new SanitizingJsonConverter() }
            };

            var json = JsonSerializer.Serialize(obj, options);
            return SanitizeText(json);
        }
        catch
        {
            return "[Serialization Error - Sanitized]";
        }
    }

    /// <summary>
    /// 对异常信息进行脱敏处理
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="maxStackTraceLines">最大堆栈行数(默认5)</param>
    /// <returns>脱敏后的异常信息</returns>
    public static string SanitizeException(Exception? exception, int maxStackTraceLines = 5)
    {
        if (exception == null)
            return string.Empty;

        var message = SanitizeText(exception.Message);

        // 限制堆栈跟踪长度
        var stackTrace = exception.StackTrace;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            var lines = stackTrace.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > maxStackTraceLines)
            {
                stackTrace = string.Join(Environment.NewLine, lines.Take(maxStackTraceLines)) +
                            Environment.NewLine + "[... truncated ...]";
            }
        }

        return $"{exception.GetType().Name}: {message}{Environment.NewLine}{stackTrace}";
    }

    #endregion

    #region 内部类

    /// <summary>
    /// 脱敏JSON序列化转换器
    /// </summary>
    private class SanitizingJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) => true;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("This converter is only for writing");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var type = value.GetType();

            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid))
            {
                JsonSerializer.Serialize(writer, value, type, new JsonSerializerOptions());
                return;
            }

            writer.WriteStartObject();

            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead)
                    continue;

                var propertyName = property.Name;
                var propertyValue = property.GetValue(value);

                writer.WritePropertyName(propertyName);

                if (propertyValue == null)
                {
                    writer.WriteNullValue();
                }
                else if (IsSensitiveFieldName(propertyName))
                {
                    writer.WriteStringValue("[REDACTED]");
                }
                else if (property.PropertyType == typeof(string))
                {
                    var stringValue = propertyValue.ToString();
                    // 对字符串值进行文本级脱敏
                    writer.WriteStringValue(SanitizeText(stringValue));
                }
                else
                {
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
    }

    #endregion
}

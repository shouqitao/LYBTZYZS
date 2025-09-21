using System.Text.Json;
using System.Text.RegularExpressions;

namespace LYBT.Shared.Utilities.Security;

/// <summary>
/// 敏感数据脱敏工具类
/// </summary>
public static class SensitiveDataMasker
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd", "pass", "passwd",
        "token", "accesstoken", "refreshtoken", "bearertoken", "apikey", "api_key",
        "secret", "secretkey", "privatekey", "key",
        "authorization", "auth",
        "credential", "credentials",
        "connectionstring", "connstring",
        "encryptionkey", "signingkey",
        "certificate", "cert",
        "ssn", "socialsecurity",
        "creditcard", "cardnumber", "cvv", "cvc"
    };

    private static readonly Regex JwtPattern = new(@"Bearer\s+[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+", RegexOptions.Compiled);
    private static readonly Regex Base64Pattern = new(@"[A-Za-z0-9+/]{20,}={0,2}", RegexOptions.Compiled);

    /// <summary>
    /// 脱敏对象中的敏感字段
    /// </summary>
    public static string MaskSensitiveData(object? data)
    {
        if (data == null)
            return "null";

        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var document = JsonDocument.Parse(json);
            var maskedElement = MaskElement(document.RootElement);
            return JsonSerializer.Serialize(maskedElement);
        }
        catch
        {
            // 如果无法序列化，返回类型名称
            return $"[{data.GetType().Name}]";
        }
    }

    /// <summary>
    /// 脱敏字符串中的敏感信息
    /// </summary>
    public static string MaskSensitiveString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        // 脱敏JWT令牌
        input = JwtPattern.Replace(input, "Bearer [MASKED_TOKEN]");

        // 脱敏长Base64字符串（可能是密钥）
        input = Base64Pattern.Replace(input, match =>
        {
            if (match.Length > 20)
                return "[MASKED_KEY]";
            return match.Value;
        });

        // 脱敏密码字段（Password=xxx 格式）
        input = Regex.Replace(input, @"(?i)(password|pwd|pass|passwd)\s*=\s*[^\s;,]+", "$1=[MASKED]", RegexOptions.IgnoreCase);

        return input;
    }

    private static object? MaskElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    if (IsSensitiveField(property.Name))
                    {
                        dict[property.Name] = "[MASKED]";
                    }
                    else
                    {
                        dict[property.Name] = MaskElement(property.Value);
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(MaskElement(item));
                }
                return list;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                return MaskSensitiveString(stringValue);

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    private static bool IsSensitiveField(string fieldName)
    {
        return SensitiveFields.Any(sensitive =>
            fieldName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 创建脱敏的异常消息
    /// </summary>
    public static string CreateSafeExceptionMessage(Exception ex)
    {
        var message = ex.Message;

        // 脱敏异常消息中的敏感信息
        message = MaskSensitiveString(message);

        // 如果有内部异常，递归处理
        if (ex.InnerException != null)
        {
            message += $" -> {CreateSafeExceptionMessage(ex.InnerException)}";
        }

        return message;
    }
}
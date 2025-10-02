using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LYBT.Core.Infrastructure.Utilities;

/// <summary>
/// 日志脱敏工具类 - 生产级安全加固
/// 负责对日志中的敏感信息进行脱敏处理
/// </summary>
public static class LogSanitizer
{
    private static readonly string[] SensitiveFields =
    {
        "Password", "NewPassword", "OldPassword", "CurrentPassword",
        "Token", "AccessToken", "RefreshToken", "BearerToken",
        "Secret", "SecretKey", "ApiKey", "ClientSecret",
        "ConnectionString", "ConnStr", "DatabaseConnection",
        "Authorization", "AuthToken", "SessionId",
        "CreditCard", "CardNumber", "CVV", "SSN",
        "Email", "PhoneNumber", "Phone", "Mobile",
        "PrivateKey", "PublicKey", "Certificate",
        "Salt", "Hash", "Nonce", "IV"
    };

    private static readonly Regex PasswordPattern = new Regex(
        @"(?i)(password|pwd|pass|passwd|secret|token|key|authorization|bearer)\s*[:=]\s*[""']?([^""'\s]+)[""']?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConnectionStringPattern = new Regex(
        @"(?i)(server|data source|user id|password|pwd|initial catalog|database)=[^;]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BearerTokenPattern = new Regex(
        @"(?i)bearer\s+[\w\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 对对象进行脱敏序列化
    /// </summary>
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
            return SanitizeString(json);
        }
        catch
        {
            return "[Serialization Error - Sanitized]";
        }
    }

    /// <summary>
    /// 对字符串进行脱敏处理
    /// </summary>
    public static string SanitizeString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var sanitized = input;

        // 替换密码模式
        sanitized = PasswordPattern.Replace(sanitized, match =>
        {
            var field = match.Groups[1].Value;
            return $"{field}=[REDACTED]";
        });

        // 替换连接字符串中的敏感信息
        sanitized = ConnectionStringPattern.Replace(sanitized, match =>
        {
            var parts = match.Value.Split('=');
            if (parts.Length >= 2)
            {
                var key = parts[0];
                if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("pwd", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("user", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{key}=[REDACTED]";
                }
            }
            return match.Value;
        });

        // 替换Bearer Token
        sanitized = BearerTokenPattern.Replace(sanitized, "Bearer [REDACTED]");

        return sanitized;
    }

    /// <summary>
    /// 检查字段名是否为敏感字段
    /// </summary>
    public static bool IsSensitiveField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return false;

        return SensitiveFields.Any(sensitive =>
            fieldName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 对异常信息进行脱敏
    /// </summary>
    public static string SanitizeException(Exception? exception)
    {
        if (exception == null)
            return string.Empty;

        var message = SanitizeString(exception.Message);

        // 限制堆栈跟踪长度
        var stackTrace = exception.StackTrace;
        if (!string.IsNullOrEmpty(stackTrace))
        {
            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 5)
            {
                stackTrace = string.Join(Environment.NewLine, lines.Take(5)) +
                            Environment.NewLine + "[... truncated ...]";
            }
        }

        return $"{exception.GetType().Name}: {message}{Environment.NewLine}{stackTrace}";
    }

    /// <summary>
    /// 自定义JSON转换器，用于脱敏序列化
    /// </summary>
    private class SanitizingJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("This converter is only for writing");
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
                else if (IsSensitiveField(propertyName))
                {
                    writer.WriteStringValue("[REDACTED]");
                }
                else if (property.PropertyType == typeof(string))
                {
                    var stringValue = propertyValue.ToString();
                    if (IsSensitiveContent(stringValue))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        writer.WriteStringValue(SanitizeString(stringValue));
                    }
                }
                else
                {
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }

        private static bool IsSensitiveContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            // 检查是否像Token (长字符串包含特殊字符)
            if (content.Length > 40 && content.Contains('.'))
                return true;

            // 检查是否像密码 (包含特殊字符和数字的组合)
            if (content.Length >= 8 &&
                Regex.IsMatch(content, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])"))
                return true;

            return false;
        }
    }
}

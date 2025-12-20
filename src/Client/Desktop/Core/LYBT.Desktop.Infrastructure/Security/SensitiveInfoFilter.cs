using System.Text.RegularExpressions;

namespace LYBT.Desktop.Infrastructure.Security;

/// <summary>
/// 敏感信息过滤器
/// ERR-012: 异常消息安全化 - 防止敏感信息泄露到用户界面
/// </summary>
public static class SensitiveInfoFilter
{
    private const string RedactedText = "[已过滤]";

    /// <summary>
    /// 敏感信息匹配模式
    /// </summary>
    private static readonly Regex[] SensitivePatterns =
    {
        // 数据库连接字符串
        new(@"(Server|Data Source|Initial Catalog|User Id|Password|Integrated Security)\s*=\s*[^;]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // SQL语句
        new(@"\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|TRUNCATE)\b.*?\b(FROM|INTO|TABLE|WHERE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // 文件路径（Windows）
        new(@"[A-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // 文件路径（Unix）
        new(@"\/(?:[^\/\0]+\/)+[^\/\0]*",
            RegexOptions.Compiled),

        // 堆栈跟踪
        new(@"at\s+[\w.]+\.<\w+>[\w.]*\(.*?\)\s*(in\s+.*?:line\s+\d+)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // 内部服务地址
        new(@"(https?|ftp):\/\/(?:localhost|127\.0\.0\.1|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[01])\.\d+\.\d+)(?::\d+)?(?:\/[^\s]*)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // JWT令牌
        new(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+",
            RegexOptions.Compiled),

        // API密钥格式
        new(@"(api[_-]?key|apikey|access[_-]?token|secret[_-]?key)\s*[=:]\s*[""']?[\w-]+[""']?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // 邮箱地址（可能包含敏感信息）
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
            RegexOptions.Compiled),

        // 身份证号
        new(@"\b\d{17}[\dXx]\b",
            RegexOptions.Compiled),

        // 电话号码
        new(@"\b1[3-9]\d{9}\b",
            RegexOptions.Compiled)
    };

    /// <summary>
    /// 过滤异常消息中的敏感信息
    /// </summary>
    /// <param name="message">原始消息</param>
    /// <returns>过滤后的安全消息</returns>
    public static string FilterSensitiveInfo(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var result = message;
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, RedactedText);
        }

        return result;
    }

    /// <summary>
    /// 检查消息是否包含敏感信息
    /// </summary>
    /// <param name="message">要检查的消息</param>
    /// <returns>是否包含敏感信息</returns>
    public static bool ContainsSensitiveInfo(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        foreach (var pattern in SensitivePatterns)
        {
            if (pattern.IsMatch(message))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取安全的用户消息
    /// 如果原始消息包含敏感信息，返回默认消息
    /// </summary>
    /// <param name="message">原始消息</param>
    /// <param name="defaultMessage">默认安全消息</param>
    /// <returns>安全消息</returns>
    public static string GetSafeMessage(string? message, string defaultMessage = "操作失败，请稍后重试")
    {
        if (string.IsNullOrEmpty(message))
        {
            return defaultMessage;
        }

        // 如果消息包含敏感信息，返回默认消息而不是过滤后的消息
        // 这样可以避免暴露"[已过滤]"文本给用户
        return ContainsSensitiveInfo(message) ? defaultMessage : message;
    }

    /// <summary>
    /// 从异常获取安全消息（用于日志记录保留原始信息）
    /// </summary>
    /// <param name="ex">异常</param>
    /// <returns>包含过滤信息的元组：(用户显示消息, 日志记录消息)</returns>
    public static (string UserMessage, string LogMessage) GetSafeExceptionMessages(Exception ex)
    {
        var originalMessage = ex.Message;

        // 日志中使用过滤后的消息（保留结构但隐藏敏感值）
        var logMessage = FilterSensitiveInfo(originalMessage);

        // 用户显示使用安全消息
        var userMessage = ContainsSensitiveInfo(originalMessage)
            ? "操作失败，请稍后重试"
            : originalMessage;

        return (userMessage, logMessage);
    }
}

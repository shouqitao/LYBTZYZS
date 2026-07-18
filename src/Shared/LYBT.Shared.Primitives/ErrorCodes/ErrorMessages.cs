namespace LYBT.Shared.Primitives.ErrorCodes;

/// <summary>
/// 错误消息映射 - 提供中英文错误消息
/// consolidate-exception-handling: 统一错误消息管理
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<ErrorCode, (string Zh, string En)> Messages = new()
    {
        // ... existing messages ...
    };

    public static string Get(ErrorCode code, bool useEnglish = false)
    {
        if (Messages.TryGetValue(code, out var msg))
            return useEnglish ? msg.En : msg.Zh;
        return code.ToString();
    }

    public static string GetUserMessage(ErrorCode code) => Get(code, useEnglish: false);
}

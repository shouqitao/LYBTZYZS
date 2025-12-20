using System.Net;

namespace LYBT.Shared.ExceptionHandling.Mappers;

/// <summary>
/// 异常消息映射器 - 将技术异常转换为用户友好的中文消息
/// consolidate-exception-handling: 从LYBT.Desktop.Foundation迁移
/// </summary>
public static class ExceptionMessageMapper
{
    /// <summary>
    /// 异常类型到用户友好消息的映射表
    /// </summary>
    private static readonly Dictionary<Type, string> ExceptionMessages = new()
    {
        { typeof(HttpRequestException), "网络连接失败，请检查网络设置" },
        { typeof(TimeoutException), "操作超时，请稍后重试" },
        { typeof(UnauthorizedAccessException), "没有权限执行此操作" },
        { typeof(ArgumentNullException), "必填信息不能为空" },
        { typeof(ArgumentException), "输入信息格式不正确" },
        { typeof(InvalidOperationException), "当前状态不允许执行此操作" },
        { typeof(NotSupportedException), "系统暂不支持此功能" },
        { typeof(FileNotFoundException), "找不到相关文件" },
        { typeof(OutOfMemoryException), "系统内存不足，请重启程序" }
    };

    /// <summary>
    /// HTTP状态码到用户友好消息的映射表
    /// </summary>
    private static readonly Dictionary<HttpStatusCode, string> HttpStatusMessages = new()
    {
        { HttpStatusCode.BadRequest, "请求数据格式不正确" },
        { HttpStatusCode.Unauthorized, "身份验证失败，请重新登录" },
        { HttpStatusCode.Forbidden, "没有权限执行此操作" },
        { HttpStatusCode.NotFound, "请求的资源不存在" },
        { HttpStatusCode.Conflict, "数据冲突，可能已被其他用户修改" },
        { HttpStatusCode.InternalServerError, "服务器内部错误" },
        { HttpStatusCode.ServiceUnavailable, "服务暂时不可用，请稍后重试" },
        { HttpStatusCode.RequestTimeout, "请求超时，请检查网络连接" }
    };

    /// <summary>
    /// 获取用户友好的异常消息
    /// </summary>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx when TryGetHttpStatusCode(httpEx, out var statusCode)
                => GetHttpStatusMessage(statusCode),

            AggregateException aggEx when aggEx.InnerExceptions.Count == 1
                => GetUserFriendlyMessage(aggEx.InnerExceptions.First()),

            _ when ExceptionMessages.TryGetValue(exception.GetType(), out var message)
                => message,

            _ => GetDefaultMessage(exception)
        };
    }

    /// <summary>
    /// 获取HTTP状态码对应的用户友好消息
    /// </summary>
    public static string GetHttpStatusMessage(HttpStatusCode statusCode)
    {
        return HttpStatusMessages.TryGetValue(statusCode, out var message)
            ? message
            : "网络请求失败，请稍后重试";
    }

    /// <summary>
    /// 尝试从HttpRequestException中提取HTTP状态码
    /// </summary>
    private static bool TryGetHttpStatusCode(HttpRequestException httpException, out HttpStatusCode statusCode)
    {
        statusCode = HttpStatusCode.InternalServerError;
        var message = httpException.Message;

        if (message.Contains("400")) { statusCode = HttpStatusCode.BadRequest; return true; }
        if (message.Contains("401")) { statusCode = HttpStatusCode.Unauthorized; return true; }
        if (message.Contains("403")) { statusCode = HttpStatusCode.Forbidden; return true; }
        if (message.Contains("404")) { statusCode = HttpStatusCode.NotFound; return true; }
        if (message.Contains("409")) { statusCode = HttpStatusCode.Conflict; return true; }
        if (message.Contains("500")) { statusCode = HttpStatusCode.InternalServerError; return true; }

        return false;
    }

    /// <summary>
    /// 获取默认错误消息
    /// </summary>
    private static string GetDefaultMessage(Exception exception)
    {
#if DEBUG
        return $"操作失败: {exception.GetType().Name} - {exception.Message}";
#else
        return "操作失败，请稍后重试";
#endif
    }
}

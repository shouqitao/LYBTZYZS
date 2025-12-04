using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using LYBT.Desktop.Infrastructure.Http;

namespace LYBT.Desktop.Infrastructure.Localization;

/// <summary>
/// 客户端错误消息映射器
/// refactor-logging-system: 提供统一的用户友好错误消息
/// </summary>
public static class ClientErrorMessageMapper
{
    /// <summary>
    /// HTTP状态码到用户消息的映射
    /// </summary>
    private static readonly Dictionary<HttpStatusCode, string> HttpStatusMessages = new()
    {
        [HttpStatusCode.BadRequest] = "请求参数无效，请检查输入",
        [HttpStatusCode.Unauthorized] = "登录已过期，请重新登录",
        [HttpStatusCode.Forbidden] = "您没有权限执行此操作",
        [HttpStatusCode.NotFound] = "请求的资源不存在",
        [HttpStatusCode.Conflict] = "数据已被其他用户修改，请刷新后重试",
        [HttpStatusCode.RequestTimeout] = "请求超时，请稍后重试",
        [HttpStatusCode.InternalServerError] = "服务器处理异常，请稍后重试",
        [HttpStatusCode.BadGateway] = "服务暂时不可用，请稍后重试",
        [HttpStatusCode.ServiceUnavailable] = "服务正在维护中，请稍后重试",
        [HttpStatusCode.GatewayTimeout] = "服务器响应超时，请稍后重试"
    };

    /// <summary>
    /// 错误码前缀到用户消息的映射
    /// </summary>
    private static readonly Dictionary<string, string> ErrorCodePrefixMessages = new()
    {
        ["ERR-00"] = "系统错误",
        ["ERR-01"] = "用户相关错误",
        ["ERR-02"] = "患者相关错误",
        ["ERR-03"] = "病历相关错误",
        ["ERR-04"] = "处方相关错误",
        ["ERR-05"] = "药材相关错误",
        ["ERR-06"] = "方剂相关错误",
        ["ERR-07"] = "诊断相关错误"
    };

    /// <summary>
    /// 从异常获取用户友好消息
    /// </summary>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => GetHttpExceptionMessage(httpEx),
            TaskCanceledException => "操作被取消",
            TimeoutException => "操作超时，请稍后重试",
            SocketException => "网络连接失败，请检查网络设置",
            OperationCanceledException => "操作已取消",
            UnauthorizedAccessException => "访问被拒绝",
            ArgumentNullException => "缺少必要的参数",
            ArgumentException => "参数无效",
            InvalidOperationException => "当前状态下无法执行此操作",
            FormatException => "数据格式不正确",
            _ => "操作失败，请稍后重试"
        };
    }

    /// <summary>
    /// 从HTTP状态码获取用户消息
    /// </summary>
    public static string GetUserMessageFromStatusCode(HttpStatusCode statusCode)
    {
        return HttpStatusMessages.TryGetValue(statusCode, out var message)
            ? message
            : $"服务器返回错误 ({(int)statusCode})";
    }

    /// <summary>
    /// 从HTTP状态码获取用户消息
    /// </summary>
    public static string GetUserMessageFromStatusCode(int statusCode)
    {
        return GetUserMessageFromStatusCode((HttpStatusCode)statusCode);
    }

    /// <summary>
    /// 从ProblemDetails获取用户消息
    /// </summary>
    public static string GetUserMessageFromProblemDetails(ProblemDetailsResponse problemDetails)
    {
        // 优先使用服务器返回的详细消息
        if (!string.IsNullOrEmpty(problemDetails.Detail))
        {
            return problemDetails.Detail;
        }

        // 如果有验证错误，格式化显示
        if (problemDetails.IsValidationError)
        {
            return problemDetails.GetValidationErrorMessage() ?? "输入数据验证失败";
        }

        // 根据错误码获取消息
        if (!string.IsNullOrEmpty(problemDetails.ErrorCode))
        {
            var prefix = GetErrorCodePrefix(problemDetails.ErrorCode);
            if (ErrorCodePrefixMessages.TryGetValue(prefix, out var prefixMessage))
            {
                return problemDetails.Title ?? prefixMessage;
            }
        }

        // 根据状态码获取消息
        if (problemDetails.Status.HasValue)
        {
            return GetUserMessageFromStatusCode(problemDetails.Status.Value);
        }

        return problemDetails.Title ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 从错误码获取用户消息
    /// </summary>
    public static string GetUserMessageFromErrorCode(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
        {
            return "操作失败";
        }

        var prefix = GetErrorCodePrefix(errorCode);
        return ErrorCodePrefixMessages.TryGetValue(prefix, out var message)
            ? message
            : "操作失败";
    }

    /// <summary>
    /// 获取HTTP异常消息
    /// </summary>
    private static string GetHttpExceptionMessage(HttpRequestException exception)
    {
        // 检查是否有状态码
        if (exception.StatusCode.HasValue)
        {
            return GetUserMessageFromStatusCode(exception.StatusCode.Value);
        }

        // 检查内部异常
        if (exception.InnerException is SocketException)
        {
            return "无法连接到服务器，请检查网络连接";
        }

        return "网络请求失败，请稍后重试";
    }

    /// <summary>
    /// 获取错误码前缀（前6个字符）
    /// </summary>
    private static string GetErrorCodePrefix(string errorCode)
    {
        return errorCode.Length >= 6 ? errorCode[..6] : errorCode;
    }
}

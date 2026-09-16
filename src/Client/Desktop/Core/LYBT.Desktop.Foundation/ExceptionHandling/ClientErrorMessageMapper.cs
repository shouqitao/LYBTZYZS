using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Desktop.Foundation.ExceptionHandling;

/// <summary>
/// 客户端错误消息映射器
/// 提供统一的用户友好错误消息
/// optimize-desktop-core: 从Infrastructure.Localization迁移到共享异常处理模块
/// </summary>
public static class ClientErrorMessageMapper
{
    /// <summary>
    /// 默认错误消息 - 用于系统异常或未知错误
    /// </summary>
    public const string DefaultErrorMessage = "操作失败，请稍后重试";

    #region HTTP状态码映射

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

    #endregion

    #region 错误码映射

    /// <summary>
    /// 错误码前缀到用户消息的映射
    /// </summary>
    private static readonly Dictionary<string, string> ErrorCodePrefixMessages = new()
    {
        ["ERR-00"] = "系统错误",
        ["ERR-01"] = "用户相关错误",
        ["ERR-02"] = "患者相关错误",
        ["ERR-03"] = "医案相关错误",
        ["ERR-04"] = "处方相关错误",
        ["ERR-05"] = "药材相关错误",
        ["ERR-06"] = "方剂相关错误",
        ["ERR-07"] = "同步相关错误"
    };

    // refactor-shared-library: ErrorCode 消息映射已统一委托到 ErrorMessages (Primitives 层)
    // 消除原 274 条重复映射，单一数据源维护

    /// <summary>
    /// 从错误码获取用户消息
    /// </summary>
    public static string GetUserMessageFromErrorCode(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
        {
            return DefaultErrorMessage;
        }

        if (int.TryParse(errorCode, out var code))
        {
            return GetUserMessageFromErrorCode(code);
        }

        var prefix = GetErrorCodePrefix(errorCode);
        return ErrorCodePrefixMessages.TryGetValue(prefix, out var message)
            ? message
            : DefaultErrorMessage;
    }

    /// <summary>
    /// 从ErrorCode枚举值获取用户消息
    /// refactor-shared-library: 委托到 ErrorMessages 单一数据源
    /// </summary>
    public static string GetUserMessageFromErrorCode(int errorCode)
    {
        var code = (ErrorCode)errorCode;
        if (Enum.IsDefined(code))
        {
            return ErrorMessages.GetUserMessage(code);
        }

        return DefaultErrorMessage;
    }

    /// <summary>
    /// 获取错误码前缀（前6个字符）
    /// </summary>
    private static string GetErrorCodePrefix(string errorCode)
    {
        return errorCode.Length >= 6 ? errorCode[..6] : errorCode;
    }

    #endregion

    #region 异常消息映射

    /// <summary>
    /// 从异常获取用户友好消息
    /// </summary>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            // 优先处理AppException及其子类（如ApiException），使用其UserMessage
            AppException appEx => GetAppExceptionMessage(appEx),
            // 领域错误层统一：本地与远程非 2xx 均抛 ApiClientException（优先服务端消息）
            ApiClientException apiEx => GetApiClientExceptionMessage(apiEx),
            HttpRequestException httpEx => GetHttpExceptionMessage(httpEx),
            // HttpClient.Timeout 触发的 TaskCanceledException（InnerException 为 TimeoutException）
            // 必须排在 TaskCanceledException 之前，否则超时被误报为「操作被取消」。
            TaskCanceledException tce when tce.InnerException is TimeoutException => "操作超时，请稍后重试",
            TaskCanceledException => "操作被取消",
            TimeoutException => "操作超时，请稍后重试",
            SocketException => "网络连接失败，请检查网络设置",
            OperationCanceledException => "操作已取消",
            UnauthorizedAccessException => "访问被拒绝",
            ArgumentNullException => "缺少必要的参数",
            ArgumentException argEx => GetArgumentExceptionMessage(argEx),
            InvalidOperationException invOpEx => GetInvalidOperationExceptionMessage(invOpEx),
            FormatException => "数据格式不正确",
            _ => DefaultErrorMessage
        };
    }

    /// <summary>
    /// 获取 <see cref="ApiClientException"/> 的用户消息。
    /// 优先使用服务端返回的面向用户消息（信封 <c>message</c>）；无则按 HTTP 状态码映射。
    /// </summary>
    private static string GetApiClientExceptionMessage(ApiClientException exception)
    {
        if (!string.IsNullOrWhiteSpace(exception.ServerMessage))
        {
            return exception.ServerMessage!;
        }

        return exception.StatusCode is { } statusCode
            ? GetUserMessageFromStatusCode(statusCode)
            : DefaultErrorMessage;
    }

    /// <summary>
    /// 获取AppException消息
    /// 优先返回UserMessage，如果为空则返回Message
    /// </summary>
    private static string GetAppExceptionMessage(AppException exception)
    {
        if (!string.IsNullOrWhiteSpace(exception.UserMessage))
        {
            return exception.UserMessage;
        }

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            return exception.Message;
        }

        return DefaultErrorMessage;
    }

    /// <summary>
    /// 获取InvalidOperationException消息
    /// 优先返回异常中的具体消息（如服务器返回的业务错误）
    /// </summary>
    private static string GetInvalidOperationExceptionMessage(InvalidOperationException exception)
    {
        // 如果异常包含具体业务消息，直接返回
        if (!string.IsNullOrWhiteSpace(exception.Message) &&
            exception.Message != "Operation is not valid due to the current state of the object.")
        {
            return exception.Message;
        }

        return "当前状态下无法执行此操作";
    }

    /// <summary>
    /// 获取ArgumentException消息
    /// </summary>
    private static string GetArgumentExceptionMessage(ArgumentException exception)
    {
        // 如果异常包含具体消息，返回（去除参数名部分）
        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            // 移除 "(Parameter 'xxx')" 后缀
            var message = exception.Message;
            var paramIndex = message.LastIndexOf(" (Parameter '", StringComparison.Ordinal);
            if (paramIndex > 0)
            {
                message = message[..paramIndex];
            }
            return message;
        }

        return "参数无效";
    }

    /// <summary>
    /// 获取HTTP异常消息
    /// </summary>
    private static string GetHttpExceptionMessage(HttpRequestException exception)
    {
        if (exception.StatusCode.HasValue)
        {
            return GetUserMessageFromStatusCode(exception.StatusCode.Value);
        }

        if (exception.InnerException is SocketException)
        {
            return "无法连接到服务器，请检查网络连接";
        }

        return "网络请求失败，请稍后重试";
    }

    #endregion



    #region 安全消息

    /// <summary>
    /// 获取安全的操作失败消息（带操作名称）
    /// </summary>
    public static string GetSafeOperationFailureMessage(string operationName, Exception exception)
    {
        var friendlyMessage = GetUserFriendlyMessage(exception);

        if (friendlyMessage == DefaultErrorMessage)
        {
            return $"{operationName}失败，请稍后重试";
        }

        return $"{operationName}失败：{friendlyMessage}";
    }

    /// <summary>
    /// 获取安全的操作失败消息（简化版）
    /// </summary>
    public static string GetSafeOperationFailureMessage(string operationName)
    {
        return $"{operationName}失败，请稍后重试";
    }

    #endregion

    #region 追踪码支持

    /// <summary>
    /// 设置追踪ID提供器（由客户端在启动时配置）
    /// </summary>
    public static Func<string>? TraceIdProvider { get; set; }

    /// <summary>
    /// 获取短追踪码（TraceId的前8位）
    /// </summary>
    public static string GetShortTrackingCode()
    {
        var traceId = TraceIdProvider?.Invoke() ?? Guid.NewGuid().ToString("N");
        return traceId.Length >= 8 ? traceId[..8].ToUpperInvariant() : traceId.ToUpperInvariant();
    }

    #endregion
}

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using LYBT.Shared.ExceptionHandling.ProblemDetails;

namespace LYBT.Shared.ExceptionHandling.Mappers;

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
        ["ERR-03"] = "病历相关错误",
        ["ERR-04"] = "处方相关错误",
        ["ERR-05"] = "药材相关错误",
        ["ERR-06"] = "方剂相关错误",
        ["ERR-07"] = "诊断相关错误"
    };

    /// <summary>
    /// ErrorCode到用户消息的详细映射
    /// </summary>
    private static readonly Dictionary<int, string> ErrorCodeMessages = new()
    {
        // 0xxxx - 通用错误
        [0] = "操作失败，请稍后重试",
        [1] = "请求参数无效，请检查输入",
        [2] = "请求的资源不存在",
        [3] = "输入数据验证失败",
        [4] = "登录已过期，请重新登录",
        [5] = "您没有权限执行此操作",
        [6] = "数据已被其他用户修改，请刷新后重试",
        [7] = "操作超时，请稍后重试",
        [8] = "服务暂时不可用，请稍后重试",
        [9] = "服务器处理异常，请稍后重试",
        [10] = "数据保存失败，请稍后重试",
        [11] = "系统配置错误，请联系管理员",
        [12] = "请求过于频繁，请稍后重试",

        // 1xxxx - 用户模块
        [10001] = "用户不存在",
        [10002] = "用户名已被使用",
        [10003] = "邮箱已被使用",
        [10004] = "密码不正确",
        [10005] = "密码不符合安全要求",
        [10006] = "用户已被禁用",
        [10007] = "账户已被锁定，请联系管理员",
        [10008] = "登录凭证已过期，请重新登录",
        [10009] = "登录状态已失效，请重新登录",
        [10010] = "角色不存在",
        [10011] = "无法删除系统管理员账户",
        [10012] = "首次登录需要修改密码",

        // 2xxxx - 患者模块
        [20001] = "患者信息不存在",
        [20002] = "该身份证号已被使用",
        [20003] = "该电话号码已被使用",
        [20004] = "患者有未完成的就诊记录，无法删除",
        [20005] = "患者已被禁用",
        [20006] = "患者状态无效",

        // 3xxxx - 病历模块
        [30001] = "病历不存在",
        [30002] = "当前病历状态不允许此操作",
        [30003] = "病历已归档，无法修改",
        [30004] = "病历正在被其他用户编辑",
        [30005] = "病历数据已被修改，请刷新后重试",
        [30006] = "该患者已存在相同日期的病历",
        [30007] = "病历缺少必要的诊断信息",
        [30008] = "病历有关联处方，无法删除",

        // 4xxxx - 处方模块
        [40001] = "处方不存在",
        [40002] = "当前处方状态不允许此操作",
        [40003] = "处方已发药，无法修改",
        [40004] = "处方中没有药材，请添加药材",
        [40005] = "药材剂量超出安全范围",
        [40006] = "处方中存在配伍禁忌",
        [40007] = "处方已完成，无法修改",

        // 5xxxx - 药材模块
        [50001] = "药材不存在",
        [50002] = "药材名称已存在",
        [50003] = "药材库存不足",
        [50004] = "药材已被禁用",
        [50005] = "药材已被使用，无法删除",
        [50006] = "药材价格无效",

        // 6xxxx - 方剂模块
        [60001] = "方剂不存在",
        [60002] = "方剂名称已存在",
        [60003] = "方剂中没有药材，请添加药材",
        [60004] = "方剂验证失败",
        [60005] = "方剂已被使用，无法删除",
        [60006] = "方剂已被禁用",

        // 7xxxx - 问诊模块
        [70001] = "问诊记录不存在",
        [70002] = "当前问诊状态不允许此操作",
        [70003] = "问诊已完成，无法修改",
        [70004] = "问诊数据不完整",
        [70005] = "请填写症状描述"
    };

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
    /// </summary>
    public static string GetUserMessageFromErrorCode(int errorCode)
    {
        return ErrorCodeMessages.TryGetValue(errorCode, out var message)
            ? message
            : DefaultErrorMessage;
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
            _ => DefaultErrorMessage
        };
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

    #region ProblemDetails解析

    /// <summary>
    /// 从ClientProblemDetails获取用户消息
    /// </summary>
    public static string GetUserMessageFromProblemDetails(ClientProblemDetails problemDetails)
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

        return problemDetails.Title ?? DefaultErrorMessage;
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
    /// 获取带追踪码的安全操作失败消息
    /// </summary>
    public static string GetSafeMessageWithTrackingCode(string operationName, Exception exception, bool includeTrackingCode = true)
    {
        var baseMessage = GetSafeOperationFailureMessage(operationName, exception);

        if (!includeTrackingCode)
        {
            return baseMessage;
        }

        var trackingCode = GetShortTrackingCode();
        return $"{baseMessage}\n\n如需帮助，请提供追踪码: {trackingCode}";
    }

    /// <summary>
    /// 获取带追踪码的通用错误消息
    /// </summary>
    public static string GetMessageWithTrackingCode(string message, bool includeTrackingCode = true)
    {
        if (!includeTrackingCode)
        {
            return message;
        }

        var trackingCode = GetShortTrackingCode();
        return $"{message}\n\n如需帮助，请提供追踪码: {trackingCode}";
    }

    /// <summary>
    /// 获取短追踪码（TraceId的前8位）
    /// </summary>
    public static string GetShortTrackingCode()
    {
        var traceId = TraceIdProvider?.Invoke() ?? Guid.NewGuid().ToString("N");
        return traceId.Length >= 8 ? traceId[..8].ToUpperInvariant() : traceId.ToUpperInvariant();
    }

    /// <summary>
    /// 获取完整追踪码
    /// </summary>
    public static string GetFullTrackingCode()
    {
        return TraceIdProvider?.Invoke() ?? Guid.NewGuid().ToString("N");
    }

    #endregion
}

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using LYBT.Shared.ExceptionHandling.Exceptions;
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
        ["ERR-03"] = "医案相关错误",
        ["ERR-04"] = "处方相关错误",
        ["ERR-05"] = "药材相关错误",
        ["ERR-06"] = "方剂相关错误",
        ["ERR-07"] = "同步相关错误"
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
        [10013] = "Access Token 已过期，请刷新令牌",
        [10014] = "登录设备不匹配",
        [10015] = "会话已到期，请重新登录",
        // Auth MCCEE 码
        [10101] = "用户名或密码错误",
        [10202] = "登录凭据无效",
        [10203] = "登录已失效，请重新登录",
        [10204] = "会话已过期，请重新登录",
        [10205] = "刷新凭据无效",
        [10303] = "登录设备数超过限制",

        // 2xxxx - 患者模块
        [20001] = "患者信息不存在",
        [20002] = "该身份证号已被使用",
        [20003] = "该电话号码已被使用",
        [20004] = "患者有关联的医案，无法删除",
        [20005] = "患者已被禁用",
        [20006] = "患者状态无效",
        // 207xx: 业务规则错误
        [20701] = "手机号已存在",
        [20702] = "该患者未被删除，无需恢复",
        [20703] = "请至少选择一个患者",
        [20704] = "批量检查最多支持100条记录",
        [20705] = "页码和页大小参数无效",
        // 208xx: 导入错误
        [20801] = "文件不能为空",
        [20802] = "仅支持.xlsx格式的Excel文件",
        [20803] = "文件大小不能超过10MB",
        [20804] = "Excel文件中没有工作表",
        [20805] = "导入数据超过限制",

        // 3xxxx - 医案模块
        [30001] = "医案不存在",
        [30002] = "当前医案状态不允许此操作",
        [30003] = "医案已归档，无法修改",
        [30004] = "医案正在被其他用户编辑",
        [30005] = "医案数据已被修改，请刷新后重试",
        [30006] = "该患者已存在相同日期的医案",
        [30007] = "医案缺少必要的诊断信息",
        [30008] = "医案有关联处方，无法删除",
        // 301xx: 创建医案错误
        [30101] = "患者不存在",
        [30102] = "医生不存在",
        [30103] = "该患者已有进行中的医案",
        [30104] = "该患者已有挂起的医案",
        [30105] = "该患者已被禁用，无法创建医案",
        // 302xx: 权限错误
        [30201] = "无权限编辑此医案",
        [30202] = "无权限删除此医案",
        [30203] = "无权限取消此医案",
        [30204] = "无权限删除此医案的处方",
        [30205] = "无权限挂起此医案",
        // 303xx: 状态转换错误
        [30301] = "不允许的状态转换",
        [30302] = "请先标记是否需要开处方",
        [30303] = "已标记需要开处方，但处方不存在",
        [30304] = "已完成的医案不可挂起",
        [30305] = "已删除的医案不可挂起",
        [30306] = "已完成的医案不可取消",
        [30307] = "医案已经是删除状态",
        // 304xx: 处方错误
        [30401] = "未标记需要开处方",
        [30402] = "医案已存在处方",
        [30403] = "医案已打印，修改需要提供原因",
        [30404] = "医案已打印，不允许删除处方",
        [30405] = "诊断记录不存在",
        // 305xx: 并发和系统错误
        [30501] = "创建处方失败，请稍后重试",
        [30502] = "保存失败，请稍后重试",
        // 306xx: 参数验证错误
        [30601] = "请求ID与路由ID不匹配",
        [30602] = "页码和页大小参数无效",
        [30603] = "单次最多查询50个医案",
        [30604] = "请至少选择一个医案",
        [30605] = "患者ID无效",
        [30606] = "返回数量参数无效",
        [30607] = "医案不存在",

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
        [50102] = "药材验证失败",
        [50103] = "您没有权限操作此药材",
        [50104] = "该药材未被删除，无需恢复",
        [50106] = "页码和页大小参数无效",
        [50201] = "请至少选择一个药材",
        [50202] = "批量导入最多支持10000条记录",
        [50203] = "批量检查最多支持100条记录",
        [50204] = "药材不存在",
        [50205] = "药材不存在或已删除",
        [50206] = "操作失败",
        [50301] = "文件不能为空",
        [50302] = "仅支持.xlsx格式的Excel文件",
        [50303] = "文件大小不能超过10MB",
        [50304] = "Excel文件格式错误",
        [50305] = "Excel文件中没有数据行",

        // 6xxxx - 方剂模块
        [60001] = "方剂不存在",
        [60002] = "方剂名称已存在",
        [60003] = "方剂中没有药材，请添加药材",
        [60004] = "方剂验证失败",
        [60005] = "方剂已被使用，无法删除",
        [60006] = "方剂已被禁用",
        // 601xx: 核心错误
        [60102] = "验方ID不能为空",
        [60103] = "您没有权限操作此验方",
        [60104] = "新增验方失败",
        [60105] = "更新验方失败",
        [60106] = "验方不存在",
        [60107] = "该验方未被删除，无需恢复",
        [60108] = "页码和页大小参数无效",
        // 602xx: 药材验证错误
        [60201] = "参数不能为空",
        [60202] = "药材项不存在",
        [60203] = "该药材已校验，无需重复操作",
        [60204] = "所选药材不存在",
        [60205] = "获取待校验验方列表失败",
        // 603xx: 批量操作错误
        [60301] = "请至少选择一个方剂",
        [60302] = "导入数据不能为空",
        [60303] = "方剂不存在",
        [60304] = "操作失败",

        // 7xxxx - 同步模块
        // 701xx: 服务端通用错误
        [70101] = "不支持的实体类型",
        [70102] = "JSON 反序列化失败",
        [70103] = "服务器已存在该数据",
        // 702xx: 上传错误
        [70201] = "药材上传失败",
        [70202] = "患者上传失败",
        [70203] = "验方上传失败",
        [70204] = "医案上传失败",
        // 703xx: MedicalCase 同步错误
        [70301] = "患者不存在，请先同步患者",
        [70302] = "药材不存在，请先同步药材",
        [70304] = "医案已完成且已锁定，无法通过同步覆盖",
        // 704xx: 删除错误
        [70401] = "无法检查引用关系",
        [70402] = "药材被处方引用，请先禁用",
        [70403] = "患者有医案记录，请先禁用",
        [70404] = "实体不存在或已删除",
        // 705xx: 客户端错误
        [70501] = "请选择要同步的数据类型",
        [70502] = "同步失败",
        [70503] = "不支持的 Checksum 实体类型",
        [70504] = "请先同步药材和患者数据",
        [70505] = "无法匹配患者，请手动处理",
        [70506] = "本地有未完成的医案，请先完成或取消后再切换模式"
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
            // 优先处理AppException及其子类（如ApiException），使用其UserMessage
            AppException appEx => GetAppExceptionMessage(appEx),
            HttpRequestException httpEx => GetHttpExceptionMessage(httpEx),
            TaskCanceledException => "操作被取消",
            TimeoutException => "操作超时，请稍后重试",
            SocketException => "网络连接失败，请检查网络设置",
            OperationCanceledException => "操作已取消",
            UnauthorizedAccessException => "访问被拒绝",
            ArgumentNullException => "缺少必要的参数",
            ArgumentException argEx => GetArgumentExceptionMessage(argEx),
            InvalidOperationException invOpEx => GetInvalidOperationExceptionMessage(invOpEx),
            FormatException => "数据格式不正确",
            // 检查是否为Refit.ApiException（通过类型名匹配，避免直接引用Refit包）
            _ when exception.GetType().FullName == "Refit.ApiException" => GetRefitApiExceptionMessage(exception),
            _ => DefaultErrorMessage
        };
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
    /// 从Refit.ApiException中提取错误消息
    /// 通过反射获取Content属性并解析服务器返回的错误信息
    /// </summary>
    private static string GetRefitApiExceptionMessage(Exception exception)
    {
        try
        {
            // 尝试获取StatusCode属性
            var statusCodeProp = exception.GetType().GetProperty("StatusCode");
            if (statusCodeProp != null)
            {
                var statusCode = (HttpStatusCode?)statusCodeProp.GetValue(exception);
                if (statusCode.HasValue)
                {
                    // 尝试获取Content属性以提取服务器返回的具体错误消息
                    var contentProp = exception.GetType().GetProperty("Content");
                    if (contentProp != null)
                    {
                        var content = contentProp.GetValue(exception) as string;
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var extractedMessage = ExtractMessageFromApiResponse(content);
                            if (!string.IsNullOrWhiteSpace(extractedMessage))
                            {
                                return extractedMessage;
                            }
                        }
                    }

                    // 如果无法从Content提取消息，使用状态码映射
                    return GetUserMessageFromStatusCode(statusCode.Value);
                }
            }
        }
        catch
        {
            // 反射失败时忽略，返回默认消息
        }

        return DefaultErrorMessage;
    }

    /// <summary>
    /// 从API响应内容中提取错误消息
    /// 支持ApiResponse和ValidationProblemDetails格式
    /// </summary>
    private static string? ExtractMessageFromApiResponse(string content)
    {
        try
        {
            // 使用JsonDocument解析响应内容
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // 检查message字段
            if (root.TryGetProperty("message", out var messageProp) &&
                messageProp.ValueKind == JsonValueKind.String)
            {
                var message = messageProp.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }

            // 检查detail字段（ProblemDetails格式）
            if (root.TryGetProperty("detail", out var detailProp) &&
                detailProp.ValueKind == JsonValueKind.String)
            {
                var detail = detailProp.GetString();
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail;
                }
            }

            // 检查title字段（ProblemDetails格式）
            if (root.TryGetProperty("title", out var titleProp) &&
                titleProp.ValueKind == JsonValueKind.String)
            {
                var title = titleProp.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
        }
        catch (JsonException)
        {
            // JSON解析失败，忽略
        }

        return null;
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

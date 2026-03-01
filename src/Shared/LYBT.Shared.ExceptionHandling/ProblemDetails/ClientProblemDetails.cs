using System.Text.Json.Serialization;

namespace LYBT.Shared.ExceptionHandling.ProblemDetails;

/// <summary>
/// 客户端ProblemDetails响应模型
/// 用于解析服务端返回的RFC 7807标准错误响应
/// optimize-desktop-core: 从Infrastructure迁移并解耦
/// </summary>
public class ClientProblemDetails
{
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// 错误标题（简短描述）
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 错误详情（详细描述）
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// 错误类型URI
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 请求实例标识
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// 错误码（业务错误码）
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// CorrelationId用于日志关联
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// TraceId用于分布式追踪
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// 验证错误详情（字段 -> 错误消息列表）
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// 资源类型（用于NotFound错误）
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// 资源ID（用于NotFound错误）
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// 业务规则（用于业务错误）
    /// </summary>
    [JsonPropertyName("businessRule")]
    public string? BusinessRule { get; set; }

    /// <summary>
    /// 错误严重程度 (info/warning/error/critical)
    /// T5-P3-03: 接收服务端 ProblemDetails 中的 severity 字段
    /// </summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    #region 便捷属性

    /// <summary>
    /// 是否为验证错误
    /// </summary>
    public bool IsValidationError => Status == 400 && Errors?.Count > 0;

    /// <summary>
    /// 是否为未找到错误
    /// </summary>
    public bool IsNotFoundError => Status == 404;

    /// <summary>
    /// 是否为授权错误
    /// </summary>
    public bool IsUnauthorizedError => Status == 401;

    /// <summary>
    /// 是否为禁止访问错误
    /// </summary>
    public bool IsForbiddenError => Status == 403;

    /// <summary>
    /// 是否为并发冲突错误
    /// </summary>
    public bool IsConcurrencyError => Status == 409 || ErrorCode?.Contains("CONCURRENCY") == true;

    /// <summary>
    /// 是否为服务器错误
    /// </summary>
    public bool IsServerError => Status >= 500;

    /// <summary>
    /// 是否为严重错误 (critical)
    /// </summary>
    public bool IsCriticalError =>
        string.Equals(Severity, "critical", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region 便捷方法

    /// <summary>
    /// 获取用户友好的错误消息
    /// 优先使用Detail，回退到Title
    /// </summary>
    public string GetUserMessage()
    {
        return Detail ?? Title ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 获取格式化的验证错误消息
    /// </summary>
    public string? GetValidationErrorMessage()
    {
        if (Errors == null || Errors.Count == 0)
            return null;

        var messages = Errors
            .SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))
            .ToList();

        return string.Join(Environment.NewLine, messages);
    }

    #endregion
}

namespace LYBT.Core.EventBus.Module;

/// <summary>
/// 模块健康状态
/// </summary>
public class ModuleHealthStatus
{
    /// <summary>
    /// 健康状态
    /// </summary>
    public HealthStatus Status { get; init; } = HealthStatus.Unknown;

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 检查时间
    /// </summary>
    public DateTime CheckTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 额外数据
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; init; } = 
        new Dictionary<string, object>();

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// 创建健康状态
    /// </summary>
    /// <param name="status">健康状态</param>
    /// <param name="description">描述</param>
    /// <param name="data">额外数据</param>
    /// <param name="exception">异常信息</param>
    /// <param name="responseTimeMs">响应时间</param>
    /// <returns>健康状态对象</returns>
    public static ModuleHealthStatus Create(
        HealthStatus status,
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null,
        Exception? exception = null,
        long responseTimeMs = 0)
    {
        return new ModuleHealthStatus
        {
            Status = status,
            Description = description ?? status.GetDefaultDescription(),
            Data = data ?? new Dictionary<string, object>(),
            Exception = exception,
            ResponseTimeMs = responseTimeMs,
            CheckTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建健康状态
    /// </summary>
    /// <param name="description">描述</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康状态对象</returns>
    public static ModuleHealthStatus Healthy(
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        return Create(HealthStatus.Healthy, description, data);
    }

    /// <summary>
    /// 创建不健康状态
    /// </summary>
    /// <param name="description">描述</param>
    /// <param name="exception">异常信息</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康状态对象</returns>
    public static ModuleHealthStatus Unhealthy(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        return Create(HealthStatus.Unhealthy, description, data, exception);
    }

    /// <summary>
    /// 创建降级状态
    /// </summary>
    /// <param name="description">描述</param>
    /// <param name="exception">异常信息</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康状态对象</returns>
    public static ModuleHealthStatus Degraded(
        string? description = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        return Create(HealthStatus.Degraded, description, data, exception);
    }

    /// <summary>
    /// 创建未知状态
    /// </summary>
    /// <param name="description">描述</param>
    /// <param name="data">额外数据</param>
    /// <returns>健康状态对象</returns>
    public static ModuleHealthStatus Unknown(
        string? description = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        return Create(HealthStatus.Unknown, description, data);
    }

    /// <summary>
    /// 检查是否健康
    /// </summary>
    /// <returns>是否健康</returns>
    public bool IsHealthy() => Status == HealthStatus.Healthy;

    /// <summary>
    /// 检查是否不健康
    /// </summary>
    /// <returns>是否不健康</returns>
    public bool IsUnhealthy() => Status == HealthStatus.Unhealthy;

    /// <summary>
    /// 检查是否降级
    /// </summary>
    /// <returns>是否降级</returns>
    public bool IsDegraded() => Status == HealthStatus.Degraded;

    /// <summary>
    /// 获取状态摘要
    /// </summary>
    /// <returns>状态摘要</returns>
    public string GetSummary()
    {
        var summary = $"状态: {Status.GetDisplayName()}";
        
        if (!string.IsNullOrWhiteSpace(Description))
        {
            summary += $", 描述: {Description}";
        }

        if (ResponseTimeMs > 0)
        {
            summary += $", 响应时间: {ResponseTimeMs}ms";
        }

        return summary;
    }
}

/// <summary>
/// 健康状态枚举
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 健康状态
    /// 模块运行正常
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// 降级状态
    /// 模块运行但功能受限
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// 不健康状态
    /// 模块无法正常工作
    /// </summary>
    Unhealthy = 3
}

/// <summary>
/// 健康状态扩展方法
/// </summary>
public static class HealthStatusExtensions
{
    /// <summary>
    /// 获取状态的显示名称
    /// </summary>
    /// <param name="status">健康状态</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(this HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Unknown => "未知",
            HealthStatus.Healthy => "健康",
            HealthStatus.Degraded => "降级",
            HealthStatus.Unhealthy => "不健康",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// 获取状态的默认描述
    /// </summary>
    /// <param name="status">健康状态</param>
    /// <returns>默认描述</returns>
    public static string GetDefaultDescription(this HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Unknown => "模块健康状态未知",
            HealthStatus.Healthy => "模块运行正常",
            HealthStatus.Degraded => "模块运行但功能受限",
            HealthStatus.Unhealthy => "模块无法正常工作",
            _ => "未定义的健康状态"
        };
    }

    /// <summary>
    /// 检查状态是否可接受
    /// </summary>
    /// <param name="status">健康状态</param>
    /// <returns>是否可接受</returns>
    public static bool IsAcceptable(this HealthStatus status)
    {
        return status is HealthStatus.Healthy or HealthStatus.Degraded;
    }
}
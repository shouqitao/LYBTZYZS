using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Interfaces;

/// <summary>
/// WebAPI 健康检查服务接口
/// </summary>
public interface IApiHealthCheckService
{
    /// <summary>
    /// 异步检查 WebAPI 连接状态
    /// </summary>
    /// <param name="timeout">超时时间(毫秒),默认 5000ms</param>
    /// <returns>API 健康状态</returns>
    Task<ApiHealthStatus> CheckHealthAsync(int timeout = 5000);

    /// <summary>
    /// 获取最后一次检查的错误信息
    /// </summary>
    string? LastErrorMessage { get; }
}

/// <summary>
/// API 健康状态枚举
/// </summary>
public enum ApiHealthStatus
{
    /// <summary>
    /// 连接中(检查中)
    /// </summary>
    Checking,

    /// <summary>
    /// 已连接(健康)
    /// </summary>
    Healthy,

    /// <summary>
    /// 连接失败(不健康)
    /// </summary>
    Unhealthy
}

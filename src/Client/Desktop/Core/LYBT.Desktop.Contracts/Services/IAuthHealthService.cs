using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 认证/API 健康检查服务接口
/// 封装 HealthCheck API，供 ViewModel 层使用（对齐 MVVM 分层：VM→Service→IApiClient）
/// </summary>
public interface IAuthHealthService
{
    /// <summary>
    /// 检查 API 服务健康状态。
    /// </summary>
    Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default);
}

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Admin.Sysadmin.Services;

/// <summary>
/// 认证健康检查服务 — 封装 IApiClientAuth.HealthCheckAsync 供 VM 消费。
/// </summary>
internal class AuthHealthService : IAuthHealthService
{
    private readonly IApiClientAuth _authApi;

    public AuthHealthService(IApiClientAuth authApi)
    {
        _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
    }

    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default)
        => _authApi.HealthCheckAsync();
}

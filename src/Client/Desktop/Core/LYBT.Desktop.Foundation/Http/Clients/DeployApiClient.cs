using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 部署 API 客户端——包装 IDeployApi（Refit）以实现 IApiClientDeploy。
/// </summary>
internal sealed class DeployApiClient : IApiClientDeploy
{
    private readonly IDeployApi _api;

    public DeployApiClient(IDeployApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content, CancellationToken ct = default)
        => _api.UploadAsync(content, ct);

    public Task<ApiResponse<object>> RestartAsync(CancellationToken ct = default)
        => _api.RestartAsync(ct);
}

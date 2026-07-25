using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// Deploy API client — wraps IDeployApi (Refit) to implement IApiClientDeploy.
/// </summary>
internal sealed class DeployApiClient : IApiClientDeploy
{
    private readonly IDeployApi _api;

    public DeployApiClient(IDeployApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content)
        => _api.UploadAsync(content);

    public Task<ApiResponse<object>> RestartAsync()
        => _api.RestartAsync();
}

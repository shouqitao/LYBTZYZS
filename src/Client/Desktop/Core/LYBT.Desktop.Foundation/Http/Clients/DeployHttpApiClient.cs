// ---------------------------------------------------------------------------
// DeployHttpApiClient — HttpClient adapter for IApiClientDeploy
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientDeploy (split from HttpClientApiClient).
// Local mode does not support deploy operations — always returns business failure.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式部署 API 客户端（拆分自 HttpClientApiClient，本地模式不支持部署）</summary>
internal sealed class DeployHttpApiClient : HttpApiClientBase, IApiClientDeploy
{
    public DeployHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content)
        => Task.FromResult(ApiResponse<object>.CreateFail("本地模式不支持部署更新"));

    public Task<ApiResponse<object>> RestartAsync()
        => Task.FromResult(ApiResponse<object>.CreateFail("本地模式不支持部署更新"));
}

using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>服务器部署端点。</summary>
public interface IApiClientDeploy
{
    Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content, CancellationToken ct = default);
    Task<ApiResponse<object>> RestartAsync(CancellationToken ct = default);
}

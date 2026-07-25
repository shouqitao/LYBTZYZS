using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>Server deployment endpoints.</summary>
public interface IApiClientDeploy
{
    Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content);
    Task<ApiResponse<object>> RestartAsync();
}

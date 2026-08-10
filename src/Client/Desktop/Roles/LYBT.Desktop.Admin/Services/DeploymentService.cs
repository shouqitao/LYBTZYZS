using System.Net.Http;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 服务器部署服务实现 — 封装 IApiClient.Deploy 供 VM 消费（D6: DP10 收口）。
    /// </summary>
    internal class DeploymentService : IDeploymentService
    {
        private readonly IApiClientDeploy _deployApi;

        public DeploymentService(IApiClientDeploy deployApi)
        {
            _deployApi = deployApi ?? throw new ArgumentNullException(nameof(deployApi));
        }

        public Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content)
            => _deployApi.UploadAsync(content);

        public Task<ApiResponse<object>> RestartAsync()
            => _deployApi.RestartAsync();
    }
}

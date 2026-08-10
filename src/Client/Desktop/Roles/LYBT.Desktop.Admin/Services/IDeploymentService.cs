using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 服务器部署服务接口（D6: DP10 收口——封装 IApiClient.Deploy，VM 禁止直注 IApiClient）。
    /// 对齐 SysadminHomeViewModel 经 IAuthHealthService 服务门面封装先例。
    /// </summary>
    public interface IDeploymentService
    {
        /// <summary>上传部署包到服务器。</summary>
        Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content);

        /// <summary>重启远程服务。</summary>
        Task<ApiResponse<object>> RestartAsync();
    }
}

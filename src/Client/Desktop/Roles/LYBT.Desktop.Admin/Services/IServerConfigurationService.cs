using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 服务器配置服务接口（D6: DP10 收口——封装 IApiClient.Configuration，VM 禁止直注 IApiClient）。
    /// 区别于 ISystemSettingsService（本地 JSON 设置）：本接口仅远程服务器安全配置项。
    /// 对齐 SysadminHomeViewModel 经 IAuthHealthService 服务门面封装先例。
    /// </summary>
    public interface IServerConfigurationService
    {
        /// <summary>获取服务器安全配置项集合。</summary>
        Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync();

        /// <summary>批量修改服务器配置项（白名单校验 + 持久化 + 热更新）。</summary>
        Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings);

        /// <summary>验证生产环境配置。</summary>
        Task<ApiResponse> ValidateProductionAsync();
        /// <summary>获取单节配置（脱敏）——服务端配置面板。</summary>
        Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section);
        /// <summary>修改单节配置（白名单 + 生效语义）——服务端配置面板。</summary>
        Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values);
        /// <summary>延迟重启服务（限频）——服务端配置面板。</summary>
        Task<ApiResponse> RestartAsync();
    }
}

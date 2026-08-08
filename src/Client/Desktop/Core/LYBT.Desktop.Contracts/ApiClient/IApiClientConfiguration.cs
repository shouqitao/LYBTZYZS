using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 系统配置 API 端点（服务器配置：App:Name/App:Version/App:Environment 等安全配置项）。
/// 仅远程模式有意义；本地模式由 ConfigurationHttpApiClient 抛 NotSupportedException。
/// </summary>
public interface IApiClientConfiguration
{
    /// <summary>获取安全配置项集合。</summary>
    Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync();

    /// <summary>获取单个配置项。</summary>
    Task<ApiResponse<string>> GetValueAsync(string key);

    /// <summary>修改单个配置项（白名单校验 + 持久化 + 热更新）。</summary>
    Task<ApiResponse> SetValueAsync(string key, string value);

    /// <summary>批量修改配置项（白名单校验 + 持久化 + 热更新）。</summary>
    Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings);

    /// <summary>验证生产环境配置。</summary>
    Task<ApiResponse> ValidateProductionAsync();
}

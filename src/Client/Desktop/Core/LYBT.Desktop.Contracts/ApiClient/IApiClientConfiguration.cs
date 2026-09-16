using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 系统配置 API 端点（服务器配置：App:Name/App:Version/App:Environment 等安全配置项）。
/// 仅远程模式有意义；本地模式由 ConfigurationHttpApiClient 抛 NotSupportedException。
/// </summary>
public interface IApiClientConfiguration
{
    /// <summary>获取安全配置项集合。</summary>
    Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>获取单个配置项。</summary>
    Task<ApiResponse<string>> GetValueAsync(string key, CancellationToken ct = default);

    /// <summary>修改单个配置项（白名单校验 + 持久化 + 热更新）。</summary>
    Task<ApiResponse> SetValueAsync(string key, string value, CancellationToken ct = default);

    /// <summary>批量修改配置项（白名单校验 + 持久化 + 热更新）。</summary>
    Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default);

    /// <summary>验证生产环境配置。</summary>
    Task<ApiResponse> ValidateProductionAsync(CancellationToken ct = default);

    /// <summary>获取单节配置（SHELL-018 Phase 1: 敏感键掩码）。</summary>
    Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section, CancellationToken ct = default);

    /// <summary>修改单节配置（SHELL-018 Phase 1: 白名单逐键 + 生效语义）。</summary>
    Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values, CancellationToken ct = default);

    /// <summary>延迟重启服务（SHELL-018 Phase 1: 30 秒后 StopApplication）。</summary>
    Task<ApiResponse> RestartAsync(CancellationToken ct = default);
}

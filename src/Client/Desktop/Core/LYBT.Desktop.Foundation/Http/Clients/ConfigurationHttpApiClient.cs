// ---------------------------------------------------------------------------
// ConfigurationHttpApiClient — HttpClient adapter for IApiClientConfiguration
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientConfiguration (split from HttpClientApiClient).
// 服务器配置区域仅远程有意义（本地模式隐藏）——所有方法抛 NotSupportedException，
// 与现有 local-only 桩模式（Refit 侧对本地方法抛 NotSupportedException）一致。
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式系统配置 API 客户端（服务器配置仅远程模式可用，本地模式不支持）</summary>
internal sealed class ConfigurationHttpApiClient : HttpApiClientBase, IApiClientConfiguration
{
    public ConfigurationHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync()
        => throw new NotSupportedException("GetConfigurationAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse<string>> GetValueAsync(string key)
        => throw new NotSupportedException("GetValueAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse> SetValueAsync(string key, string value)
        => throw new NotSupportedException("SetValueAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings)
        => throw new NotSupportedException("UpdateConfigurationAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse> ValidateProductionAsync()
        => throw new NotSupportedException("ValidateProductionAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section)
        => throw new NotSupportedException("GetSectionAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values)
        => throw new NotSupportedException("UpdateSectionAsync is a remote-only method and is not available in local mode.");

    public Task<ApiResponse> RestartAsync()
        => throw new NotSupportedException("RestartAsync is a remote-only method and is not available in local mode.");
}

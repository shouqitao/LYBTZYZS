using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 系统配置 API 客户端——包装 IConfigurationApi（Refit）以实现 IApiClientConfiguration。
/// </summary>
internal sealed class ConfigurationApiClient : IApiClientConfiguration
{
    private readonly IConfigurationApi _api;

    public ConfigurationApiClient(IConfigurationApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync()
        => _api.GetConfigurationAsync();

    public Task<ApiResponse<string>> GetValueAsync(string key)
        => _api.GetValueAsync(key);

    public Task<ApiResponse> SetValueAsync(string key, string value)
        => _api.SetValueAsync(key, value);

    public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings)
        => _api.UpdateConfigurationAsync(settings);

    public Task<ApiResponse> ValidateProductionAsync()
        => _api.ValidateProductionAsync();
}

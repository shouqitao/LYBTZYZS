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

    public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken ct = default)
        => _api.GetConfigurationAsync(ct);

    public Task<ApiResponse<string>> GetValueAsync(string key, CancellationToken ct = default)
        => _api.GetValueAsync(key, ct);

    public Task<ApiResponse> SetValueAsync(string key, string value, CancellationToken ct = default)
        => _api.SetValueAsync(key, value, ct);

    public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default)
        => _api.UpdateConfigurationAsync(settings, ct);

    public Task<ApiResponse> ValidateProductionAsync(CancellationToken ct = default)
        => _api.ValidateProductionAsync(ct);

    public Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section, CancellationToken ct = default)
        => _api.GetSectionAsync(section, ct);

    public Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values, CancellationToken ct = default)
        => _api.UpdateSectionAsync(section, values, ct);

    public Task<ApiResponse> RestartAsync(CancellationToken ct = default)
        => _api.RestartAsync(ct);
}

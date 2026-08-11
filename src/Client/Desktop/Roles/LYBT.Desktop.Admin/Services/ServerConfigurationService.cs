using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 服务器配置服务实现 — 封装 IApiClient.Configuration 供 VM 消费（D6: DP10 收口）。
    /// </summary>
    internal class ServerConfigurationService : IServerConfigurationService
    {
        private readonly IApiClientConfiguration _configurationApi;

        public ServerConfigurationService(IApiClientConfiguration configurationApi)
        {
            _configurationApi = configurationApi ?? throw new ArgumentNullException(nameof(configurationApi));
        }

        public Task<ApiResponse<Dictionary<string, string>>> GetConfigurationAsync()
            => _configurationApi.GetConfigurationAsync();

        public Task<ApiResponse> UpdateConfigurationAsync(Dictionary<string, string> settings)
            => _configurationApi.UpdateConfigurationAsync(settings);

        public Task<ApiResponse<Dictionary<string, string>>> GetSectionAsync(string section)
            => _configurationApi.GetSectionAsync(section);

        public Task<ApiResponse<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values)
            => _configurationApi.UpdateSectionAsync(section, values);

        public Task<ApiResponse> RestartAsync()
            => _configurationApi.RestartAsync();

        public Task<ApiResponse> ValidateProductionAsync()
            => _configurationApi.ValidateProductionAsync();
    }
}

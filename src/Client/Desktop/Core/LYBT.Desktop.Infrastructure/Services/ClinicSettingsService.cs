using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 诊所配置服务实现
    /// D2: 诊所信息配置化——读取走 <see cref="IConfiguration"/> 实时绑定（clinic-settings.json，
    /// reloadOnChange 已启用），保存走 <see cref="IClientConfigurationStore"/> 节级原子写 + Reload，
    /// 因此「保存即生效」，无需重启。
    /// </summary>
    public class ClinicSettingsService : IClinicSettingsService
    {
        private readonly IConfiguration _configuration;
        private readonly IClientConfigurationStore _configurationStore;
        private readonly ILogger<ClinicSettingsService> _logger;

        public ClinicSettingsService(
            IConfiguration configuration,
            IClientConfigurationStore configurationStore,
            ILogger<ClinicSettingsService> logger)
        {
            _configuration = configuration;
            _configurationStore = configurationStore;
            _logger = logger;
        }

        public string ClinicName => GetSettings().Name;
        public string ClinicAddress => GetSettings().Address;
        public string ClinicPhone => GetSettings().Phone;
        public string Department => GetSettings().Department;
        public string LicenseNumber => GetSettings().LicenseNumber;
        public string Email => GetSettings().Email;

        /// <summary>
        /// 获取当前诊所配置（每次从 IConfiguration 实时绑定——保存后立即可见，支持热更新）
        /// </summary>
        public ClinicSettingsOptions GetSettings()
        {
            return _configuration
                       .GetSection(ClinicSettingsOptions.SectionName)
                       .Get<ClinicSettingsOptions>()
                   ?? new ClinicSettingsOptions();
        }

        /// <summary>
        /// 保存诊所配置：经 <see cref="IClientConfigurationStore"/> 节级原子写（写 clinic-settings.json
        /// + .bak 备份 + IConfiguration.Reload），保留其他配置节。
        /// </summary>
        public async Task<bool> SaveSettingsAsync(ClinicSettingsOptions settings)
        {
            if (settings is null)
                return false;

            try
            {
                var values = new Dictionary<string, object>
                {
                    ["Name"] = settings.Name ?? string.Empty,
                    ["Address"] = settings.Address ?? string.Empty,
                    ["Phone"] = settings.Phone ?? string.Empty,
                    ["Department"] = settings.Department ?? string.Empty,
                    ["LicenseNumber"] = settings.LicenseNumber ?? string.Empty,
                    ["Email"] = settings.Email ?? string.Empty
                };

                // 节级覆盖会整体替换该节——时区非空时必须一并写入，否则静默丢失
                if (!string.IsNullOrWhiteSpace(settings.Timezone))
                    values["Timezone"] = settings.Timezone!;

                var saved = await _configurationStore.SaveSectionAsync(ClinicSettingsOptions.SectionName, values);
                if (saved)
                {
                    _logger.LogInformation(
                        "诊所配置已保存（节 {Section}，文件 {FilePath}）",
                        ClinicSettingsOptions.SectionName,
                        _configurationStore.ResolveFilePath(ClinicSettingsOptions.SectionName));
                }

                return saved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊所配置失败");
                return false;
            }
        }
    }
}

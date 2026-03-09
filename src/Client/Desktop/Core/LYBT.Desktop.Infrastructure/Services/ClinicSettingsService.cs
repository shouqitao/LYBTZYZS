using System.IO;
using System.Text.Json;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 诊所配置服务实现
    /// D2: 从 clinic-settings.json 读取配置，支持热更新 (reloadOnChange)
    /// SaveSettingsAsync 写入文件后，IConfiguration 自动重载
    /// </summary>
    public class ClinicSettingsService : IClinicSettingsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClinicSettingsService> _logger;

        private static readonly JsonSerializerOptions JsonWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public string ClinicName => GetSettings().Name;
        public string ClinicAddress => GetSettings().Address;
        public string ClinicPhone => GetSettings().Phone;
        public string Department => GetSettings().Department;
        public string LicenseNumber => GetSettings().LicenseNumber;
        public string Email => GetSettings().Email;

        public ClinicSettingsService(
            IConfiguration configuration,
            ILogger<ClinicSettingsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前诊所配置（每次从 IConfiguration 读取，支持热更新）
        /// </summary>
        public ClinicSettingsOptions GetSettings()
        {
            var section = _configuration.GetSection(ClinicSettingsOptions.SectionName);
            return section.Get<ClinicSettingsOptions>() ?? new ClinicSettingsOptions();
        }

        /// <summary>
        /// 保存诊所配置到 clinic-settings.json，写入后 IConfiguration 自动重载
        /// </summary>
        public async Task<bool> SaveSettingsAsync(ClinicSettingsOptions settings)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "clinic-settings.json");

                var wrapper = new Dictionary<string, ClinicSettingsOptions>
                {
                    [ClinicSettingsOptions.SectionName] = settings
                };

                var json = JsonSerializer.Serialize(wrapper, JsonWriteOptions);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("诊所配置已保存到 {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊所配置失败");
                return false;
            }
        }
    }
}

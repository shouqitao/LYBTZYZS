using LYBT.UI.PrismWpf.Models;
using LYBT.UI.PrismWpf.Services;

namespace LYBT.UI.PrismWpf.Services
{
    /// <summary>
    /// 应用设置服务接口
    /// </summary>
    public interface IAppSettingsService
    {
        ApiSettings ApiSettings { get; }
        AppSettings AppSettings { get; }
        void SaveSettings();
        void ReloadSettings();
    }

    /// <summary>
    /// 应用设置服务实现
    /// </summary>
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IConfigurationService _configurationService;
        private AppConfiguration _appConfiguration;

        public AppSettingsService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            LoadSettings();
        }

        public ApiSettings ApiSettings => _appConfiguration.ApiSettings;
        public AppSettings AppSettings => _appConfiguration.AppSettings;

        private void LoadSettings()
        {
            _appConfiguration = new AppConfiguration
            {
                ApiSettings = _configurationService.GetSection<ApiSettings>("ApiSettings"),
                AppSettings = _configurationService.GetSection<AppSettings>("AppSettings")
            };
        }

        public void SaveSettings()
        {
            // 这里可以实现保存设置到文件的逻辑
            // 暂时保留为后续扩展
        }

        public void ReloadSettings()
        {
            LoadSettings();
        }
    }
}
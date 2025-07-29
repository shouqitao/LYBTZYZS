using LYBT.UI.PrismWpf.Models;
using LYBT.UI.PrismWpf.Services;

namespace LYBT.UI.PrismWpf.Configuration
{
    /// <summary>
    /// 配置管理器
    /// </summary>
    public class ConfigurationManager
    {
        private static ConfigurationManager? _instance;
        private static readonly object _lock = new object();
        
        private readonly IConfigurationService _configurationService;
        
        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ConfigurationManager();
                    }
                }
                return _instance;
            }
        }

        private ConfigurationManager()
        {
            _configurationService = new ConfigurationService();
            LoadConfiguration();
        }

        public ApiSettings ApiSettings { get; private set; } = new();
        public AppSettings AppSettings { get; private set; } = new();

        private void LoadConfiguration()
        {
            try
            {
                ApiSettings = _configurationService.GetSection<ApiSettings>("ApiSettings");
                AppSettings = _configurationService.GetSection<AppSettings>("AppSettings");
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"配置加载失败: {ex.Message}");
                
                // 使用默认配置
                ApiSettings = new ApiSettings();
                AppSettings = new AppSettings();
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfiguration()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// 获取API基础地址
        /// </summary>
        public string GetApiBaseUrl() => ApiSettings.BaseUrl;

        /// <summary>
        /// 获取API超时时间
        /// </summary>
        public TimeSpan GetApiTimeout() => TimeSpan.FromSeconds(ApiSettings.Timeout);
    }
}
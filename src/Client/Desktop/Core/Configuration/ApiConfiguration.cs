using System.IO;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Core.Configuration
{

    /// <summary>
    /// API客户端配置
    /// </summary>
    public static class ApiConfiguration
    {
        private static ApiSettings? _settings;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取API设置
        /// </summary>
        public static ApiSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    lock (_lock)
                    {
                        if (_settings == null)
                        {
                            LoadSettings();
                        }
                    }
                }
                return _settings!;
            }
        }

        /// <summary>
        /// API基础地址
        /// </summary>
        public static string BaseUrl
        {
            get => Settings.BaseUrl;
            set => Settings.BaseUrl = value;
        }

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public static int TimeoutSeconds
        {
            get => Settings.TimeoutSeconds;
            set => Settings.TimeoutSeconds = value;
        }

        /// <summary>
        /// 获取完整的API地址
        /// </summary>
        public static string GetApiUrl(string endpoint = "") => Settings.GetApiUrl(endpoint);

        /// <summary>
        /// 获取超时时间跨度
        /// </summary>
        public static TimeSpan GetTimeout() => Settings.GetTimeout();

        /// <summary>
        /// 加载配置设置
        /// </summary>
        private static void LoadSettings()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: true)
                    .Build();

                var apiSection = configuration.GetSection(ApiSettings.SectionName);
                var settings = apiSection.Get<ApiSettings>() ?? new ApiSettings();

                // DT-012配置验证：验证配置项的有效性
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(settings);
                
                if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(settings, validationContext, validationResults, true))
                {
                    var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                    throw new InvalidOperationException($"API配置验证失败: {string.Join("; ", errors)}");
                }

                // 确保URL以斜杠结尾
                if (!settings.BaseUrl.EndsWith("/"))
                {
                    settings.BaseUrl += "/";
                }

                _settings = settings;
            }
            catch (Exception ex)
            {
                // 如果配置文件加载失败，抛出更详细的异常
                throw new InvalidOperationException($"无法加载API配置: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public static void ReloadSettings()
        {
            lock (_lock)
            {
                _settings = null!;
            }
        }
    }
}

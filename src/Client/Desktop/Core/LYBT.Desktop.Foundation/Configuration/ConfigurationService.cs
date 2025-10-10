using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Configuration
{
    /// <summary>
    /// 简化的配置管理服务 - 遵循"适度设计、拒绝过度工程"原则
    /// 提供基本的配置读写功能，避免过度复杂的分层架构
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 获取配置值
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// 设置配置值
        /// </summary>
        Task SetValueAsync<T>(string key, T value);

        /// <summary>
        /// 获取配置节
        /// </summary>
        IConfigurationSection GetSection(string key);

        /// <summary>
        /// 重载配置
        /// </summary>
        Task ReloadAsync();
    }

    /// <summary>
    /// 简化的配置管理服务实现
    /// </summary>
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configPath;
        private readonly string _userConfigFile;
        private IConfigurationRoot _configuration = null!;
        private readonly Dictionary<string, object?> _userSettings;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            _userConfigFile = Path.Combine(_configPath, "user.config.json");
            _userSettings = new Dictionary<string, object?>();

            InitializeConfiguration();
            LoadUserSettings();
        }

        private void InitializeConfiguration()
        {
            try
            {
                Directory.CreateDirectory(_configPath);

                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables("LYBT_")
                    .AddInMemoryCollection(GetDefaultSettings());

                _configuration = builder.Build();

                _logger.LogInformation("配置服务初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置服务初始化失败");
                throw;
            }
        }

        private Dictionary<string, string?> GetDefaultSettings()
        {
            return new Dictionary<string, string?>
            {
                ["Application:Name"] = "凌隐宝堂中医诊所系统",
                ["Application:Version"] = "2.1.0",
                ["Logging:LogLevel:Default"] = "Information",
                ["Cache:DefaultExpiration"] = "300",
                ["UI:Theme"] = "Light",
                ["UI:Language"] = "zh-CN",
                ["API:Timeout"] = "30",
                ["API:RetryCount"] = "3",
                ["Security:SessionTimeout"] = "30"
            };
        }

        private void LoadUserSettings()
        {
            try
            {
                if (!File.Exists(_userConfigFile))
                {
                    File.WriteAllText(_userConfigFile, "{}");
                    return;
                }

                var json = File.ReadAllText(_userConfigFile);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        _userSettings[kvp.Key] = kvp.Value;
                    }
                }

                _logger.LogDebug("用户配置加载完成，共 {Count} 项设置", _userSettings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户配置失败");
            }
        }

        /// <inheritdoc/>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            try
            {
                // 优先从用户设置获取
                if (_userSettings.TryGetValue(key, out var userValue) && userValue != null)
                {
                    if (userValue is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    if (userValue is T directValue)
                    {
                        return directValue;
                    }
                    // 尝试类型转换
                    return (T?)Convert.ChangeType(userValue, typeof(T));
                }

                // 从配置文件获取
                var value = _configuration.GetValue<T>(key);
                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置值失败: {Key}", key);
                return defaultValue;
            }
        }

        /// <inheritdoc/>
        public async Task SetValueAsync<T>(string key, T value)
        {
            try
            {
                _userSettings[key] = value;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_userSettings, options);
                await File.WriteAllTextAsync(_userConfigFile, json);

                _logger.LogInformation("配置已更新: {Key} = {Value}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置配置值失败: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <inheritdoc/>
        public Task ReloadAsync()
        {
            try
            {
                _logger.LogInformation("开始重新加载配置");

                InitializeConfiguration();
                LoadUserSettings();

                _logger.LogInformation("配置重新加载完成");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载配置失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // IConfigurationRoot不需要手动释放
            _logger.LogInformation("配置服务已关闭");
        }
    }

    /// <summary>
    /// 配置服务扩展方法
    /// </summary>
    public static class ConfigurationServiceExtensions
    {
        /// <summary>
        /// 获取API基础URL
        /// </summary>
        public static string GetApiBaseUrl(this IConfigurationService config)
        {
            return config.GetValue<string>("API:BaseUrl") ?? "https://localhost:5001";
        }

        /// <summary>
        /// 获取API超时时间
        /// </summary>
        public static int GetApiTimeout(this IConfigurationService config)
        {
            return config.GetValue<int>("API:Timeout", 30);
        }

        /// <summary>
        /// 获取UI主题
        /// </summary>
        public static string GetUITheme(this IConfigurationService config)
        {
            return config.GetValue<string>("UI:Theme") ?? "Light";
        }

        /// <summary>
        /// 获取语言设置
        /// </summary>
        public static string GetLanguage(this IConfigurationService config)
        {
            return config.GetValue<string>("UI:Language") ?? "zh-CN";
        }

        /// <summary>
        /// 是否启用动画
        /// </summary>
        public static bool IsAnimationEnabled(this IConfigurationService config)
        {
            return config.GetValue<bool>("UI:AnimationEnabled", true);
        }
    }
}

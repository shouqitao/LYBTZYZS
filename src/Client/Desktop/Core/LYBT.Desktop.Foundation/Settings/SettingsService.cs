using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Settings
{
    /// <summary>
    /// 设置服务接口
    /// </summary>
    public interface ISettingsService
    {
        T GetSetting<T>(string key);
        Task SaveSettingAsync<T>(string key, T value);
        Task ResetToDefaultsAsync();
        bool HasSetting(string key);
    }

    /// <summary>
    /// 设置服务实现 - UltraThink架构
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly Dictionary<string, object> _settings = new();

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadDefaultSettings();
        }

        public T GetSetting<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default!;
        }

        public async Task SaveSettingAsync<T>(string key, T value)
        {
            await Task.Run(() =>
            {
                _settings[key] = value!;
                _logger.LogInformation("保存设置：{Key} = {Value}", key, value);
            });
        }

        public async Task ResetToDefaultsAsync()
        {
            await Task.Run(() =>
            {
                _settings.Clear();
                LoadDefaultSettings();
                _logger.LogInformation("设置已重置为默认值");
            });
        }

        public bool HasSetting(string key)
        {
            return _settings.ContainsKey(key);
        }

        private void LoadDefaultSettings()
        {
            _settings["Theme"] = "Light";
            _settings["Language"] = "zh-CN";
            _settings["AutoSave"] = true;
        }
    }
}

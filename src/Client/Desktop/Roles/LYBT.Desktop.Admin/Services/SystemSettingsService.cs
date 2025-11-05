using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 系统设置服务实现
    /// 负责系统配置的持久化管理（%LOCALAPPDATA%\LYBT\Desktop\system-settings.json）
    /// Epic #1832 Phase 2 - 系统设置完整实现
    /// </summary>
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ILogger<SystemSettingsService> _logger;
        private readonly string _settingsFilePath;
        private SystemSettings _settings;

        public SystemSettingsService(ILogger<SystemSettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 配置文件路径：%LOCALAPPDATA%\LYBT\Desktop\system-settings.json
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtDir = Path.Combine(localAppData, "LYBT", "Desktop");
            Directory.CreateDirectory(lybtDir); // 确保目录存在
            _settingsFilePath = Path.Combine(lybtDir, "system-settings.json");

            _settings = LoadSettings();
        }

        #region 公共属性

        public string SystemName
        {
            get => _settings.SystemName;
            set
            {
                if (_settings.SystemName != value)
                {
                    _settings.SystemName = value;
                    Save();
                }
            }
        }

        public string HospitalName
        {
            get => _settings.HospitalName;
            set
            {
                if (_settings.HospitalName != value)
                {
                    _settings.HospitalName = value;
                    Save();
                }
            }
        }

        public string ContactPhone
        {
            get => _settings.ContactPhone;
            set
            {
                if (_settings.ContactPhone != value)
                {
                    _settings.ContactPhone = value;
                    Save();
                }
            }
        }

        public bool AutoBackupEnabled
        {
            get => _settings.AutoBackupEnabled;
            set
            {
                if (_settings.AutoBackupEnabled != value)
                {
                    _settings.AutoBackupEnabled = value;
                    Save();
                }
            }
        }

        public string BackupPath
        {
            get => _settings.BackupPath;
            set
            {
                if (_settings.BackupPath != value)
                {
                    _settings.BackupPath = value;
                    Save();
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 保存系统设置到本地文件
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);
                _logger.LogDebug("系统设置已保存到: {Path}", _settingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存系统设置失败");
            }
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            _logger.LogInformation("重置系统设置为默认值");
            _settings = CreateDefaultSettings();
            Save();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从文件加载系统设置
        /// </summary>
        private SystemSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<SystemSettings>(json);
                    if (settings != null)
                    {
                        _logger.LogInformation("系统设置已加载: {SystemName}", settings.SystemName);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载系统设置失败，使用默认配置");
            }

            // 默认配置
            return CreateDefaultSettings();
        }

        /// <summary>
        /// 创建默认设置
        /// </summary>
        private static SystemSettings CreateDefaultSettings()
        {
            return new SystemSettings
            {
                SystemName = "中医诊疗系统",
                HospitalName = string.Empty,
                ContactPhone = string.Empty,
                AutoBackupEnabled = false,
                BackupPath = string.Empty
            };
        }

        #endregion

        #region 私有模型类

        /// <summary>
        /// 系统设置数据模型
        /// </summary>
        private class SystemSettings
        {
            public string SystemName { get; set; } = "中医诊疗系统";
            public string HospitalName { get; set; } = string.Empty;
            public string ContactPhone { get; set; } = string.Empty;
            public bool AutoBackupEnabled { get; set; }
            public string BackupPath { get; set; } = string.Empty;
        }

        #endregion
    }
}

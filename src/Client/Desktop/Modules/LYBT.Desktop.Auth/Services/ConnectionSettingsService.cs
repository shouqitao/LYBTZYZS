using System.IO;
using System.Text.Json;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Auth.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 连接设置服务实现 - Issue #1825
/// 负责管理远程/本地连接模式的持久化配置
/// </summary>
public class ConnectionSettingsService : IConnectionSettingsService
{
    private readonly ILogger<ConnectionSettingsService> _logger;
    private readonly string _settingsFilePath;
    private ConnectionSettings _settings;

    public ConnectionSettingsService(ILogger<ConnectionSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 配置文件路径：%LOCALAPPDATA%\LYBT\Desktop\connection-settings.json
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var lybthDir = Path.Combine(localAppData, "LYBT", "Desktop");
        Directory.CreateDirectory(lybthDir); // 确保目录存在
        _settingsFilePath = Path.Combine(lybthDir, "connection-settings.json");

        // 加载配置
        _settings = LoadSettings();
    }

    /// <summary>
    /// 获取当前连接模式
    /// </summary>
    public ConnectionMode GetConnectionMode()
    {
        return _settings.DefaultMode;
    }

    /// <summary>
    /// 保存连接模式
    /// </summary>
    public void SaveConnectionMode(ConnectionMode mode)
    {
        _settings.DefaultMode = mode;
        SaveSettings();
        _logger.LogInformation("连接模式已保存: {Mode}", mode);
    }

    /// <summary>
    /// 是否记住上次选择
    /// </summary>
    public bool RememberLastChoice
    {
        get => _settings.RememberLastChoice;
        set
        {
            _settings.RememberLastChoice = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    private ConnectionSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ConnectionSettings>(json);
                if (settings != null)
                {
                    _logger.LogInformation("连接设置已加载: {Mode}", settings.DefaultMode);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载连接设置失败，使用默认配置");
        }

        // 默认配置
        return new ConnectionSettings
        {
            DefaultMode = ConnectionMode.Remote,
            RememberLastChoice = true
        };
    }

    /// <summary>
    /// 保存配置文件
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
            _logger.LogDebug("连接设置已保存到: {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存连接设置失败");
        }
    }

    /// <summary>
    /// 连接设置数据模型
    /// </summary>
    private class ConnectionSettings
    {
        public ConnectionMode DefaultMode { get; set; }
        public bool RememberLastChoice { get; set; }
    }
}

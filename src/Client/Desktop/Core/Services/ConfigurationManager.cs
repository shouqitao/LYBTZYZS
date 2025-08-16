using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 配置管理器实现
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private readonly ILogger<ConfigurationManager>? _logger;
        private readonly string _configFilePath;
        private Dictionary<string, object> _configurations;
        private readonly object _lockObject = new();
        
        public ConfigurationManager(ILogger<ConfigurationManager>? logger = null)
        {
            _logger = logger;
            
            // 获取配置文件路径
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LYBT", "Config");
            Directory.CreateDirectory(appDirectory);
            
            _configFilePath = Path.Combine(appDirectory, "appsettings.json");
            _configurations = new Dictionary<string, object>();
            
            LoadConfiguration();
        }
        
        public T GetValue<T>(string key)
        {
            lock (_lockObject)
            {
                if (_configurations.TryGetValue(key, out var value))
                {
                    try
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText())!;
                        }
                        
                        if (value is T typedValue)
                        {
                            return typedValue;
                        }
                        
                        // 尝试转换
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "转换配置值失败: {Key}", key);
                        throw new InvalidOperationException($"无法将配置 '{key}' 转换为类型 {typeof(T).Name}", ex);
                    }
                }
                
                throw new KeyNotFoundException($"配置项 '{key}' 不存在");
            }
        }
        
        public T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                return GetValue<T>(key);
            }
            catch (KeyNotFoundException)
            {
                return defaultValue;
            }
        }
        
        public void SetValue<T>(string key, T value)
        {
            lock (_lockObject)
            {
                _configurations[key] = value!;
                _logger?.LogDebug("设置配置项: {Key} = {Value}", key, value);
            }
        }
        
        public bool Contains(string key)
        {
            lock (_lockObject)
            {
                return _configurations.ContainsKey(key);
            }
        }
        
        public string GetConnectionString(string name)
        {
            var key = $"ConnectionStrings:{name}";
            return GetValue<string>(key);
        }
        
        public void Reload()
        {
            _logger?.LogInformation("重新加载配置文件");
            LoadConfiguration();
        }
        
        public void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_configurations, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(_configFilePath, json);
                    _logger?.LogInformation("配置已保存到: {Path}", _configFilePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "保存配置失败");
                    throw;
                }
            }
        }
        
        private void LoadConfiguration()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_configFilePath))
                    {
                        var json = File.ReadAllText(_configFilePath);
                        _configurations = JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                                         ?? new Dictionary<string, object>();
                        _logger?.LogInformation("从文件加载配置: {Path}", _configFilePath);
                    }
                    else
                    {
                        // 创建默认配置
                        CreateDefaultConfiguration();
                        Save();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "加载配置失败，使用默认配置");
                    CreateDefaultConfiguration();
                }
            }
        }
        
        private void CreateDefaultConfiguration()
        {
            _configurations = new Dictionary<string, object>
            {
                ["ApiBaseUrl"] = "https://localhost:7001",
                ["ApiTimeout"] = 30,
                ["MaxRetryAttempts"] = 3,
                ["EnableLogging"] = true,
                ["Theme"] = "Light",
                ["Language"] = "zh-CN",
                ["AutoLogin"] = false,
                ["RememberUsername"] = true,
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDB;Trusted_Connection=True;"
            };
            
            _logger?.LogInformation("创建默认配置");
        }
    }
}
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Configuration
{

    /// <summary>
    /// 配置管理服务接口 - UltraThink Stage 5.3.2
    /// 提供分层配置管理、动态更新、优先级覆盖等高级功能
    /// </summary>
    public interface IConfigurationManagerService
    {

        /// <summary>
        /// 获取配置值
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// 获取配置节
        /// </summary>
        IConfigurationSection GetSection(string key);

        /// <summary>
        /// 设置配置值（仅影响用户层）
        /// </summary>
        Task SetValueAsync<T>(string key, T value);

        /// <summary>
        /// 重载配置
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// 注册配置变更监听
        /// </summary>
        IDisposable RegisterChangeCallback(Action<ConfigurationChangeEventArgs> callback);

        /// <summary>
        /// 获取配置层级信息
        /// </summary>
        ConfigurationLayerInfo GetLayerInfo(string key);

        /// <summary>
        /// 导出配置
        /// </summary>
        Task<string> ExportConfigurationAsync(ConfigurationExportOptions options);

        /// <summary>
        /// 导入配置
        /// </summary>
        Task ImportConfigurationAsync(string configData, ConfigurationImportOptions options);

        /// <summary>
        /// 验证配置
        /// </summary>
        ValidationResult ValidateConfiguration();

        /// <summary>
        /// 获取配置统计
        /// </summary>
        ConfigurationStatistics GetStatistics();
    }

    /// <summary>
    /// 分层配置管理服务实现
    /// </summary>
    public class ConfigurationManagerService : IConfigurationManagerService, IDisposable
    {
        private readonly ILogger<ConfigurationManagerService> _logger;
        private readonly object _lock = new object();
        private readonly List<ConfigurationChangeCallback> _changeCallbacks = new();
        private readonly Dictionary<ConfigurationLayer, IConfigurationRoot> _configurations = new();
        private readonly Dictionary<string, ConfigurationMetadata> _metadata = new();
        private readonly ConfigurationStatistics _statistics = new();

        private IConfigurationRoot _mergedConfiguration = null!;
        private FileSystemWatcher? _fileWatcher;
        private Timer? _autoSaveTimer;
        private readonly string _configPath;

        public ConfigurationManagerService(ILogger<ConfigurationManagerService> logger)
        {
            _logger = logger;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

            InitializeConfigurations();
            SetupFileWatcher();
            SetupAutoSave();
        }

        #region 初始化

        private void InitializeConfigurations()
        {
            try
            {
                // 确保配置目录存在
                Directory.CreateDirectory(_configPath);

                // 加载各层配置
                LoadDefaultConfiguration();
                LoadEnvironmentConfiguration();
                LoadUserConfiguration();
                LoadDynamicConfiguration();

                // 合并配置
                MergeConfigurations();

                _logger.LogInformation("配置管理服务初始化完成，加载了 {LayerCount} 层配置", _configurations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置管理服务初始化失败");
                throw;
            }
        }

        private void LoadDefaultConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddInMemoryCollection(GetDefaultSettings().Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)));

            _configurations[ConfigurationLayer.Default] = builder.Build();
            _logger.LogDebug("默认配置加载完成");
        }

        private void LoadEnvironmentConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("LYBT_ENVIRONMENT") ?? "Development";
            var envConfigFile = Path.Combine(_configPath, $"appsettings.{environment}.json");

            if (File.Exists(envConfigFile))
            {
                var builder = new ConfigurationBuilder()
                    .AddJsonFile(envConfigFile, optional: true, reloadOnChange: true);

                _configurations[ConfigurationLayer.Environment] = builder.Build();
                _logger.LogDebug("环境配置加载完成: {Environment}", environment);
            }
        }

        private void LoadUserConfiguration()
        {
            var userConfigFile = Path.Combine(_configPath, "user.config.json");

            if (!File.Exists(userConfigFile))
            {
                // 创建默认用户配置
                File.WriteAllText(userConfigFile, "{}");
            }

            var builder = new ConfigurationBuilder()
                .AddJsonFile(userConfigFile, optional: false, reloadOnChange: true);

            _configurations[ConfigurationLayer.User] = builder.Build();
            _logger.LogDebug("用户配置加载完成");
        }

        private void LoadDynamicConfiguration()
        {
            var dynamicConfig = new Dictionary<string, string>();
            _configurations[ConfigurationLayer.Dynamic] = new ConfigurationBuilder()
                .AddInMemoryCollection(dynamicConfig.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)))
                .Build();

            _logger.LogDebug("动态配置初始化完成");
        }

        private Dictionary<string, string> GetDefaultSettings()
        {
            return new Dictionary<string, string>
            {
                ["Application:Name"] = "凌隐宝堂中医诊所系统",
                ["Application:Version"] = "1.0.0",
                ["Logging:LogLevel:Default"] = "Information",
                ["Cache:DefaultExpiration"] = "300",
                ["Performance:SlowOperationThreshold"] = "1000",
                ["UI:Theme"] = "Light",
                ["UI:Language"] = "zh-CN",
                ["UI:AnimationEnabled"] = "true",
                ["API:Timeout"] = "30",
                ["API:RetryCount"] = "3",
                ["Security:PasswordMinLength"] = "8",
                ["Security:SessionTimeout"] = "30"
            };
        }

        #endregion 初始化

        #region 配置合并

        private void MergeConfigurations()
        {
            lock (_lock)
            {
                var sources = new List<IConfigurationSource>();

                // 按优先级顺序添加配置源（越后面优先级越高）
                foreach (var layer in Enum.GetValues<ConfigurationLayer>().OrderBy(l => (int)l))
                {
                    if (_configurations.ContainsKey(layer))
                    {
                        foreach (var provider in _configurations[layer].Providers)
                        {
                            // 这里需要复制provider的数据
                            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                            foreach (var child in _configurations[layer].GetChildren())
                            {
                                AddToDictionary(data, child);
                            }

                            sources.Add(new MemoryConfigurationSource { InitialData = data });
                        }
                    }
                }

                var builder = new ConfigurationBuilder();
                foreach (var source in sources)
                {
                    builder.Add(source);
                }

                _mergedConfiguration = builder.Build();

                // 更新元数据
                UpdateMetadata();

                _statistics.LastMergeTime = DateTime.Now;
                _statistics.MergeCount++;
            }
        }

        private void AddToDictionary(Dictionary<string, string?> data, IConfigurationSection section)
        {
            if (section.Value != null)
            {
                data[section.Path] = section.Value;
            }

            foreach (var child in section.GetChildren())
            {
                AddToDictionary(data, child);
            }
        }

        private void UpdateMetadata()
        {
            _metadata.Clear();

            foreach (var section in _mergedConfiguration.GetChildren())
            {
                UpdateSectionMetadata(section, ConfigurationLayer.Default);
            }
        }

        private void UpdateSectionMetadata(IConfigurationSection section, ConfigurationLayer layer)
        {
            _metadata[section.Path] = new ConfigurationMetadata
            {
                Key = section.Path,
                Layer = layer,
                LastModified = DateTime.Now,
                IsOverridden = false
            };

            foreach (var child in section.GetChildren())
            {
                UpdateSectionMetadata(child, layer);
            }
        }

        #endregion 配置合并

        #region 公共方法

        /// <inheritdoc/>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            _statistics.ReadCount++;

            try
            {
                var value = _mergedConfiguration.GetValue<T>(key);

                if (value == null)
                {
                    _logger.LogDebug("配置键 {Key} 未找到，使用默认值", key);
                    return defaultValue;
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置值失败: {Key}", key);
                return defaultValue;
            }
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key)
        {
            _statistics.ReadCount++;
            return _mergedConfiguration.GetSection(key);
        }

        /// <inheritdoc/>
        public async Task SetValueAsync<T>(string key, T value)
        {
            try
            {
                // 更新用户配置
                var userConfigFile = Path.Combine(_configPath, "user.config.json");
                var userConfig = new Dictionary<string, object?>();

                if (File.Exists(userConfigFile))
                {
                    var json = await File.ReadAllTextAsync(userConfigFile);
                    userConfig = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
                }

                // 设置值（支持嵌套键）
                SetNestedValue(userConfig, key, value);

                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var updatedJson = JsonSerializer.Serialize(userConfig, options);
                await File.WriteAllTextAsync(userConfigFile, updatedJson);

                // 重新加载用户配置
                LoadUserConfiguration();
                MergeConfigurations();

                // 触发变更通知
                NotifyChange(new ConfigurationChangeEventArgs
                {
                    Key = key,
                    OldValue = GetValue<T>(key),
                    NewValue = value,
                    Layer = ConfigurationLayer.User,
                    Timestamp = DateTime.Now
                });

                _statistics.WriteCount++;
                _logger.LogInformation("配置已更新: {Key} = {Value}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置配置值失败: {Key}", key);
                throw;
            }
        }

        private void SetNestedValue(Dictionary<string, object?> dict, string key, object? value)
        {
            var parts = key.Split(':');
            var current = dict;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current.ContainsKey(parts[i]) || current[parts[i]] is not Dictionary<string, object?> nested)
                {
                    nested = new Dictionary<string, object?>();
                    current[parts[i]] = nested;
                }
                else
                {
                    nested = (Dictionary<string, object?>)current[parts[i]]!;
                }

                current = nested;
            }

            current[parts[^1]] = value;
        }

        /// <inheritdoc/>
        public async Task ReloadAsync()
        {
            try
            {
                _logger.LogInformation("开始重新加载配置");

                await Task.Run(() =>
                {
                    InitializeConfigurations();
                });

                NotifyChange(new ConfigurationChangeEventArgs
                {
                    Key = "*",
                    Layer = ConfigurationLayer.All,
                    Timestamp = DateTime.Now
                });

                _statistics.ReloadCount++;
                _logger.LogInformation("配置重新加载完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新加载配置失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public IDisposable RegisterChangeCallback(Action<ConfigurationChangeEventArgs> callback)
        {
            var registration = new ConfigurationChangeCallback(callback);

            lock (_changeCallbacks)
            {
                _changeCallbacks.Add(registration);
            }

            return new CallbackDisposable(() =>
            {
                lock (_changeCallbacks)
                {
                    _changeCallbacks.Remove(registration);
                }
            });
        }

        /// <inheritdoc/>
        public ConfigurationLayerInfo GetLayerInfo(string key)
        {
            var info = new ConfigurationLayerInfo
            {
                Key = key,
                Layers = new List<LayerValue>()
            };

            foreach (var layer in Enum.GetValues<ConfigurationLayer>().OrderBy(l => (int)l))
            {
                if (_configurations.ContainsKey(layer))
                {
                    var value = _configurations[layer][key];
                    if (value != null)
                    {
                        info.Layers.Add(new LayerValue
                        {
                            Layer = layer,
                            Value = value,
                            Priority = (int)layer
                        });
                    }
                }
            }

            info.EffectiveValue = GetValue<string>(key);
            info.EffectiveLayer = info.Layers.LastOrDefault()?.Layer ?? ConfigurationLayer.Default;

            return info;
        }

        /// <inheritdoc/>
        public async Task<string> ExportConfigurationAsync(ConfigurationExportOptions options)
        {
            try
            {
                var exportData = new Dictionary<string, object>();

                if (options.IncludeDefaults)
                {
                    exportData["defaults"] = ExtractConfiguration(_configurations[ConfigurationLayer.Default]);
                }

                if (options.IncludeEnvironment)
                {
                    exportData["environment"] = ExtractConfiguration(_configurations[ConfigurationLayer.Environment]);
                }

                if (options.IncludeUser)
                {
                    exportData["user"] = ExtractConfiguration(_configurations[ConfigurationLayer.User]);
                }

                if (options.IncludeDynamic)
                {
                    exportData["dynamic"] = ExtractConfiguration(_configurations[ConfigurationLayer.Dynamic]);
                }

                if (options.IncludeMetadata)
                {
                    exportData["metadata"] = _metadata;
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(exportData, jsonOptions);

                if (!string.IsNullOrEmpty(options.FilePath))
                {
                    await File.WriteAllTextAsync(options.FilePath, json);
                }

                _logger.LogInformation("配置导出完成");
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出配置失败");
                throw;
            }
        }

        private Dictionary<string, string?> ExtractConfiguration(IConfiguration config)
        {
            var result = new Dictionary<string, string?>();

            foreach (var child in config.GetChildren())
            {
                ExtractSection(result, child);
            }

            return result;
        }

        private void ExtractSection(Dictionary<string, string?> result, IConfigurationSection section)
        {
            if (section.Value != null)
            {
                result[section.Path] = section.Value;
            }

            foreach (var child in section.GetChildren())
            {
                ExtractSection(result, child);
            }
        }

        /// <inheritdoc/>
        public async Task ImportConfigurationAsync(string configData, ConfigurationImportOptions options)
        {
            try
            {
                var importData = JsonSerializer.Deserialize<Dictionary<string, object>>(configData);
                if (importData == null)
                {
                    throw new InvalidOperationException("无效的配置数据");
                }

                if (options.BackupExisting)
                {
                    var backupPath = Path.Combine(_configPath, $"backup_{DateTime.Now:yyyyMMddHHmmss}.json");
                    await ExportConfigurationAsync(new ConfigurationExportOptions
                    {
                        IncludeDefaults = true,
                        IncludeEnvironment = true,
                        IncludeUser = true,
                        IncludeDynamic = true,
                        IncludeMetadata = true,
                        FilePath = backupPath
                    });
                }

                if (options.ClearExisting)
                {
                    // 清除现有配置
                    var userConfigFile = Path.Combine(_configPath, "user.config.json");
                    await File.WriteAllTextAsync(userConfigFile, "{}");
                }

                // 导入配置
                if (importData.ContainsKey("user") && options.ImportUser)
                {
                    var userConfig = JsonSerializer.Serialize(importData["user"], new JsonSerializerOptions { WriteIndented = true });
                    var userConfigFile = Path.Combine(_configPath, "user.config.json");
                    await File.WriteAllTextAsync(userConfigFile, userConfig);
                }

                // 重新加载
                await ReloadAsync();

                _logger.LogInformation("配置导入完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入配置失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new List<ValidationError>()
            };

            // 验证必需的配置项
            var requiredKeys = new[]
            {
                "Application:Name",
                "Application:Version",
                "API:BaseUrl",
                "Logging:LogLevel:Default"
            };

            foreach (var key in requiredKeys)
            {
                if (string.IsNullOrEmpty(GetValue<string>(key)))
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Key = key,
                        Message = $"必需的配置项 '{key}' 缺失或为空",
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            // 验证数值范围
            var timeout = GetValue<int>("API:Timeout");
            if (timeout < 1 || timeout > 300)
            {
                result.Errors.Add(new ValidationError
                {
                    Key = "API:Timeout",
                    Message = "API超时时间应在1-300秒之间",
                    Severity = ValidationSeverity.Warning
                });
            }

            // 验证枚举值
            var theme = GetValue<string>("UI:Theme");
            if (!new[] { "Light", "Dark", "Auto" }.Contains(theme))
            {
                result.Errors.Add(new ValidationError
                {
                    Key = "UI:Theme",
                    Message = "UI主题必须是 Light、Dark 或 Auto",
                    Severity = ValidationSeverity.Warning
                });
            }

            _logger.LogInformation(
                "配置验证完成，有效: {IsValid}, 错误数: {ErrorCount}",
                result.IsValid, result.Errors.Count);

            return result;
        }

        /// <inheritdoc/>
        public ConfigurationStatistics GetStatistics()
        {
            _statistics.TotalKeys = CountKeys(_mergedConfiguration);
            _statistics.LayerCount = _configurations.Count;
            return _statistics;
        }

        private int CountKeys(IConfiguration config)
        {
            int count = 0;
            foreach (var child in config.GetChildren())
            {
                if (child.Value != null)
                {
                    count++;
                }

                count += CountKeys(child);
            }

            return count;
        }

        #endregion 公共方法

        #region 文件监控

        private void SetupFileWatcher()
        {
            try
            {
                _fileWatcher = new FileSystemWatcher(_configPath)
                {
                    Filter = "*.json",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += OnConfigFileChanged;
                _fileWatcher.Created += OnConfigFileChanged;
                _fileWatcher.Deleted += OnConfigFileChanged;

                _logger.LogDebug("配置文件监控已启动");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置文件监控失败");
            }
        }

        private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // FileSystemWatcher事件处理器 - async void是合理用法
            try
            {
                // 防抖处理
                await Task.Delay(500);

                _logger.LogInformation("检测到配置文件变更: {FileName}", e.Name);

                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理配置文件变更失败");
            }
        }

        #endregion 文件监控

        #region 自动保存

        private void SetupAutoSave()
        {
            _autoSaveTimer = new Timer(AutoSave, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private async void AutoSave(object? state)
        {
            // Timer回调方法 - async void是合理用法
            try
            {
                // 自动保存动态配置到用户配置
                if (_statistics.WriteCount > 0)
                {
                    _logger.LogDebug("执行配置自动保存");

                    // 实现自动保存逻辑
                    await Task.CompletedTask; // Placeholder for async operation
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动保存配置失败");
            }
        }

        #endregion 自动保存

        #region 通知

        private void NotifyChange(ConfigurationChangeEventArgs args)
        {
            List<ConfigurationChangeCallback> callbacks;

            lock (_changeCallbacks)
            {
                callbacks = _changeCallbacks.ToList();
            }

            foreach (var callback in callbacks)
            {
                try
                {
                    callback.Invoke(args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "配置变更回调执行失败");
                }
            }
        }

        #endregion 通知

        #region IDisposable

        /// <inheritdoc/>
        public void Dispose()
        {
            _fileWatcher?.Dispose();
            _autoSaveTimer?.Dispose();

            _logger.LogInformation(
                "配置管理服务已关闭 - 读取: {Reads}, 写入: {Writes}, 重载: {Reloads}",
                _statistics.ReadCount, _statistics.WriteCount, _statistics.ReloadCount);
        }

        #endregion IDisposable
    }

    #region 辅助类

    /// <summary>
    /// 配置层级枚举
    /// </summary>
    public enum ConfigurationLayer
    {
        Default = 0,     // 默认配置（最低优先级）
        Environment = 1, // 环境配置
        User = 2,        // 用户配置
        Dynamic = 3,     // 动态配置（最高优先级）
        All = 99 // 所有层级
    }

    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigurationChangeEventArgs
    {
        public string Key { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public ConfigurationLayer Layer { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 配置层级信息
    /// </summary>
    public class ConfigurationLayerInfo
    {
        public string Key { get; set; } = string.Empty;
        public List<LayerValue> Layers { get; set; } = new();
        public string? EffectiveValue { get; set; }
        public ConfigurationLayer EffectiveLayer { get; set; }
    }

    /// <summary>
    /// 层级值
    /// </summary>
    public class LayerValue
    {
        public ConfigurationLayer Layer { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    /// <summary>
    /// 配置元数据
    /// </summary>
    public class ConfigurationMetadata
    {
        public string Key { get; set; } = string.Empty;
        public ConfigurationLayer Layer { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsOverridden { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }

    /// <summary>
    /// 配置导出选项
    /// </summary>
    public class ConfigurationExportOptions
    {
        public bool IncludeDefaults { get; set; } = true;
        public bool IncludeEnvironment { get; set; } = true;
        public bool IncludeUser { get; set; } = true;
        public bool IncludeDynamic { get; set; } = true;
        public bool IncludeMetadata { get; set; } = false;
        public bool IncludeAll => IncludeDefaults && IncludeEnvironment && IncludeUser && IncludeDynamic;
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// 配置导入选项
    /// </summary>
    public class ConfigurationImportOptions
    {
        public bool ImportUser { get; set; } = true;
        public bool ImportDynamic { get; set; } = false;
        public bool BackupExisting { get; set; } = true;
        public bool ClearExisting { get; set; } = false;
        public bool ValidateBeforeImport { get; set; } = true;
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string Key { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 配置统计
    /// </summary>
    public class ConfigurationStatistics
    {
        public int TotalKeys { get; set; }
        public int LayerCount { get; set; }
        public long ReadCount { get; set; }
        public long WriteCount { get; set; }
        public int ReloadCount { get; set; }
        public DateTime LastMergeTime { get; set; }
        public int MergeCount { get; set; }
    }

    /// <summary>
    /// 配置变更回调
    /// </summary>
    internal class ConfigurationChangeCallback
    {
        private readonly Action<ConfigurationChangeEventArgs> _callback;

        public ConfigurationChangeCallback(Action<ConfigurationChangeEventArgs> callback)
        {
            _callback = callback;
        }

        public void Invoke(ConfigurationChangeEventArgs args)
        {
            _callback(args);
        }
    }

    /// <summary>
    /// 回调释放器
    /// </summary>
    internal class CallbackDisposable : IDisposable
    {
        private readonly Action _disposeAction;

        public CallbackDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _disposeAction();
        }
    }

    #endregion 辅助类
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Configuration
{
    /// <summary>
    /// 配置热更新服务接口 - UltraThink Stage 5.3.2
    /// 提供配置动态更新、无需重启应用的能力
    /// </summary>
    public interface IHotReloadService
    {
        /// <summary>
        /// 启动热更新监控
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止热更新监控
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 注册热更新处理器
        /// </summary>
        void RegisterHandler(string configKey, Action<HotReloadEventArgs> handler);

        /// <summary>
        /// 注册异步热更新处理器
        /// </summary>
        void RegisterAsyncHandler(string configKey, Func<HotReloadEventArgs, Task> handler);

        /// <summary>
        /// 手动触发配置重载
        /// </summary>
        Task TriggerReloadAsync(string? configKey = null);

        /// <summary>
        /// 获取热更新状态
        /// </summary>
        HotReloadStatus GetStatus();

        /// <summary>
        /// 订阅配置变更事件
        /// </summary>
        IDisposable Subscribe(IObserver<ConfigurationChange> observer);

        /// <summary>
        /// 设置重载策略
        /// </summary>
        void SetReloadStrategy(ReloadStrategy strategy);

        /// <summary>
        /// 获取重载历史
        /// </summary>
        List<ReloadHistoryEntry> GetReloadHistory();
    }

    /// <summary>
    /// 配置热更新服务实现
    /// </summary>
    public class HotReloadService : IHotReloadService, IDisposable
    {
        private readonly ILogger<HotReloadService> _logger;
        private readonly IConfigurationManagerService _configService;
        private readonly IFeatureToggleService _featureToggleService;

        private readonly Dictionary<string, List<ConfigurationHandler>> _handlers = new();
        private readonly List<ReloadHistoryEntry> _history = new();
        private readonly Subject<ConfigurationChange> _changeSubject = new();

        private FileSystemWatcher? _configWatcher;
        private FileSystemWatcher? _featureWatcher;
        private Timer? _pollingTimer;
        private CancellationTokenSource? _cancellationTokenSource;

        private ReloadStrategy _reloadStrategy = ReloadStrategy.Automatic;
        private HotReloadStatus _status = HotReloadStatus.Stopped;
        private readonly object _lock = new object();

        // 配置缓存，用于检测变更
        private readonly Dictionary<string, object?> _configCache = new();
        private readonly Dictionary<string, bool> _featureCache = new();

        public HotReloadService(
            ILogger<HotReloadService> logger,
            IConfigurationManagerService configService,
            IFeatureToggleService featureToggleService)
        {
            _logger = logger;
            _configService = configService;
            _featureToggleService = featureToggleService;
        }

        #region 启动和停止

        public async Task StartAsync()
        {
            lock (_lock)
            {
                if (_status == HotReloadStatus.Running)
                {
                    _logger.LogWarning("热更新服务已在运行");
                    return;
                }

                _status = HotReloadStatus.Starting;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                // 初始化缓存
                await InitializeCacheAsync();

                // 设置文件监控
                SetupFileWatchers();

                // 设置轮询监控（作为备用方案）
                SetupPolling();

                // 注册配置变更回调
                RegisterConfigurationCallbacks();

                lock (_lock)
                {
                    _status = HotReloadStatus.Running;
                }

                _logger.LogInformation("配置热更新服务已启动");

                // 记录历史
                AddHistory(new ReloadHistoryEntry
                {
                    Timestamp = DateTime.Now,
                    Type = ReloadType.ServiceStart,
                    Description = "热更新服务启动"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动热更新服务失败");
                _status = HotReloadStatus.Error;
                throw;
            }
        }

        public Task StopAsync()
        {
            lock (_lock)
            {
                if (_status != HotReloadStatus.Running)
                {
                    return Task.CompletedTask;
                }

                _status = HotReloadStatus.Stopping;
            }

            try
            {
                _cancellationTokenSource?.Cancel();

                // 停止文件监控
                _configWatcher?.Dispose();
                _featureWatcher?.Dispose();
                _configWatcher = null;
                _featureWatcher = null;

                // 停止轮询
                _pollingTimer?.Dispose();
                _pollingTimer = null;

                lock (_lock)
                {
                    _status = HotReloadStatus.Stopped;
                }

                _logger.LogInformation("配置热更新服务已停止");

                // 记录历史
                AddHistory(new ReloadHistoryEntry
                {
                    Timestamp = DateTime.Now,
                    Type = ReloadType.ServiceStop,
                    Description = "热更新服务停止"
                });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止热更新服务失败");
                throw;
            }
        }

        #endregion

        #region 处理器注册

        public void RegisterHandler(string configKey, Action<HotReloadEventArgs> handler)
        {
            lock (_handlers)
            {
                if (!_handlers.ContainsKey(configKey))
                {
                    _handlers[configKey] = new List<ConfigurationHandler>();
                }

                _handlers[configKey].Add(new ConfigurationHandler
                {
                    Key = configKey,
                    SyncHandler = handler,
                    IsAsync = false
                });

                _logger.LogDebug("注册配置热更新处理器: {Key}", configKey);
            }
        }

        public void RegisterAsyncHandler(string configKey, Func<HotReloadEventArgs, Task> handler)
        {
            lock (_handlers)
            {
                if (!_handlers.ContainsKey(configKey))
                {
                    _handlers[configKey] = new List<ConfigurationHandler>();
                }

                _handlers[configKey].Add(new ConfigurationHandler
                {
                    Key = configKey,
                    AsyncHandler = handler,
                    IsAsync = true
                });

                _logger.LogDebug("注册异步配置热更新处理器: {Key}", configKey);
            }
        }

        #endregion

        #region 手动触发

        public async Task TriggerReloadAsync(string? configKey = null)
        {
            try
            {
                _logger.LogInformation("手动触发配置重载: {Key}", configKey ?? "全部");

                if (string.IsNullOrEmpty(configKey))
                {
                    // 重载所有配置
                    await _configService.ReloadAsync();
                    await CheckAndNotifyAllChangesAsync();
                }
                else
                {
                    // 重载特定配置
                    await CheckAndNotifyChangeAsync(configKey);
                }

                // 记录历史
                AddHistory(new ReloadHistoryEntry
                {
                    Timestamp = DateTime.Now,
                    Type = ReloadType.Manual,
                    ConfigKey = configKey,
                    Description = $"手动触发重载: {configKey ?? "全部配置"}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动触发配置重载失败");
                throw;
            }
        }

        #endregion

        #region 状态和订阅

        public HotReloadStatus GetStatus()
        {
            return _status;
        }

        public IDisposable Subscribe(IObserver<ConfigurationChange> observer)
        {
            return _changeSubject.Subscribe(observer);
        }

        public void SetReloadStrategy(ReloadStrategy strategy)
        {
            _reloadStrategy = strategy;
            _logger.LogInformation("配置重载策略已更改为: {Strategy}", strategy);
        }

        public List<ReloadHistoryEntry> GetReloadHistory()
        {
            lock (_history)
            {
                return _history.ToList();
            }
        }

        #endregion

        #region 私有方法

        private Task InitializeCacheAsync()
        {
            // 缓存当前配置值
            var commonKeys = new[]
            {
                "UI:Theme",
                "UI:Language",
                "UI:AnimationEnabled",
                "API:Timeout",
                "API:RetryCount",
                "Cache:DefaultExpiration",
                "Performance:SlowOperationThreshold",
                "Logging:LogLevel:Default"
            };

            foreach (var key in commonKeys)
            {
                _configCache[key] = _configService.GetValue<string>(key);
            }

            // 缓存特性状态
            var features = _featureToggleService.GetAllFeatures();
            foreach (var feature in features)
            {
                _featureCache[feature.Name] = feature.IsEnabled;
            }

            _logger.LogDebug("初始化配置缓存完成，缓存了 {ConfigCount} 个配置项和 {FeatureCount} 个特性",
                _configCache.Count, _featureCache.Count);

            return Task.CompletedTask;
        }

        private void SetupFileWatchers()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

                if (Directory.Exists(configPath))
                {
                    _configWatcher = new FileSystemWatcher(configPath)
                    {
                        Filter = "*.json",
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                        EnableRaisingEvents = true
                    };

                    _configWatcher.Changed += OnConfigFileChanged;
                    _configWatcher.Created += OnConfigFileChanged;
                    _configWatcher.Deleted += OnConfigFileChanged;

                    _logger.LogDebug("配置文件监控已设置: {Path}", configPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置文件监控失败");
            }
        }

        private void SetupPolling()
        {
            // 每30秒检查一次配置变更（作为文件监控的备用方案）
            _pollingTimer = new Timer(
                async _ => await PollForChangesAsync(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30));
        }

        private void RegisterConfigurationCallbacks()
        {
            // 注册配置管理器的变更回调
            _configService.RegisterChangeCallback(OnConfigurationChanged);

            // 注册关键特性的变更回调
            var criticalFeatures = new[] { "AdvancedCaching", "BatchOperations", "DetailedLogging" };
            foreach (var feature in criticalFeatures)
            {
                _featureToggleService.OnFeatureChanged(feature, OnFeatureChanged);
            }
        }

        private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // FileSystemWatcher事件处理器 - async void是合理用法
            try
            {
                // 防抖处理
                await Task.Delay(500);

                if (_reloadStrategy == ReloadStrategy.Manual)
                {
                    _logger.LogInformation("检测到配置文件变更，但当前策略为手动重载: {File}", e.Name);
                    return;
                }

                _logger.LogInformation("检测到配置文件变更，开始热更新: {File}", e.Name);

                await _configService.ReloadAsync();
                await CheckAndNotifyAllChangesAsync();

                // 记录历史
                AddHistory(new ReloadHistoryEntry
                {
                    Timestamp = DateTime.Now,
                    Type = ReloadType.FileChange,
                    ConfigKey = e.Name,
                    Description = $"文件变更触发: {e.Name}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理配置文件变更失败");
            }
        }

        private async Task PollForChangesAsync()
        {
            if (_status != HotReloadStatus.Running || _reloadStrategy == ReloadStrategy.Manual)
            {
                return;
            }

            try
            {
                await CheckAndNotifyAllChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮询检查配置变更失败");
            }
        }

        private async Task CheckAndNotifyAllChangesAsync()
        {
            // 检查配置变更
            foreach (var kvp in _configCache.ToList())
            {
                await CheckAndNotifyChangeAsync(kvp.Key);
            }

            // 检查特性变更
            var features = _featureToggleService.GetAllFeatures();
            foreach (var feature in features)
            {
                var wasEnabled = _featureCache.ContainsKey(feature.Name) && _featureCache[feature.Name];
                if (wasEnabled != feature.IsEnabled)
                {
                    _featureCache[feature.Name] = feature.IsEnabled;
                    await NotifyFeatureChangeAsync(feature.Name, wasEnabled, feature.IsEnabled);
                }
            }
        }

        private async Task CheckAndNotifyChangeAsync(string key)
        {
            var newValue = _configService.GetValue<object>(key);
            var oldValue = _configCache.ContainsKey(key) ? _configCache[key] : null;

            if (!Equals(oldValue, newValue))
            {
                _configCache[key] = newValue;
                await NotifyChangeAsync(key, oldValue, newValue);
            }
        }

        private async Task NotifyChangeAsync(string key, object? oldValue, object? newValue)
        {
            var args = new HotReloadEventArgs
            {
                ConfigKey = key,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = DateTime.Now,
                Source = ReloadSource.Configuration
            };

            // 发布到Observable
            _changeSubject.OnNext(new ConfigurationChange
            {
                Key = key,
                Value = newValue,
                Timestamp = args.Timestamp
            });

            // 调用注册的处理器
            await InvokeHandlersAsync(key, args);

            _logger.LogInformation("配置已热更新: {Key}, 旧值: {OldValue}, 新值: {NewValue}",
                key, oldValue, newValue);
        }

        private async Task NotifyFeatureChangeAsync(string featureName, bool wasEnabled, bool isEnabled)
        {
            var args = new HotReloadEventArgs
            {
                ConfigKey = $"Feature:{featureName}",
                OldValue = wasEnabled,
                NewValue = isEnabled,
                Timestamp = DateTime.Now,
                Source = ReloadSource.Feature
            };

            // 发布到Observable
            _changeSubject.OnNext(new ConfigurationChange
            {
                Key = args.ConfigKey,
                Value = isEnabled,
                Timestamp = args.Timestamp
            });

            // 调用处理器
            await InvokeHandlersAsync($"Feature:*", args);
            await InvokeHandlersAsync(args.ConfigKey, args);

            _logger.LogInformation("特性已热更新: {Feature}, 状态: {OldState} -> {NewState}",
                featureName, wasEnabled ? "启用" : "禁用", isEnabled ? "启用" : "禁用");
        }

        private async Task InvokeHandlersAsync(string key, HotReloadEventArgs args)
        {
            List<ConfigurationHandler>? handlers = null;

            lock (_handlers)
            {
                // 查找精确匹配的处理器
                if (_handlers.TryGetValue(key, out var exactHandlers))
                {
                    handlers = exactHandlers.ToList();
                }

                // 查找通配符处理器
                if (_handlers.TryGetValue("*", out var wildcardHandlers))
                {
                    handlers = handlers ?? new List<ConfigurationHandler>();
                    handlers.AddRange(wildcardHandlers);
                }
            }

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        if (handler.IsAsync && handler.AsyncHandler != null)
                        {
                            await handler.AsyncHandler(args);
                        }
                        else if (!handler.IsAsync && handler.SyncHandler != null)
                        {
                            handler.SyncHandler(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "执行热更新处理器失败: {Key}", handler.Key);
                    }
                }
            }
        }

        private void OnConfigurationChanged(ConfigurationChangeEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                await NotifyChangeAsync(args.Key, args.OldValue, args.NewValue);
            });
        }

        private void OnFeatureChanged(FeatureChangeEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                await NotifyFeatureChangeAsync(
                    args.FeatureName,
                    args.OldState == FeatureState.Enabled,
                    args.NewState == FeatureState.Enabled);
            });
        }

        private void AddHistory(ReloadHistoryEntry entry)
        {
            lock (_history)
            {
                _history.Add(entry);

                // 只保留最近100条记录
                while (_history.Count > 100)
                {
                    _history.RemoveAt(0);
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopAsync().Wait(TimeSpan.FromSeconds(5));

            _configWatcher?.Dispose();
            _featureWatcher?.Dispose();
            _pollingTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
            _changeSubject?.Dispose();

            _logger.LogInformation("热更新服务已释放，处理了 {Count} 次配置变更", _history.Count);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 热更新状态
    /// </summary>
    public enum HotReloadStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    /// <summary>
    /// 重载策略
    /// </summary>
    public enum ReloadStrategy
    {
        Automatic,  // 自动重载
        Manual,     // 手动重载
        Scheduled   // 定时重载
    }

    /// <summary>
    /// 重载类型
    /// </summary>
    public enum ReloadType
    {
        ServiceStart,
        ServiceStop,
        FileChange,
        Manual,
        Polling,
        Remote
    }

    /// <summary>
    /// 重载源
    /// </summary>
    public enum ReloadSource
    {
        Configuration,
        Feature,
        Security,
        Remote
    }

    /// <summary>
    /// 热更新事件参数
    /// </summary>
    public class HotReloadEventArgs
    {
        public string ConfigKey { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public ReloadSource Source { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 配置变更
    /// </summary>
    public class ConfigurationChange
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 重载历史条目
    /// </summary>
    public class ReloadHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public ReloadType Type { get; set; }
        public string? ConfigKey { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
    }

    /// <summary>
    /// 配置处理器
    /// </summary>
    internal class ConfigurationHandler
    {
        public string Key { get; set; } = string.Empty;
        public Action<HotReloadEventArgs>? SyncHandler { get; set; }
        public Func<HotReloadEventArgs, Task>? AsyncHandler { get; set; }
        public bool IsAsync { get; set; }
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Services.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Examples
{
    /// <summary>
    /// 配置管理示例ViewModel - UltraThink Stage 5.3.2 完整演示
    /// 
    /// 展示功能：
    /// 1. 分层配置管理
    /// 2. 特性开关系统
    /// 3. 配置热更新
    /// 4. 安全配置存储
    /// </summary>
    public class ConfigurationManagementExampleViewModel : BindableBase, IDisposable
    {
        #region 私有字段

        private readonly IConfigurationManagerService _configManager;
        private readonly IFeatureToggleService _featureToggle;
        private readonly IHotReloadService _hotReload;
        private readonly ISecureConfigurationService _secureConfig;
        private readonly ILogger<ConfigurationManagementExampleViewModel> _logger;
        
        private string _currentTheme = "Light";
        private string _currentLanguage = "zh-CN";
        private bool _animationsEnabled = true;
        private int _apiTimeout = 30;
        private int _cacheExpiration = 300;
        
        private string _selectedFeature = string.Empty;
        private bool _hotReloadEnabled = false;
        private string _configurationReport = string.Empty;
        
        private readonly List<IDisposable> _subscriptions = new();
        
        #endregion

        #region 构造函数

        public ConfigurationManagementExampleViewModel(
            IConfigurationManagerService configManager,
            IFeatureToggleService featureToggle,
            IHotReloadService hotReload,
            ISecureConfigurationService secureConfig,
            ILogger<ConfigurationManagementExampleViewModel> logger)
        {
            _configManager = configManager;
            _featureToggle = featureToggle;
            _hotReload = hotReload;
            _secureConfig = secureConfig;
            _logger = logger;
            
            InitializeCommands();
            LoadConfigurations();
            SetupHotReload();
            LoadFeatures();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前主题
        /// </summary>
        public string CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        /// <summary>
        /// 当前语言
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        /// <summary>
        /// 动画启用状态
        /// </summary>
        public bool AnimationsEnabled
        {
            get => _animationsEnabled;
            set => SetProperty(ref _animationsEnabled, value);
        }

        /// <summary>
        /// API超时时间
        /// </summary>
        public int ApiTimeout
        {
            get => _apiTimeout;
            set => SetProperty(ref _apiTimeout, value);
        }

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        public int CacheExpiration
        {
            get => _cacheExpiration;
            set => SetProperty(ref _cacheExpiration, value);
        }

        /// <summary>
        /// 选中的特性
        /// </summary>
        public string SelectedFeature
        {
            get => _selectedFeature;
            set => SetProperty(ref _selectedFeature, value);
        }

        /// <summary>
        /// 热更新启用状态
        /// </summary>
        public bool HotReloadEnabled
        {
            get => _hotReloadEnabled;
            set => SetProperty(ref _hotReloadEnabled, value);
        }

        /// <summary>
        /// 配置报告
        /// </summary>
        public string ConfigurationReport
        {
            get => _configurationReport;
            set => SetProperty(ref _configurationReport, value);
        }

        /// <summary>
        /// 配置层级信息
        /// </summary>
        public ObservableCollection<ConfigLayerDisplay> ConfigLayers { get; } = new();

        /// <summary>
        /// 特性列表
        /// </summary>
        public ObservableCollection<FeatureDisplay> Features { get; } = new();

        /// <summary>
        /// 配置变更历史
        /// </summary>
        public ObservableCollection<ConfigChangeDisplay> ChangeHistory { get; } = new();

        /// <summary>
        /// 安全配置列表
        /// </summary>
        public ObservableCollection<SecureConfigDisplay> SecureConfigs { get; } = new();

        #endregion

        #region 命令

        public ICommand SaveConfigurationCommand { get; private set; } = null!;
        public ICommand ReloadConfigurationCommand { get; private set; } = null!;
        public ICommand ToggleFeatureCommand { get; private set; } = null!;
        public ICommand StartHotReloadCommand { get; private set; } = null!;
        public ICommand StopHotReloadCommand { get; private set; } = null!;
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand ExportConfigurationCommand { get; private set; } = null!;
        public ICommand ImportConfigurationCommand { get; private set; } = null!;
        public ICommand ValidateConfigurationCommand { get; private set; } = null!;
        public ICommand AddSecureConfigCommand { get; private set; } = null!;
        public ICommand RemoveSecureConfigCommand { get; private set; } = null!;
        public ICommand RotateKeyCommand { get; private set; } = null!;
        public ICommand CheckIntegrityCommand { get; private set; } = null!;
        public ICommand SimulateChangeCommand { get; private set; } = null!;

        #endregion

        #region 初始化

        private void InitializeCommands()
        {
            SaveConfigurationCommand = new DelegateCommand(async () => await SaveConfigurationAsync());
            ReloadConfigurationCommand = new DelegateCommand(async () => await ReloadConfigurationAsync());
            ToggleFeatureCommand = new DelegateCommand<string>(async (name) => await ToggleFeatureAsync(name));
            StartHotReloadCommand = new DelegateCommand(async () => await StartHotReloadAsync());
            StopHotReloadCommand = new DelegateCommand(async () => await StopHotReloadAsync());
            GenerateReportCommand = new DelegateCommand(GenerateReport);
            ExportConfigurationCommand = new DelegateCommand(async () => await ExportConfigurationAsync());
            ImportConfigurationCommand = new DelegateCommand(async () => await ImportConfigurationAsync());
            ValidateConfigurationCommand = new DelegateCommand(ValidateConfiguration);
            AddSecureConfigCommand = new DelegateCommand(async () => await AddSecureConfigAsync());
            RemoveSecureConfigCommand = new DelegateCommand<string>(async (key) => await RemoveSecureConfigAsync(key));
            RotateKeyCommand = new DelegateCommand(async () => await RotateKeyAsync());
            CheckIntegrityCommand = new DelegateCommand(async () => await CheckIntegrityAsync());
            SimulateChangeCommand = new DelegateCommand(SimulateConfigurationChange);
        }

        private void LoadConfigurations()
        {
            // 加载当前配置值
            CurrentTheme = _configManager.GetValue<string>("UI:Theme", "Light")!;
            CurrentLanguage = _configManager.GetValue<string>("UI:Language", "zh-CN")!;
            AnimationsEnabled = _configManager.GetValue<bool>("UI:AnimationEnabled", true);
            ApiTimeout = _configManager.GetValue<int>("API:Timeout", 30);
            CacheExpiration = _configManager.GetValue<int>("Cache:DefaultExpiration", 300);
            
            // 加载配置层级信息
            UpdateConfigLayers();
            
            // 加载安全配置
            LoadSecureConfigs();
            
            _logger.LogInformation("配置已加载");
        }

        private void SetupHotReload()
        {
            // 注册热更新处理器
            _hotReload.RegisterHandler("UI:Theme", OnThemeChanged);
            _hotReload.RegisterHandler("UI:Language", OnLanguageChanged);
            _hotReload.RegisterHandler("UI:AnimationEnabled", OnAnimationSettingChanged);
            
            // 订阅配置变更
            var subscription = _hotReload.Subscribe(new ConfigurationObserver(this));
            _subscriptions.Add(subscription);
            
            // 注册配置管理器的变更回调
            var configCallback = _configManager.RegisterChangeCallback(OnConfigurationChanged);
            _subscriptions.Add(configCallback);
        }

        private void LoadFeatures()
        {
            Features.Clear();
            
            var allFeatures = _featureToggle.GetAllFeatures();
            foreach (var feature in allFeatures)
            {
                Features.Add(new FeatureDisplay
                {
                    Name = feature.Name,
                    DisplayName = feature.DisplayName,
                    Category = feature.Category,
                    IsEnabled = feature.IsEnabled,
                    State = feature.State.ToString(),
                    Stage = feature.ReleaseStage.ToString(),
                    Usage = feature.Usage
                });
            }
            
            _logger.LogDebug("加载了 {Count} 个特性开关", Features.Count);
        }

        private void LoadSecureConfigs()
        {
            SecureConfigs.Clear();
            
            var keys = _secureConfig.GetSecureKeys();
            foreach (var key in keys)
            {
                SecureConfigs.Add(new SecureConfigDisplay
                {
                    Key = key,
                    HasValue = true,
                    LastModified = DateTime.Now // 实际应该从配置中获取
                });
            }
        }

        #endregion

        #region 命令实现

        private async Task SaveConfigurationAsync()
        {
            try
            {
                // 保存UI配置
                await _configManager.SetValueAsync("UI:Theme", CurrentTheme);
                await _configManager.SetValueAsync("UI:Language", CurrentLanguage);
                await _configManager.SetValueAsync("UI:AnimationEnabled", AnimationsEnabled);
                
                // 保存API配置
                await _configManager.SetValueAsync("API:Timeout", ApiTimeout);
                
                // 保存缓存配置
                await _configManager.SetValueAsync("Cache:DefaultExpiration", CacheExpiration);
                
                AddChangeHistory("配置已保存", "用户手动保存");
                _logger.LogInformation("配置保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
            }
        }

        private async Task ReloadConfigurationAsync()
        {
            try
            {
                await _configManager.ReloadAsync();
                LoadConfigurations();
                
                AddChangeHistory("配置已重载", "用户手动重载");
                _logger.LogInformation("配置重载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重载配置失败");
            }
        }

        private async Task ToggleFeatureAsync(string? featureName)
        {
            if (string.IsNullOrEmpty(featureName))
                return;
            
            try
            {
                var feature = Features.FirstOrDefault(f => f.Name == featureName);
                if (feature != null)
                {
                    var newState = feature.IsEnabled ? FeatureState.Disabled : FeatureState.Enabled;
                    await _featureToggle.SetFeatureStateAsync(featureName, newState);
                    
                    feature.IsEnabled = !feature.IsEnabled;
                    feature.State = newState.ToString();
                    
                    AddChangeHistory($"特性 {featureName}", feature.IsEnabled ? "已启用" : "已禁用");
                    _logger.LogInformation("特性 {Feature} 状态已更改为 {State}", featureName, newState);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换特性状态失败");
            }
        }

        private async Task StartHotReloadAsync()
        {
            try
            {
                await _hotReload.StartAsync();
                HotReloadEnabled = true;
                
                AddChangeHistory("热更新已启动", "监控配置文件变更");
                _logger.LogInformation("配置热更新已启动");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动热更新失败");
            }
        }

        private async Task StopHotReloadAsync()
        {
            try
            {
                await _hotReload.StopAsync();
                HotReloadEnabled = false;
                
                AddChangeHistory("热更新已停止", "停止监控");
                _logger.LogInformation("配置热更新已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止热更新失败");
            }
        }

        private void GenerateReport()
        {
            var stats = _configManager.GetStatistics();
            var hotReloadStatus = _hotReload.GetStatus();
            var reloadHistory = _hotReload.GetReloadHistory();
            
            ConfigurationReport = $@"
══════════════════════════════════════════
         配置管理系统报告
    {DateTime.Now:yyyy-MM-dd HH:mm:ss}
══════════════════════════════════════════

【配置统计】
• 配置项总数: {stats.TotalKeys}
• 配置层级数: {stats.LayerCount}
• 读取次数: {stats.ReadCount:N0}
• 写入次数: {stats.WriteCount:N0}
• 重载次数: {stats.ReloadCount}
• 最后合并: {stats.LastMergeTime:HH:mm:ss}

【当前配置】
• UI主题: {CurrentTheme}
• 语言: {CurrentLanguage}
• 动画: {(AnimationsEnabled ? "启用" : "禁用")}
• API超时: {ApiTimeout}秒
• 缓存过期: {CacheExpiration}秒

【特性开关】
{string.Join("\n", Features.Select(f => 
    $"• {f.DisplayName}: {(f.IsEnabled ? "✓启用" : "✗禁用")} [{f.Stage}]"))}

【热更新状态】
• 状态: {hotReloadStatus}
• 最近重载: {reloadHistory.LastOrDefault()?.Timestamp:HH:mm:ss}
• 重载历史: {reloadHistory.Count}次

【安全配置】
• 安全配置数: {SecureConfigs.Count}
• 加密算法: AES-256
• 密钥派生: PBKDF2-SHA256

【配置层级】（优先级从低到高）
1. 默认配置 (内置)
2. 环境配置 (Development/Production)
3. 用户配置 (可修改)
4. 动态配置 (运行时)

══════════════════════════════════════════
";
            
            AddChangeHistory("报告已生成", "查看配置系统状态");
            _logger.LogInformation("配置报告已生成");
        }

        private async Task ExportConfigurationAsync()
        {
            try
            {
                var exportOptions = new ConfigurationExportOptions
                {
                    IncludeAll = true,
                    IncludeMetadata = true
                };
                
                var exportData = await _configManager.ExportConfigurationAsync(exportOptions);
                
                // 实际应该保存到文件或显示对话框
                AddChangeHistory("配置已导出", $"大小: {exportData.Length} 字符");
                _logger.LogInformation("配置导出成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出配置失败");
            }
        }

        private async Task ImportConfigurationAsync()
        {
            try
            {
                // 实际应该从文件选择对话框获取
                var importData = "{}"; // 示例数据
                
                var importOptions = new ConfigurationImportOptions
                {
                    BackupExisting = true,
                    ValidateBeforeImport = true
                };
                
                await _configManager.ImportConfigurationAsync(importData, importOptions);
                
                LoadConfigurations();
                AddChangeHistory("配置已导入", "从文件导入");
                _logger.LogInformation("配置导入成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入配置失败");
            }
        }

        private void ValidateConfiguration()
        {
            var result = _configManager.ValidateConfiguration();
            
            if (result.IsValid)
            {
                AddChangeHistory("配置验证通过", "所有必需配置项有效");
                _logger.LogInformation("配置验证通过");
            }
            else
            {
                var errors = string.Join("\n", result.Errors.Select(e => $"• {e.Key}: {e.Message}"));
                AddChangeHistory("配置验证失败", $"发现 {result.Errors.Count} 个问题");
                _logger.LogWarning("配置验证失败:\n{Errors}", errors);
            }
        }

        private async Task AddSecureConfigAsync()
        {
            try
            {
                // 示例：添加数据库连接字符串
                var key = "Database:ConnectionString";
                var value = "Server=localhost;Database=LYBTDB;Trusted_Connection=true;";
                
                await _secureConfig.SetSecureValueAsync(key, value);
                
                LoadSecureConfigs();
                AddChangeHistory($"安全配置已添加: {key}", "加密存储");
                _logger.LogInformation("安全配置已添加: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加安全配置失败");
            }
        }

        private async Task RemoveSecureConfigAsync(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            
            try
            {
                await _secureConfig.RemoveSecureValueAsync(key);
                
                LoadSecureConfigs();
                AddChangeHistory($"安全配置已删除: {key}", "永久删除");
                _logger.LogInformation("安全配置已删除: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除安全配置失败");
            }
        }

        private async Task RotateKeyAsync()
        {
            try
            {
                // 实际应该通过对话框获取新密码
                await _secureConfig.RotateEncryptionKeyAsync("", "NewSecurePassword123!");
                
                AddChangeHistory("加密密钥已轮换", "安全更新");
                _logger.LogInformation("加密密钥轮换成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密钥轮换失败");
            }
        }

        private async Task CheckIntegrityAsync()
        {
            try
            {
                var result = await _secureConfig.VerifyIntegrityAsync();
                
                if (result.IsValid)
                {
                    AddChangeHistory("完整性检查通过", $"{result.ValidConfigs}/{result.TotalConfigs} 配置有效");
                }
                else
                {
                    AddChangeHistory("完整性检查失败", $"发现 {result.Issues.Count} 个问题");
                }
                
                _logger.LogInformation("完整性检查完成: {Valid}", result.IsValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完整性检查失败");
            }
        }

        private void SimulateConfigurationChange()
        {
            // 模拟配置变更
            var themes = new[] { "Light", "Dark", "Auto" };
            var newTheme = themes[new Random().Next(themes.Length)];
            
            CurrentTheme = newTheme;
            _ = _configManager.SetValueAsync("UI:Theme", newTheme);
            
            AddChangeHistory($"主题变更为 {newTheme}", "模拟变更");
            _logger.LogInformation("模拟配置变更: Theme = {Theme}", newTheme);
        }

        #endregion

        #region 热更新处理器

        private void OnThemeChanged(HotReloadEventArgs args)
        {
            CurrentTheme = args.NewValue?.ToString() ?? "Light";
            AddChangeHistory($"主题热更新: {args.OldValue} → {args.NewValue}", "自动应用");
        }

        private void OnLanguageChanged(HotReloadEventArgs args)
        {
            CurrentLanguage = args.NewValue?.ToString() ?? "zh-CN";
            AddChangeHistory($"语言热更新: {args.OldValue} → {args.NewValue}", "自动应用");
        }

        private void OnAnimationSettingChanged(HotReloadEventArgs args)
        {
            AnimationsEnabled = Convert.ToBoolean(args.NewValue);
            AddChangeHistory($"动画设置热更新: {(AnimationsEnabled ? "启用" : "禁用")}", "自动应用");
        }

        private void OnConfigurationChanged(ConfigurationChangeEventArgs args)
        {
            AddChangeHistory($"配置变更: {args.Key}", $"层级: {args.Layer}");
            UpdateConfigLayers();
        }

        #endregion

        #region 辅助方法

        private void UpdateConfigLayers()
        {
            ConfigLayers.Clear();
            
            var commonKeys = new[] { "UI:Theme", "API:Timeout", "Cache:DefaultExpiration" };
            
            foreach (var key in commonKeys)
            {
                var layerInfo = _configManager.GetLayerInfo(key);
                
                ConfigLayers.Add(new ConfigLayerDisplay
                {
                    Key = key,
                    EffectiveValue = layerInfo.EffectiveValue ?? "(null)",
                    EffectiveLayer = layerInfo.EffectiveLayer.ToString(),
                    LayerValues = string.Join(" → ", 
                        layerInfo.Layers.OrderBy(l => l.Priority).Select(l => $"{l.Layer}: {l.Value}"))
                });
            }
        }

        private void AddChangeHistory(string change, string details)
        {
            ChangeHistory.Insert(0, new ConfigChangeDisplay
            {
                Timestamp = DateTime.Now,
                Change = change,
                Details = details
            });
            
            // 限制历史记录数量
            while (ChangeHistory.Count > 50)
            {
                ChangeHistory.RemoveAt(ChangeHistory.Count - 1);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            
            _logger.LogInformation("配置管理示例已关闭");
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 配置变更观察者
        /// </summary>
        private class ConfigurationObserver : IObserver<ConfigurationChange>
        {
            private readonly ConfigurationManagementExampleViewModel _viewModel;
            
            public ConfigurationObserver(ConfigurationManagementExampleViewModel viewModel)
            {
                _viewModel = viewModel;
            }
            
            public void OnNext(ConfigurationChange value)
            {
                _viewModel.AddChangeHistory($"热更新: {value.Key}", $"新值: {value.Value}");
            }
            
            public void OnError(Exception error)
            {
                _viewModel._logger.LogError(error, "配置观察者错误");
            }
            
            public void OnCompleted()
            {
                _viewModel._logger.LogInformation("配置观察完成");
            }
        }

        #endregion
    }

    #region 显示模型

    /// <summary>
    /// 配置层级显示
    /// </summary>
    public class ConfigLayerDisplay
    {
        public string Key { get; set; } = string.Empty;
        public string EffectiveValue { get; set; } = string.Empty;
        public string EffectiveLayer { get; set; } = string.Empty;
        public string LayerValues { get; set; } = string.Empty;
    }

    /// <summary>
    /// 特性显示
    /// </summary>
    public class FeatureDisplay : BindableBase
    {
        private bool _isEnabled;
        
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        
        public string State { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public long Usage { get; set; }
    }

    /// <summary>
    /// 配置变更显示
    /// </summary>
    public class ConfigChangeDisplay
    {
        public DateTime Timestamp { get; set; }
        public string Change { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// 安全配置显示
    /// </summary>
    public class SecureConfigDisplay
    {
        public string Key { get; set; } = string.Empty;
        public bool HasValue { get; set; }
        public DateTime LastModified { get; set; }
    }

    #endregion
}
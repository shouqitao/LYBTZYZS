using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Infrastructure.Extensions;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 状态栏管理器 - 负责 API 健康状态、连接地址、连接模式的显示
/// 从 MainWindowViewModel 提取，减少主 VM 行数
/// </summary>
public partial class StatusBarManager : ObservableObject, IStatusBarManager, IDisposable
{
    private readonly IApiHealthMonitor _apiHealthMonitor;
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly IConnectionModeService _connectionModeService;
    private readonly IUiThreadDispatcher _uiThreadDispatcher;
    private readonly ILogger<StatusBarManager> _logger;

    [ObservableProperty]
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;

    [ObservableProperty]
    private string _connectionUrl = "http://127.0.0.1:5300";

    [ObservableProperty]
    private bool _isLocal;

    [ObservableProperty]
    private string _connectionModeDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRemoteMode;

    [ObservableProperty]
    private string _currentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public PackIconKind ApiStatusIcon => ApiStatus switch
    {
        ApiHealthStatus.Healthy => PackIconKind.Wifi,
        ApiHealthStatus.Unhealthy => PackIconKind.WifiOff,
        _ => PackIconKind.WifiStrengthAlertOutline
    };

    public Brush ApiStatusColor => ApiStatus switch
    {
        ApiHealthStatus.Healthy => Brushes.Green,
        ApiHealthStatus.Unhealthy => Brushes.Orange,
        _ => Brushes.Gray
    };

    public StatusBarManager(
        IApiHealthMonitor apiHealthMonitor,
        IConnectionSettingsService connectionSettings,
        IConnectionModeService connectionModeService,
        IUiThreadDispatcher uiThreadDispatcher,
        ILogger<StatusBarManager> logger)
    {
        _apiHealthMonitor = apiHealthMonitor ?? throw new ArgumentNullException(nameof(apiHealthMonitor));
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _connectionModeService = connectionModeService ?? throw new ArgumentNullException(nameof(connectionModeService));
        _uiThreadDispatcher = uiThreadDispatcher ?? throw new ArgumentNullException(nameof(uiThreadDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConnectionUrl = _connectionSettings.CurrentUrl;
        IsLocal = _connectionSettings.IsLocal;
        ConnectionModeDisplay = _connectionModeService.CurrentModeDisplay;
        IsRemoteMode = _connectionModeService.IsRemote;

        Initialize();
    }

    private void Initialize()
    {
        _apiHealthMonitor.StatusChanged += OnHealthStatusChanged;
        _apiHealthMonitor.StartMonitoringAsync().SafeFireAndForget(ex => _logger.LogError(ex, "启动健康监控失败"));
        _connectionSettings.UrlChanged += OnConnectionUrlChanged;
        _connectionModeService.ModeChanged += OnConnectionModeChanged;
    }

    private void OnHealthStatusChanged(object? sender, ApiHealthMonitorChangedEventArgs e)
    {
        var apiStatus = e.NewStatus switch
        {
            ApiMonitorHealthStatus.Healthy => ApiHealthStatus.Healthy,
            ApiMonitorHealthStatus.Unhealthy => ApiHealthStatus.Unhealthy,
            _ => ApiHealthStatus.Checking
        };
        _uiThreadDispatcher.InvokeAsync(() => ApiStatus = apiStatus);
    }

    private void OnConnectionUrlChanged(object? sender, string newUrl)
    {
        _uiThreadDispatcher.InvokeAsync(() =>
        {
            ConnectionUrl = newUrl;
            IsLocal = _connectionSettings.IsLocal;
            _logger.LogInformation("[UI] 连接地址变更: {Url}", newUrl);
        });
    }

    private void OnConnectionModeChanged(object? sender, ConnectionMode e)
    {
        _uiThreadDispatcher.InvokeAsync(() =>
        {
            ConnectionModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            _logger.LogInformation("[UI] 连接模式变更: {Mode} ({Display})", e, _connectionModeService.CurrentModeDisplay);
        });
    }

    public void UpdateTime()
    {
        _uiThreadDispatcher.InvokeAsync(() =>
        {
            CurrentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        });
    }

    public async Task ForceCheckAsync()
    {
        _logger.LogInformation("用户手动触发 API 健康检查");
        await _apiHealthMonitor.ForceCheckAsync();
    }

    public void Dispose()
    {
        try
        {
            _apiHealthMonitor.StatusChanged -= OnHealthStatusChanged;
            _connectionSettings.UrlChanged -= OnConnectionUrlChanged;
            _connectionModeService.ModeChanged -= OnConnectionModeChanged;
            _apiHealthMonitor.Dispose();
        }
        catch (Exception ex) { _logger.LogError(ex, "清理状态栏管理器失败"); }
    }
}

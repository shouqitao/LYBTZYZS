using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 连接状态 UI — API 健康状态、连接模式显示、模式切换
/// 从 LoginViewModel 提取，单一职责：连接状态管理
/// </summary>
public partial class ConnectionStatusViewModel : NavigableViewModelBase
{
    private readonly IApplicationStateService _applicationStateService;
    private readonly IConnectionModeService? _connectionModeService;
    private readonly IConnectionSettingsService? _connectionSettingsService;

    [ObservableProperty]
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;

    [ObservableProperty]
    private string _apiStatusMessage = "正在检查连接...";

    [ObservableProperty]
    private string _currentModeDisplay = "检测中...";

    [ObservableProperty]
    private bool _isRemoteMode;

    [ObservableProperty]
    private bool _isRemoteAvailable;

    [ObservableProperty]
    private string _currentServerUrl = string.Empty;

    public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

    public ConnectionStatusViewModel(
        IViewModelServices services,
        IApplicationStateService applicationStateService,
        IConnectionModeService? connectionModeService,
        IConnectionSettingsService? connectionSettingsService)
        : base(services)
    {
        _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
        _connectionModeService = connectionModeService;
        _connectionSettingsService = connectionSettingsService;

        _applicationStateService.StatusChanged += OnApiStatusChanged;

        if (_connectionModeService != null)
        {
            _connectionModeService.ModeChanged += OnConnectionModeChanged;
            CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            IsRemoteAvailable = _connectionModeService.IsRemoteAvailable;
        }

        UpdateServerUrl();
    }

    /// <summary>
    /// 同步当前连接 URL 到 UI 属性。
    /// </summary>
    private void UpdateServerUrl()
    {
        if (_connectionSettingsService is not null && _connectionModeService is not null)
        {
            // 直接按当前模式读取 URL，避免 _preferredMode 与实际模式不同步
            CurrentServerUrl = _connectionModeService.IsRemote
                ? _connectionSettingsService.RemoteUrl
                : _connectionSettingsService.LocalUrl;
        }
        else if (_connectionSettingsService is not null)
        {
            CurrentServerUrl = _connectionSettingsService.CurrentUrl;
        }
    }

    /// <summary>
    /// 加载 API 状态
    /// </summary>
    public async Task LoadApiStatusAsync()
    {
        try
        {
            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                if (_applicationStateService.IsApiHealthy)
                {
                    ApiStatus = ApiHealthStatus.Healthy;
                    ApiStatusMessage = "WebAPI 已连接";
                }
                else
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}";
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.LoadApiStatus failed");
            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                ApiStatus = ApiHealthStatus.Unhealthy;
                ApiStatusMessage = "加载API状态失败，请稍后重试";
            });
        }
    }

    /// <summary>
    /// 刷新连接模式显示与远程可用性（仅 UI 状态，不自动切换模式）
    /// </summary>
    public async Task DetectConnectionModeAsync()
    {
        if (_connectionModeService is null) return;

        try
        {
            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
            });

            var remoteAvailable = await _connectionModeService.CheckRemoteAvailableAsync();

            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                IsRemoteAvailable = remoteAvailable;
                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                Logger.LogInformation("[VM] Login.DetectMode - 连接模式: {Display}, 远程可用: {RemoteAvailable}",
                    _connectionModeService.CurrentModeDisplay, remoteAvailable);
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.DetectMode failed");
        }
    }

    /// <summary>
    /// 切换到本地模式
    /// </summary>
    [RelayCommand]
    private async Task SwitchToLocal()
    {
        if (_connectionModeService is null)
        {
            Logger.LogWarning("[VM] Login.SwitchToLocal - IConnectionModeService 未注入");
            return;
        }

        try
        {
            Logger.LogInformation("[VM] Login.SwitchToLocal → 本地模式");
            var result = await _connectionModeService.SetModeAsync(ConnectionMode.Local).ConfigureAwait(true);

            if (!result.Succeeded)
            {
                Logger.LogWarning("[VM] Login.SwitchToLocal blocked - {ErrorCode}: {Message}", result.ErrorCode, result.Message);
                ShowSwitchBlockedMessage(result);
                return;
            }

            SyncModeDisplay();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.SwitchToLocal failed");
        }
    }

    /// <summary>
    /// 切换到远程模式
    /// </summary>
    [RelayCommand]
    private async Task SwitchToRemote()
    {
        if (_connectionModeService is null)
        {
            Logger.LogWarning("[VM] Login.SwitchToRemote - IConnectionModeService 未注入");
            return;
        }

        try
        {
            Logger.LogInformation("[VM] Login.SwitchToRemote → 远程模式");
            var switchResult = await _connectionModeService.SetModeAsync(ConnectionMode.Remote).ConfigureAwait(true);

            if (!switchResult.Succeeded)
            {
                Logger.LogWarning("[VM] Login.SwitchToRemote blocked - {ErrorCode}: {Message}", switchResult.ErrorCode, switchResult.Message);
                ShowSwitchBlockedMessage(switchResult);
                return;
            }

            SyncModeDisplay();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.SwitchToRemote failed");
        }
    }

    /// <summary>
    /// 将连接模式相关 UI 属性与 <see cref="IConnectionModeService"/> 当前状态对齐。
    /// 先同步刷新立即状态（模式显示/当前模式——按钮可见性依赖 IsRemoteMode，立即生效），
    /// 再异步探测远程可用性（按钮可用性依赖 IsRemoteAvailable，后台刷新，不阻塞切换响应）。
    /// 状态单一事实来源仍在服务层（探测经 CheckRemoteAvailableAsync 更新服务缓存）。
    /// async void：被命令/事件处理器以 fire-and-forget 方式调用，探测异常必须就地捕获。
    /// </summary>
    private async void SyncModeDisplay()
    {
        if (_connectionModeService is null) return;

        // 1. 立即同步状态：模式显示、当前模式（按钮可见性依赖 IsRemoteMode，立即生效）
        CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
        IsRemoteMode = _connectionModeService.IsRemote;
        ApiStatusMessage = _connectionModeService.ApiStatusDisplay;
        UpdateServerUrl();
        SwitchToRemoteCommand.NotifyCanExecuteChanged();

        // 2. 异步探测远程可用性（按钮可用性依赖 IsRemoteAvailable，后台刷新）
        try
        {
            var remoteAvailable = await _connectionModeService.CheckRemoteAvailableAsync().ConfigureAwait(true);
            IsRemoteAvailable = remoteAvailable;
            SwitchToRemoteCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.SyncModeDisplay - remote availability probe failed");
        }
    }

    /// <summary>
    /// 切换被守卫阻断时的用户提示（B2 US-SHELL-007: ERR-70506 等）
    /// </summary>
    private static void ShowSwitchBlockedMessage(ModeSwitchResult result)
    {
        System.Windows.MessageBox.Show(
            result.Message ?? "切换失败",
            "无法切换模式",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }

    /// <summary>
    /// 重试 API 健康检查
    /// </summary>
    [RelayCommand]
    private async Task RetryApiCheckAsync()
    {
        try
        {
            ApiStatus = ApiHealthStatus.Checking;
            ApiStatusMessage = "正在检查连接...";

            await _applicationStateService.CheckApiHealthAsync();

            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                if (_applicationStateService.IsApiHealthy)
                {
                    ApiStatus = ApiHealthStatus.Healthy;
                    ApiStatusMessage = "WebAPI 已连接";
                }
                else
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"WebAPI 连接失败: {_applicationStateService.ConnectionStatus}";
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.RetryApiCheck failed");
            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                ApiStatus = ApiHealthStatus.Unhealthy;
                ApiStatusMessage = "连接检查失败，请稍后重试";
            });
        }
    }

    private void OnApiStatusChanged(object? sender, ApiStatusChangedEventArgs e)
    {
        try
        {
            Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                if (e.IsHealthy)
                {
                    ApiStatus = ApiHealthStatus.Healthy;
                    ApiStatusMessage = "WebAPI 已连接";
                }
                else
                {
                    ApiStatus = ApiHealthStatus.Unhealthy;
                    ApiStatusMessage = $"WebAPI 连接失败: {e.LastError ?? e.ConnectionStatus}";
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.OnApiStatusChanged failed");
        }
    }

    private void OnConnectionModeChanged(object? sender, ConnectionMode e)
    {
        try
        {
            if (_connectionModeService is null) return;
            Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                // 模式切换（含设置对话框/首次运行向导发起）后，UI 立即反映新状态——
                // 含状态文案 ApiStatusMessage，避免与实际连接不一致
                SyncModeDisplay();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.OnConnectionModeChanged failed");
        }
    }

    protected override void OnDisposing()
    {
        _applicationStateService.StatusChanged -= OnApiStatusChanged;

        if (_connectionModeService != null)
        {
            _connectionModeService.ModeChanged -= OnConnectionModeChanged;
        }

        base.OnDisposing();
    }
}

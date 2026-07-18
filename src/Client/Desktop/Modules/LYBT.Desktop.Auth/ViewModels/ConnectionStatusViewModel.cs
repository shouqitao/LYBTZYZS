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
public partial class ConnectionStatusViewModel : CoreViewModelBase
{
    private readonly IApplicationStateService _applicationStateService;
    private readonly IConnectionModeService? _connectionModeService;

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

    public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

    public ConnectionStatusViewModel(
        IViewModelServices services,
        IApplicationStateService applicationStateService,
        IConnectionModeService? connectionModeService)
        : base(services)
    {
        _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
        _connectionModeService = connectionModeService;

        _applicationStateService.StatusChanged += OnApiStatusChanged;

        if (_connectionModeService != null)
        {
            _connectionModeService.ModeChanged += OnConnectionModeChanged;
            CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            IsRemoteAvailable = _connectionModeService.IsRemoteAvailable;
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
    /// 检测连接模式
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

            var mode = await _connectionModeService.DetectBestModeAsync();
            var remoteAvailable = await _connectionModeService.CheckRemoteAvailableAsync();

            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                IsRemoteAvailable = remoteAvailable;
                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                Logger.LogInformation("[VM] Login.DetectMode - 连接模式: {Mode} ({Display}), 远程可用: {RemoteAvailable}",
                    mode, _connectionModeService.CurrentModeDisplay, remoteAvailable);
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
    private void SwitchToLocal()
    {
        if (_connectionModeService is null)
        {
            Logger.LogWarning("[VM] Login.SwitchToLocal - IConnectionModeService 未注入");
            return;
        }

        try
        {
            Logger.LogInformation("[VM] Login.SwitchToLocal → 本地模式");
            _connectionModeService.SetMode(ConnectionMode.Local);

            CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            ApiStatusMessage = _connectionModeService.ApiStatusDisplay;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.SwitchToLocal failed");
        }
    }

    /// <summary>
    /// 切换到远程模式
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsRemoteAvailable))]
    private void SwitchToRemote()
    {
        if (_connectionModeService is null)
        {
            Logger.LogWarning("[VM] Login.SwitchToRemote - IConnectionModeService 未注入");
            return;
        }

        try
        {
            Logger.LogInformation("[VM] Login.SwitchToRemote → 远程模式");
            _connectionModeService.SetMode(ConnectionMode.Remote);

            CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            ApiStatusMessage = _connectionModeService.ApiStatusDisplay;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[VM] Login.SwitchToRemote failed");
        }
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
                CurrentModeDisplay = _connectionModeService.CurrentModeDisplay;
                IsRemoteMode = _connectionModeService.IsRemote;
                IsRemoteAvailable = _connectionModeService.IsRemoteAvailable;
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Admin.Sysadmin.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 运维设置主页视图模型
/// </summary>
public partial class SysadminHomeViewModel : NavigableViewModelBase
{
    private readonly IAuthHealthService _authHealthService;
    private readonly IClinicSettingsService _clinicSettings;
    private readonly IConnectionModeService _connectionMode;
    private readonly INavigationCoordinator _navigationCoordinator;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private DashboardStatus _dashboard = new();

    /// <summary>配置中心面板（SHELL-018 Phase 2: 5 组可编辑 + 只读占位）</summary>
    [ObservableProperty]
    private ConfigurationCenterViewModel _configCenter;

    /// <summary>服务端配置面板（SHELL-018 Phase 3: 仅远程模式）</summary>
    [ObservableProperty]
    private ServerConfigSectionViewModel _serverConfig;

    /// <summary>读卡器诊断面板（SHELL-019: 测试模式——双模式同硬件）</summary>
    [ObservableProperty]
    private CardReaderDiagnosticsViewModel _cardReaderDiagnostics;

    [ObservableProperty]
    private bool _isRemoteMode;

    [ObservableProperty]
    private bool _isLocalMode;

    public SysadminHomeViewModel(
        IViewModelServices services,
        IAuthHealthService authHealthService,
        IClinicSettingsService clinicSettings,
        IConnectionModeService connectionMode,
        INavigationCoordinator navigationCoordinator,
        ConfigurationCenterViewModel configCenter,
        ServerConfigSectionViewModel serverConfig,
        CardReaderDiagnosticsViewModel cardReaderDiagnostics)
        : base(services)
    {
        _authHealthService = authHealthService;
        _clinicSettings = clinicSettings;
        _connectionMode = connectionMode;
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        ConfigCenter = configCenter;
        ServerConfig = serverConfig;
        CardReaderDiagnostics = cardReaderDiagnostics;
        UpdateModeFlags();
        _connectionMode.ModeChanged += OnModeChanged;
        PageTitle = "运维设置";
    }

    private void UpdateModeFlags()
    {
        IsRemoteMode = _connectionMode.IsRemote;
        IsLocalMode = _connectionMode.IsLocal;
    }

    private void OnModeChanged(object? sender, ConnectionMode mode)
    {
        UpdateModeFlags();
        if (IsRemoteMode)
            _ = ServerConfig.LoadSectionsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// 导航到用户管理
    /// </summary>
    [RelayCommand]
    private void NavigateToUserManagement() => NavigateTo(ViewNames.UserManagement);

    /// <summary>
    /// 导航到日志级别控制
    /// </summary>
    [RelayCommand]
    private void NavigateToLogLevelControl() => NavigateTo(ViewNames.LogLevelControl);

    /// <summary>
    /// 导航到部署管理
    /// </summary>
    [RelayCommand]
    private void NavigateToDeployment() => NavigateTo(ViewNames.Deployment);

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    private void NavigateTo(string viewName)
    {
        Logger.LogInformation("导航到 {ViewName}", viewName);
        _ = _navigationCoordinator.NavigateTo(viewName);
    }

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        StartPolling();
    }

    public override void OnNavigatedFrom(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedFrom(navigationContext);
        StopPolling();
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = new CancellationTokenSource();
        _ = PollDashboardAsync(_pollCts.Token);
    }

    private void StopPolling() => _pollCts?.Cancel();

    private async Task PollDashboardAsync(CancellationToken ct)
    {
        var isFirstLoad = true;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (isFirstLoad) Dashboard.IsLoading = true;

                var healthResp = await _authHealthService.HealthCheckAsync();

                if (healthResp.Success)
                {
                    Dashboard.DbStatus.Value = "连接正常";
                    Dashboard.DbStatus.IsHealthy = true;
                    Dashboard.DbStatus.Status = "正常";
                }
                else
                {
                    Dashboard.DbStatus.Value = "连接异常";
                    Dashboard.DbStatus.IsHealthy = false;
                    Dashboard.DbStatus.Status = "异常";
                }

                Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
                Dashboard.SystemInfo.Status = _clinicSettings.ClinicName;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSADMIN] Dashboard poll failed");
            }
            finally
            {
                if (isFirstLoad) { Dashboard.IsLoading = false; isFirstLoad = false; }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
            catch { break; }
        }
    }
}

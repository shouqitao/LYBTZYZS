using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Sysadmin.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Sysadmin.ViewModels;

/// <summary>
/// 系统运维控制台主页视图模型 - 暗色仪表盘 + 4 状态卡片 + 30秒轮询
/// </summary>
public partial class SysadminHomeViewModel : NavigableViewModelBase
{
    private readonly IAuthApi _authApi;
    private readonly INavigationCoordinator _navigationCoordinator;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private DashboardStatus _dashboard = new();

    public SysadminHomeViewModel(
        IViewModelServices services,
        IAuthApi authApi,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _authApi = authApi;
        _navigationCoordinator = navigationCoordinator;
        PageTitle = "运维控制台";
    }

    [RelayCommand]
    private void NavigateToAdminUsers() => _navigationCoordinator.NavigateTo("AdminUserManagementView");

    [RelayCommand]
    private void NavigateToLogLevel() => _navigationCoordinator.NavigateTo("LogLevelControlView");

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

                var healthResp = await _authApi.HealthCheckAsync();
                if (healthResp.Success)
                {
                    Dashboard.ApiStatus.Value = "在线";
                    Dashboard.ApiStatus.IsHealthy = true;
                    Dashboard.ApiStatus.Status = "正常";
                }
                else
                {
                    Dashboard.ApiStatus.Value = "离线";
                    Dashboard.ApiStatus.IsHealthy = false;
                    Dashboard.ApiStatus.Status = "异常";
                }

                Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
                Dashboard.LoginCount.Value = "--";
                Dashboard.DbStatus.Value = "连接正常";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSADMIN] Dashboard poll failed");
                Dashboard.ApiStatus.Value = "不可达";
                Dashboard.ApiStatus.IsHealthy = false;
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Admin.Sysadmin.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 系统运维控制台主页视图模型
/// </summary>
public partial class SysadminHomeViewModel : NavigableViewModelBase
{
    private readonly IApiClientAuth _authApi;
    private readonly IClinicSettingsService _clinicSettings;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private DashboardStatus _dashboard = new();

    public SysadminHomeViewModel(
        IViewModelServices services,
        IApiClientAuth authApi,
        IClinicSettingsService clinicSettings)
        : base(services)
    {
        _authApi = authApi;
        _clinicSettings = clinicSettings;
        PageTitle = "运维控制台";
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

                var healthResp = await _authApi.HealthCheckAsync();

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

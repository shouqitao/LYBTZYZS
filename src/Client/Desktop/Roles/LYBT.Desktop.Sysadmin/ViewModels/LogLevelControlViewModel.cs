using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Desktop.Sysadmin.ViewModels;

/// <summary>
/// 日志级别控制视图模型 - 调用 DiagnosticsController 实现运行时日志级别调整
/// </summary>
public partial class LogLevelControlViewModel : NavigableViewModelBase
{
    private readonly IDiagnosticsApi _diagnosticsApi;

    [ObservableProperty]
    private string _currentLevel = "加载中...";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public LogLevelControlViewModel(IViewModelServices services, IDiagnosticsApi diagnosticsApi)
        : base(services)
    {
        _diagnosticsApi = diagnosticsApi;
        PageTitle = "日志级别控制";
    }

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        _ = LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        try
        {
            IsBusy = true;
            var resp = await _diagnosticsApi.GetLoggingStatusAsync();
            if (resp.Success)
            {
                var json = JsonSerializer.Serialize(resp.Data);
                CurrentLevel = json.Contains("Debug", StringComparison.OrdinalIgnoreCase) ? "Debug" :
                               json.Contains("Verbose", StringComparison.OrdinalIgnoreCase) ? "Verbose" :
                               json.Contains("Information", StringComparison.OrdinalIgnoreCase) ? "Information" :
                               json.Contains("Warning", StringComparison.OrdinalIgnoreCase) ? "Warning" :
                               json.Contains("Error", StringComparison.OrdinalIgnoreCase) ? "Error" : "未知";
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[SYSADMIN] Load log status failed"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SetLevelAsync(string level)
    {
        try
        {
            IsBusy = true;
            var req = new SetLoggingLevelRequest { Level = level };
            await _diagnosticsApi.SetLoggingLevelAsync(req);
            CurrentLevel = level;
            StatusMessage = $"日志级别已设置为 {level}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SYSADMIN] Set log level failed");
            StatusMessage = $"设置失败: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task EnableDebugAsync()
    {
        try
        {
            IsBusy = true;
            var req = new EnableDebugModeRequest { Level = "Debug", DurationMinutes = 60 };
            await _diagnosticsApi.EnableDebugModeAsync(req);
            CurrentLevel = "Debug (60分钟)";
            StatusMessage = "Debug 模式已开启（60分钟后自动关闭）";
        }
        catch (Exception ex) { StatusMessage = $"开启失败: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DisableDebugAsync()
    {
        try
        {
            IsBusy = true;
            await _diagnosticsApi.DisableDebugModeAsync();
            CurrentLevel = "Information";
            StatusMessage = "Debug 模式已关闭";
        }
        catch (Exception ex) { StatusMessage = $"关闭失败: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}

using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup.Steps;

/// <summary>
/// Desktop 自动更新检查步骤（US-SHELL-010: 启动后后台检查更新——非阻塞；
/// 有更新时提示用户，确认后下载——应用重启由用户触发）
/// </summary>
public class DesktopUpdateStartupStep : IStartupStep
{
    private readonly IDesktopUpdateService _updateService;
    private readonly ILogger<DesktopUpdateStartupStep> _logger;

    public DesktopUpdateStartupStep(
        IDesktopUpdateService updateService,
        ILogger<DesktopUpdateStartupStep> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    public int Order => 400;

    public bool IsRequired => false; // 更新检查失败不阻塞启动

    public string? ParallelGroup => null;

    public string Name => "DesktopUpdate";

    public Task<StartupStepResult> ExecuteAsync(
        IProgress<string>? progress, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在检查更新...");
        // 后台 fire-and-forget——不阻塞启动流程
        _ = CheckAndPromptAsync(cancellationToken);
        return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.Zero));
    }

    private async Task CheckAndPromptAsync(CancellationToken ct)
    {
        try
        {
            var info = await _updateService.CheckForUpdatesAsync();
            if (info is null || ct.IsCancellationRequested)
                return;

            _logger.LogInformation("[UPDATE] 发现新版本: {Version}", info.NewVersion);

            // 提示用户（异步——不阻塞主流程）
            var result = System.Windows.MessageBox.Show(
                $"发现新版本 {info.NewVersion}，是否下载并更新？",
                "软件更新",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            var downloaded = await _updateService.DownloadUpdateAsync();
            if (!downloaded)
            {
                System.Windows.MessageBox.Show("更新包下载失败，请稍后重试或联系管理员", "软件更新",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var restart = System.Windows.MessageBox.Show(
                "更新包已下载，是否立即重启应用完成更新？（请先保存当前工作）",
                "软件更新",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (restart == System.Windows.MessageBoxResult.Yes)
                _updateService.ApplyUpdateAndRestart();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UPDATE] 更新检查流程异常（忽略——不阻塞启动）");
        }
    }
}

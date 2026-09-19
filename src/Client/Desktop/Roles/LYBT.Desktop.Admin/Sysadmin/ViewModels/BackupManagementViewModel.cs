using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services.Backup;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 备份恢复管理视图模型（T7-2: US-SHELL-013）。
/// 备份状态展示 + 手动备份 + 文件列表 + 恢复工作流（确认弹框 → 服务层编排停止/恢复/重启）。
/// </summary>
public partial class BackupManagementViewModel : NavigableViewModelBase
{
    private readonly ILocalDbBackupService _backupService;
    private readonly ILogger<BackupManagementViewModel> _logger;

    public ObservableCollection<BackupFileInfo> BackupFiles { get; } = new();

    [ObservableProperty]
    private bool _isBackingUp;

    [ObservableProperty]
    private bool _isRestoring;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _lastBackupTime = "—";

    [ObservableProperty]
    private string _backupFileCount = "0";

    [ObservableProperty]
    private string _totalSize = "0 B";

    [ObservableProperty]
    private BackupFileInfo? _selectedBackup;

    public BackupManagementViewModel(
        IViewModelServices services,
        ILocalDbBackupService backupService,
        ILogger<BackupManagementViewModel> logger)
        : base(services)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        PageTitle = "备份恢复";
    }

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        // P2-A：async void 有进程崩溃风险——改 fire-and-forget（RefreshAsync 内部已 try-catch）
        _ = RefreshAsync();
    }

    /// <summary>手动备份</summary>
    [RelayCommand]
    private async Task BackupAsync()
    {
        if (IsBackingUp) return;
        IsBackingUp = true;
        StatusMessage = "正在备份...";
        try
        {
            var result = await _backupService.BackupAsync();
            if (result.Success)
            {
                StatusMessage = $"备份成功：{result.File?.FileName}";
                _logger.LogInformation("[BACKUP] 手动备份成功: {File}", result.File?.FileName);
            }
            else
            {
                StatusMessage = $"备份失败：{result.Error}";
                _logger.LogWarning("[BACKUP] 手动备份失败: {Error}", result.Error);
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 手动备份异常");
            StatusMessage = $"备份失败：{ex.Message}";
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    /// <summary>恢复所选备份（N6：确认弹框下沉 VM，走 CommonDialogService，View 层无 MessageBox）</summary>
    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (SelectedBackup == null || IsRestoring) return;

        var confirmed = await CommonDialogService.ShowConfirmAsync(
            $"将覆盖当前数据库并恢复为备份「{SelectedBackup.FileName}」，是否继续？",
            "恢复确认");
        if (!confirmed) return;

        IsRestoring = true;
        StatusMessage = "正在恢复...";
        try
        {
            var result = await _backupService.RestoreAsync(SelectedBackup.Id);
            if (result.Success)
            {
                StatusMessage = $"恢复完成：{SelectedBackup.FileName}，请重启应用";
                _logger.LogInformation("[BACKUP] 恢复成功: {File}", SelectedBackup.FileName);
            }
            else
            {
                StatusMessage = $"恢复失败：{result.Error}";
                _logger.LogWarning("[BACKUP] 恢复失败: {Error}", result.Error);
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 恢复异常");
            StatusMessage = $"恢复失败：{ex.Message}";
        }
        finally
        {
            IsRestoring = false;
        }
    }

    /// <summary>刷新状态与文件列表</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var status = await _backupService.GetStatusAsync();
            LastBackupTime = status.LastBackupAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "从未备份";
            BackupFileCount = status.FileCount.ToString();
            TotalSize = FormatSize(status.TotalSizeBytes);

            var files = await _backupService.ListBackupsAsync();
            BackupFiles.Clear();
            foreach (var file in files)
            {
                BackupFiles.Add(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 刷新备份状态失败");
            StatusMessage = $"刷新失败：{ex.Message}";
        }
    }

    private static string FormatSize(long bytes)
        => bytes switch
        {
            >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
            >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            >= 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes} B"
        };
}

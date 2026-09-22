using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Admin.Sysadmin.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Backup;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 备份恢复管理视图模型（B-06 / US-SHELL-013）。
/// 备份状态与进度展示 + 手动备份（全量/差异，可选压缩/加密）+ 文件列表/删除/清理 +
/// 恢复工作流（整库或选择性表/记录恢复，恢复前自动保护性备份，输入「确认恢复」+ 倒计时二次确认）。
/// </summary>
/// <remarks>
/// 数据面经 <see cref="IBackupManagementService"/> 门面（DP10：VM 禁注入 IApiClient 子接口），
/// 远程模式由服务端执行、本地模式由内嵌 LocalWebAPI 对本机 LocalDB 执行，路由由连接模式决定。
/// 备份/恢复进行中通过 <c>GET /api/v1/backup/status</c> 轮询进度（服务端读 DMV percent_complete）。
/// </remarks>
public partial class BackupManagementViewModel : NavigableViewModelBase
{
    private const string RestoreConfirmKeyword = "确认恢复";
    private static readonly TimeSpan ProgressPollInterval = TimeSpan.FromSeconds(1);

    private readonly IBackupManagementService _backupService;
    private readonly ILogger<BackupManagementViewModel> _logger;
    private CancellationTokenSource? _progressCts;

    /// <summary>备份文件列表</summary>
    public ObservableCollection<BackupFileModel> BackupFiles { get; } = new();

    /// <summary>选择性恢复可选表清单</summary>
    public ObservableCollection<RestorableTableModel> RestorableTables { get; } = new();

    [ObservableProperty]
    private BackupFileModel? _selectedBackup;

    [ObservableProperty]
    private string _lastBackupTime = "—";

    [ObservableProperty]
    private string _backupFileCount = "0";

    [ObservableProperty]
    private string _totalSize = "0 B";

    [ObservableProperty]
    private string _backupDirectory = "—";

    [ObservableProperty]
    private string _retentionInfo = "—";

    [ObservableProperty]
    private string _autoBackupInfo = "—";

    [ObservableProperty]
    private bool _isOperationRunning;

    [ObservableProperty]
    private string? _operationPhase;

    [ObservableProperty]
    private int _progressPercent;

    // ---- 备份选项 ----

    [ObservableProperty]
    private bool _compressBackup = true;

    [ObservableProperty]
    private bool _encryptBackup;

    [ObservableProperty]
    private string _encryptionPassword = string.Empty;

    // ---- 恢复面板 ----

    [ObservableProperty]
    private bool _isRestorePanelVisible;

    /// <summary>true = 选择性恢复（按表/记录），false = 整库恢复</summary>
    [ObservableProperty]
    private bool _isSelectiveRestore;

    [ObservableProperty]
    private bool _createPreRestoreBackup = true;

    [ObservableProperty]
    private string _restorePassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRestoreCommand))]
    private string _restoreConfirmText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmRestoreCommand))]
    private int _restoreCountdown;

    [ObservableProperty]
    private string? _restoreTargetText;

    public BackupManagementViewModel(
        IViewModelServices services,
        IBackupManagementService backupService,
        ILogger<BackupManagementViewModel> logger)
        : base(services)
    {
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        PageTitle = "备份恢复";
    }

    /// <inheritdoc />
    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        // P2-A：async void 有进程崩溃风险——改 fire-and-forget（RefreshAsync 内部已 try-catch）
        _ = RefreshAsync();
    }

    // ------------------------------------------------------------------
    // 备份
    // ------------------------------------------------------------------

    /// <summary>立即执行全量备份</summary>
    [RelayCommand]
    private Task BackupFullAsync() => RunBackupAsync(BackupKind.Full);

    /// <summary>立即执行差异备份（依赖最近一次全量备份）</summary>
    [RelayCommand]
    private Task BackupDifferentialAsync() => RunBackupAsync(BackupKind.Differential);

    private async Task RunBackupAsync(BackupKind kind)
    {
        if (IsOperationRunning)
            return;

        ClearMessages();
        StatusMessage = kind == BackupKind.Differential ? "正在执行差异备份..." : "正在执行全量备份...";
        StartProgressPolling();

        try
        {
            var result = await _backupService.CreateAsync(new BackupCreateRequestDto
            {
                Kind = kind,
                Compress = CompressBackup,
                Encrypt = EncryptBackup,
                Password = string.IsNullOrWhiteSpace(EncryptionPassword) ? null : EncryptionPassword
            });

            if (result.Success)
            {
                StatusMessage = result.Message;
                await ShowSuccessMessageAsync(result.Message);
            }
            else
            {
                ShowError(result.Error ?? "备份失败");
                _logger.LogWarning("[BACKUP] 手动备份失败: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 手动备份异常");
            ShowError($"备份失败：{ex.Message}");
        }
        finally
        {
            await StopProgressPollingAsync();
            await RefreshAsync();
        }
    }

    // ------------------------------------------------------------------
    // 删除 / 清理
    // ------------------------------------------------------------------

    /// <summary>删除所选备份文件</summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedBackup == null || IsOperationRunning)
            return;

        var confirmed = await CommonDialogService.ShowConfirmAsync(
            $"确定删除备份「{SelectedBackup.FileName}」？该操作不可撤销。",
            "删除备份");
        if (!confirmed)
            return;

        ClearMessages();
        try
        {
            var result = await _backupService.DeleteAsync(SelectedBackup.Id);
            if (result.Success)
            {
                StatusMessage = result.Message;
                await ShowSuccessMessageAsync(result.Message);
            }
            else
            {
                ShowError(result.Error ?? "删除失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 删除备份异常");
            ShowError($"删除失败：{ex.Message}");
        }
        finally
        {
            await RefreshAsync();
        }
    }

    /// <summary>清理超过保留期的备份文件</summary>
    [RelayCommand]
    private async Task CleanupAsync()
    {
        if (IsOperationRunning)
            return;

        ClearMessages();
        try
        {
            var result = await _backupService.CleanupAsync();
            if (result.Success)
            {
                StatusMessage = result.Message;
            }
            else
            {
                ShowError(result.Error ?? "清理失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 清理备份异常");
            ShowError($"清理失败：{ex.Message}");
        }
        finally
        {
            await RefreshAsync();
        }
    }

    // ------------------------------------------------------------------
    // 恢复
    // ------------------------------------------------------------------

    /// <summary>打开恢复确认面板（含输入「确认恢复」+ 倒计时二次确认）</summary>
    [RelayCommand]
    private async Task OpenRestorePanelAsync()
    {
        if (SelectedBackup == null || IsOperationRunning)
            return;

        ClearMessages();
        if (SelectedBackup.IsChainBroken)
        {
            ShowError($"备份「{SelectedBackup.FileName}」的基准全量备份缺失，无法恢复");
            return;
        }

        RestoreTargetText = $"{SelectedBackup.FileName}（{SelectedBackup.KindText} · {SelectedBackup.CreatedAtText}）";
        RestoreConfirmText = string.Empty;
        RestorePassword = string.Empty;
        IsSelectiveRestore = false;
        CreatePreRestoreBackup = true;
        IsRestorePanelVisible = true;

        await LoadRestorableTablesAsync();
        await StartRestoreCountdownAsync();
    }

    /// <summary>取消恢复</summary>
    [RelayCommand]
    private void CancelRestore()
    {
        IsRestorePanelVisible = false;
        RestoreConfirmText = string.Empty;
        RestoreCountdown = 0;
    }

    /// <summary>执行恢复（需输入「确认恢复」且倒计时结束）</summary>
    [RelayCommand(CanExecute = nameof(CanConfirmRestore))]
    private async Task ConfirmRestoreAsync()
    {
        var target = SelectedBackup;
        if (target == null || IsOperationRunning)
            return;

        var tables = IsSelectiveRestore
            ? RestorableTables
                .Where(table => table.IsSelected)
                .Select(table => new SelectiveRestoreTableDto
                {
                    TableName = table.TableName,
                    Ids = table.ParseRecordIds()
                })
                .ToList()
            : new List<SelectiveRestoreTableDto>();

        if (IsSelectiveRestore && tables.Count == 0)
        {
            ShowError("选择性恢复需至少勾选一张表");
            return;
        }

        IsRestorePanelVisible = false;
        ClearMessages();
        StatusMessage = "正在恢复...";
        StartProgressPolling();

        try
        {
            var result = await _backupService.RestoreAsync(target.Id, new RestoreRequestDto
            {
                Mode = IsSelectiveRestore ? RestoreMode.Selective : RestoreMode.Full,
                Tables = tables,
                CreatePreRestoreBackup = CreatePreRestoreBackup,
                Password = string.IsNullOrWhiteSpace(RestorePassword) ? null : RestorePassword
            });

            if (result.Success)
            {
                StatusMessage = result.Message;
                if (!string.IsNullOrWhiteSpace(result.Warning))
                {
                    StatusMessage = $"{result.Message}；{result.Warning}";
                    await ShowWarningMessageAsync(result.Warning);
                }
                else
                {
                    await ShowSuccessMessageAsync(result.Message);
                }
            }
            else
            {
                ShowError(result.Error ?? "恢复失败");
                _logger.LogWarning("[BACKUP] 恢复失败: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 恢复异常");
            ShowError($"恢复失败：{ex.Message}");
        }
        finally
        {
            await StopProgressPollingAsync();
            await RefreshAsync();
        }
    }

    private bool CanConfirmRestore()
        => RestoreCountdown <= 0 &&
           string.Equals(RestoreConfirmText?.Trim(), RestoreConfirmKeyword, StringComparison.Ordinal);

    private async Task StartRestoreCountdownAsync()
    {
        RestoreCountdown = 5;
        while (RestoreCountdown > 0 && IsRestorePanelVisible)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await UiDispatcher.InvokeAsync(() =>
            {
                if (IsRestorePanelVisible && RestoreCountdown > 0)
                    RestoreCountdown--;
            });
        }
    }

    private async Task LoadRestorableTablesAsync()
    {
        try
        {
            var tables = await _backupService.GetTablesAsync();
            RestorableTables.Clear();
            foreach (var table in tables)
            {
                RestorableTables.Add(RestorableTableModel.FromDto(table));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 加载可恢复表清单失败");
            ShowError($"加载可恢复表清单失败：{ex.Message}");
        }
    }

    // ------------------------------------------------------------------
    // 状态刷新与进度轮询
    // ------------------------------------------------------------------

    /// <summary>刷新状态与文件列表</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var status = await _backupService.GetStatusAsync();
            await UiDispatcher.InvokeAsync(() => ApplyStatus(status));

            var files = await _backupService.GetBackupsAsync();
            var selectedId = SelectedBackup?.Id;

            await UiDispatcher.InvokeAsync(() =>
            {
                BackupFiles.Clear();
                foreach (var file in files)
                {
                    BackupFiles.Add(BackupFileModel.FromDto(file));
                }

                SelectedBackup = selectedId == null
                    ? null
                    : BackupFiles.FirstOrDefault(file => file.Id == selectedId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 刷新备份状态失败");
            ShowError($"刷新失败：{ex.Message}");
        }
    }

    private void ApplyStatus(BackupStatusDto status)
    {
        LastBackupTime = status.LastBackupAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "从未备份";
        BackupFileCount = status.FileCount.ToString();
        TotalSize = FormatSize(status.TotalSizeBytes);
        BackupDirectory = string.IsNullOrWhiteSpace(status.BackupDirectory) ? "—" : status.BackupDirectory;
        RetentionInfo = status.RetentionDays > 0 ? $"保留 {status.RetentionDays} 天" : "—";
        AutoBackupInfo = status.AutoBackupEnabled
            ? $"计划调度：每 {status.AutoBackupIntervalHours} 小时"
            : $"登录自动备份：间隔 {status.AutoBackupIntervalHours} 小时（计划调度未启用）";

        // 本地轮询进行中时保持进度可见——服务端作业可能尚未登记到跟踪器（首个轮询窗口）
        IsOperationRunning = status.IsOperationRunning || _progressCts != null;
        OperationPhase = status.IsOperationRunning
            ? $"{status.OperationKind}：{status.PhaseMessage}"
            : status.PhaseMessage;
        ProgressPercent = status.ProgressPercent;

        if (!string.IsNullOrWhiteSpace(status.LastError))
            ErrorMessage = status.LastError;
    }

    private void StartProgressPolling()
    {
        _progressCts?.Cancel();
        var cts = new CancellationTokenSource();
        _progressCts = cts;
        _ = PollProgressAsync(cts);
    }

    private async Task PollProgressAsync(CancellationTokenSource cts)
    {
        try
        {
            // 立即标记运行中——POST 请求返回前 UI 也要显示进度条
            await UiDispatcher.InvokeAsync(() =>
            {
                IsOperationRunning = true;
                ProgressPercent = 0;
            });

            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(ProgressPollInterval, cts.Token);
                var status = await _backupService.GetStatusAsync(cts.Token);
                await UiDispatcher.InvokeAsync(() => ApplyStatus(status));
            }
        }
        catch (OperationCanceledException)
        {
            // 正常结束
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[BACKUP] 进度轮询结束（异常）");
        }
    }

    private async Task StopProgressPollingAsync()
    {
        var cts = _progressCts;
        _progressCts = null;
        if (cts != null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        await UiDispatcher.InvokeAsync(() => IsOperationRunning = false);
    }

    private void ClearMessages()
    {
        StatusMessage = string.Empty;
        ClearError();
    }

    /// <summary>统一失败处理：状态文本 + 基类错误态 + Toast</summary>
    private void ShowError(string message)
    {
        ErrorMessage = message;
        StatusMessage = message;
        ToastService.ShowError(message);
    }

    /// <inheritdoc />
    protected override void OnDisposing()
    {
        _progressCts?.Cancel();
        _progressCts?.Dispose();
        _progressCts = null;
        base.OnDisposing();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}

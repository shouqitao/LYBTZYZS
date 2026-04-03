using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Events;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Sync.Services;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Sync.ViewModels;

/// <summary>
/// US-SYNC-007: Phase-based sync workflow ViewModel.
/// Replaces boolean IsSyncing/HasCheckedDifferences with SyncPhase enum.
/// </summary>
public partial class SyncViewModel : NavigableViewModelBase
{
    private readonly ISyncService _syncService;
    private readonly IDialogService _dialogService;
    private readonly IApiHealthCheckService _healthCheckService;
    private readonly SyncItemViewModelFactory _itemFactory;

    private SyncRetryDescriptor? _lastRetryDescriptor;
    private readonly CancellationTokenSource _cts = new();

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<string> _entityTypes = [];

    [ObservableProperty]
    private string? _selectedEntityType;

    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _localOnlyItems = [];

    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _serverOnlyItems = [];

    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _conflictItems = [];

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    [ObservableProperty]
    private int _syncProgress;

    /// <summary>
    /// Current workflow phase (replaces IsSyncing + HasCheckedDifferences).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSyncing))]
    [NotifyCanExecuteChangedFor(nameof(CheckDifferencesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteSyncCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    private SyncPhase _currentPhase = SyncPhase.Idle;

    /// <summary>
    /// Step indicator text (e.g. "Step 2/4: Reviewing differences").
    /// </summary>
    [ObservableProperty]
    private string _phaseDescription = string.Empty;

    /// <summary>
    /// Per-entity-type result summaries (card display).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SyncResultSummary> _resultSummaries = [];

    /// <summary>
    /// Error message when CurrentPhase == Failed.
    /// </summary>
    [ObservableProperty]
    private string _syncErrorMessage = string.Empty;

    /// <summary>
    /// Classified error category for retry decisions.
    /// </summary>
    [ObservableProperty]
    private SyncErrorCategory _errorCategory = SyncErrorCategory.Unknown;

    /// <summary>
    /// Whether retry is available after failure.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    private bool _canRetry;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Computed from CurrentPhase (keeps SyncEvents.StatusChangedEvent working).
    /// </summary>
    public bool IsSyncing =>
        CurrentPhase is SyncPhase.CheckingDifferences or SyncPhase.ExecutingSync;

    public bool HasDataToSync =>
        LocalOnlyItems.Any(x => x.IsSelected) ||
        ServerOnlyItems.Any(x => x.IsSelected) ||
        ConflictItems.Any(x => x.IsSelected);

    public int UploadCount => LocalOnlyItems.Count(x => x.IsSelected);
    public int DownloadCount => ServerOnlyItems.Count(x => x.IsSelected);
    public int ConflictCount => ConflictItems.Count;
    public int TotalDifferenceCount => LocalOnlyItems.Count + ServerOnlyItems.Count + ConflictItems.Count;

    #endregion

    #region Nested Types

    private enum SyncRetryAction { CheckDifferences, ExecuteSync }

    private sealed record SyncRetryDescriptor(
        SyncRetryAction Action,
        string EntityType,
        SyncPhase FailedPhase);

    #endregion

    #region Constructor

    public SyncViewModel(
        IViewModelServices services,
        ISyncService syncService,
        IDialogService dialogService,
        IApiHealthCheckService healthCheckService,
        SyncItemViewModelFactory itemFactory)
        : base(services)
    {
        _syncService = syncService;
        _dialogService = dialogService;
        _healthCheckService = healthCheckService;
        _itemFactory = itemFactory;
        _itemFactory.SetSelectionChangedCallback(NotifyCountsChanged);

        PageTitle = "数据同步";
    }

    #endregion

    #region Navigation

    protected override async Task InitializeAsync(NavigationContext context)
    {
        await LoadEntityTypesAsync();
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanCheckDifferences))]
    private async Task CheckDifferencesAsync()
    {
        if (string.IsNullOrEmpty(SelectedEntityType))
        {
            SetError("请选择要同步的数据类型");
            return;
        }

        if (!await ValidatePreConditionsAsync())
            return;

        _lastRetryDescriptor = new SyncRetryDescriptor(
            SyncRetryAction.CheckDifferences,
            SelectedEntityType,
            SyncPhase.CheckingDifferences);

        try
        {
            CurrentPhase = SyncPhase.CheckingDifferences;
            ClearDifferences();
            SyncErrorMessage = string.Empty;
            CanRetry = false;

            var result = await _syncService.CheckDifferencesAsync(SelectedEntityType);

            foreach (var diff in result.LocalOnly)
                LocalOnlyItems.Add(_itemFactory.Create(diff, true));

            foreach (var diff in result.ServerOnly)
                ServerOnlyItems.Add(_itemFactory.Create(diff, true));

            foreach (var diff in result.Conflicts)
                ConflictItems.Add(_itemFactory.Create(diff, false));

            NotifyCountsChanged();

            if (!result.HasDifferences)
            {
                StatusMessage = "数据已同步，无需更新";
                CurrentPhase = SyncPhase.Completed;
            }
            else
            {
                StatusMessage = $"发现 {result.TotalDifferences} 条差异";
                CurrentPhase = SyncPhase.ReviewingDifferences;
            }
        }
        catch (Exception ex)
        {
            HandleWorkflowFailure(ex, "检查差异");
        }
    }

    private bool CanCheckDifferences() =>
        !string.IsNullOrEmpty(SelectedEntityType) &&
        CurrentPhase is SyncPhase.Idle or SyncPhase.Completed or SyncPhase.Failed;

    [RelayCommand(CanExecute = nameof(CanExecuteSync))]
    private async Task ExecuteSyncAsync()
    {
        if (string.IsNullOrEmpty(SelectedEntityType))
            return;

        if (!await ValidatePreConditionsAsync())
            return;

        var unresolvedConflicts = ConflictItems
            .Where(x => x.IsSelected && !x.ResolutionDecision.HasValue)
            .ToList();
        if (unresolvedConflicts.Count > 0)
        {
            await ShowConflictResolutionDialogAsync(unresolvedConflicts);
            return;
        }

        _lastRetryDescriptor = new SyncRetryDescriptor(
            SyncRetryAction.ExecuteSync,
            SelectedEntityType,
            SyncPhase.ExecutingSync);

        try
        {
            CurrentPhase = SyncPhase.ExecutingSync;
            SyncProgress = 0;
            CanRetry = false;
            SyncErrorMessage = string.Empty;

            var resolution = SyncResolutionBuilder.Build(LocalOnlyItems, ServerOnlyItems, ConflictItems);

            SyncProgress = 20;
            var result = await _syncService.ExecuteSyncAsync(SelectedEntityType, resolution);
            SyncProgress = 100;

            ResultSummaries = new ObservableCollection<SyncResultSummary>(
                [CreateSummary(result)]);

            if (result.IsSuccess)
            {
                LastSyncTime = DateTime.Now;

                var statusParts = new List<string>();
                if (result.UploadedCount > 0) statusParts.Add($"上传 {result.UploadedCount} 条");
                if (result.DownloadedCount > 0) statusParts.Add($"下载 {result.DownloadedCount} 条");
                if (result.DeletedCount > 0) statusParts.Add($"删除 {result.DeletedCount} 条");
                if (result.DeleteRejections.Count > 0) statusParts.Add($"删除拒绝 {result.DeleteRejections.Count} 条");
                StatusMessage = $"同步完成: {(statusParts.Count > 0 ? string.Join(", ", statusParts) : "无变更")}";

                ClearDifferences();
                CurrentPhase = SyncPhase.Completed;

                var successMessage = $"同步成功!\n上传: {result.UploadedCount} 条\n下载: {result.DownloadedCount} 条";
                if (result.DeletedCount > 0) successMessage += $"\n删除: {result.DeletedCount} 条";
                if (result.DeleteRejections.Count > 0)
                    successMessage += $"\n删除被拒绝: {result.DeleteRejections.Count} 条 (引用检查未通过)";
                await ShowSuccessMessageAsync(successMessage);
            }
            else
            {
                SetError($"同步失败: {string.Join(", ", result.Errors)}");
                CurrentPhase = SyncPhase.Failed;
                CanRetry = true;
            }
        }
        catch (Exception ex)
        {
            HandleWorkflowFailure(ex, "执行同步");
        }
        finally
        {
            SyncProgress = 0;
        }
    }

    private bool CanExecuteSync() =>
        CurrentPhase == SyncPhase.ReviewingDifferences && HasDataToSync;

    [RelayCommand(CanExecute = nameof(CanRetrySync))]
    private async Task RetryAsync()
    {
        if (_lastRetryDescriptor is null)
            return;

        switch (_lastRetryDescriptor.Action)
        {
            case SyncRetryAction.CheckDifferences:
                await CheckDifferencesAsync();
                break;
            case SyncRetryAction.ExecuteSync:
                await ExecuteSyncAsync();
                break;
        }
    }

    private bool CanRetrySync() => CanRetry && CurrentPhase == SyncPhase.Failed;

    [RelayCommand]
    private void Reset()
    {
        CurrentPhase = SyncPhase.Idle;
        SyncErrorMessage = string.Empty;
        ErrorCategory = SyncErrorCategory.Unknown;
        CanRetry = false;
        _lastRetryDescriptor = null;
        SyncProgress = 0;
        ResultSummaries.Clear();
        ClearError();
    }

    [RelayCommand]
    private void SelectAllUpload()
    {
        foreach (var item in LocalOnlyItems)
            item.IsSelected = true;
        NotifyCountsChanged();
    }

    [RelayCommand]
    private void SelectAllDownload()
    {
        foreach (var item in ServerOnlyItems)
            item.IsSelected = true;
        NotifyCountsChanged();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in LocalOnlyItems) item.IsSelected = false;
        foreach (var item in ServerOnlyItems) item.IsSelected = false;
        foreach (var item in ConflictItems) item.IsSelected = false;
        NotifyCountsChanged();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(SelectedEntityType))
            await CheckDifferencesAsync();
    }

    #endregion

    #region Private Methods

    private async Task<bool> ValidatePreConditionsAsync()
    {
        if (!SessionManager.IsAuthenticated)
        {
            SetError("请先登录后再进行同步操作");
            return false;
        }

        var healthStatus = await _healthCheckService.CheckHealthAsync(timeout: 5000);
        if (healthStatus != ApiHealthStatus.Healthy)
        {
            var errorDetail = _healthCheckService.LastErrorMessage ?? "无法连接到服务器";
            SetError($"网络连接不可用: {errorDetail}");
            return false;
        }

        return true;
    }

    private async Task LoadEntityTypesAsync()
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var types = await _syncService.GetSupportedEntityTypesAsync();
            EntityTypes = new ObservableCollection<string>(types);

            if (EntityTypes.Count > 0)
                SelectedEntityType = EntityTypes[0];
        }, "加载实体类型");
    }

    private void ClearDifferences()
    {
        LocalOnlyItems.Clear();
        ServerOnlyItems.Clear();
        ConflictItems.Clear();
        NotifyCountsChanged();
    }

    private async Task ShowConflictResolutionDialogAsync(List<SyncItemViewModel> conflicts)
    {
        var parameters = new DialogParameters
        {
            { "Conflicts", conflicts }
        };

        var result = await Task.Run(() =>
        {
            IDialogResult? dialogResult = null;
            RunOnUIThread(() =>
            {
                _dialogService.ShowDialog("SyncConflictDialog", parameters, r => dialogResult = r);
            });
            return dialogResult;
        }, _cts.Token);

        if (result?.Result == ButtonResult.OK)
        {
            NotifyCountsChanged();
            await ExecuteSyncAsync();
        }
    }

    private static SyncResultSummary CreateSummary(SyncExecutionResult result) =>
        new(result.EntityType,
            result.UploadedCount,
            result.DownloadedCount,
            result.DeletedCount,
            result.SkippedCount,
            result.FailedCount,
            result.DeleteRejections.Select(x => x.Reason).ToList());

    private void HandleWorkflowFailure(Exception ex, string operationName)
    {
        Logger.LogError(ex, "{Operation} failed", operationName);

        ErrorCategory = SyncErrorClassifier.Classify(ex);
        SyncErrorMessage = ex.Message;
        CanRetry = SyncErrorClassifier.IsRetryable(ErrorCategory);

        CurrentPhase = SyncPhase.Failed;
        SetError($"{operationName}失败: {ex.Message}");
    }

    private void NotifyCountsChanged()
    {
        OnPropertyChanged(nameof(HasDataToSync));
        OnPropertyChanged(nameof(UploadCount));
        OnPropertyChanged(nameof(DownloadCount));
        OnPropertyChanged(nameof(ConflictCount));
        OnPropertyChanged(nameof(TotalDifferenceCount));
        ExecuteSyncCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentPhaseChanged(SyncPhase value)
    {
        PhaseDescription = value switch
        {
            SyncPhase.Idle => "准备就绪",
            SyncPhase.CheckingDifferences => "Step 1/4: 检查差异",
            SyncPhase.ReviewingDifferences => "Step 2/4: 审查差异",
            SyncPhase.ExecutingSync => "Step 3/4: 执行同步",
            SyncPhase.Completed => "Step 4/4: 完成",
            SyncPhase.Failed => "同步失败",
            _ => PhaseDescription
        };

        Events.Publish<SyncEvents.StatusChangedEvent, SyncStatusPayload>(
            new SyncStatusPayload
            {
                IsSyncing = IsSyncing,
                LastSyncTime = LastSyncTime,
                StatusMessage = IsSyncing ? "正在同步..." : StatusMessage
            });
    }

    partial void OnSelectedEntityTypeChanged(string? value)
    {
        ClearDifferences();
        Reset();
    }

    #endregion
}

/// <summary>
/// 同步项 ViewModel
/// </summary>
public partial class SyncItemViewModel : ObservableObject
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;

    [ObservableProperty]
    private string _entityName = string.Empty;

    public SyncDiffType DiffType { get; set; }
    public string? LocalChecksum { get; set; }
    public string? ServerChecksum { get; set; }
    public DateTime? LocalChangedAt { get; set; }
    public DateTime? ServerChangedAt { get; set; }
    public List<string>? ChangedFields { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool? _resolutionDecision;

    public string OperationDisplay => DiffType switch
    {
        SyncDiffType.LocalOnly => "上传",
        SyncDiffType.ServerOnly => "下载",
        SyncDiffType.Modified => "冲突",
        _ => "无"
    };

    public string ChangedAtDisplay
    {
        get
        {
            if (LocalChangedAt.HasValue && ServerChangedAt.HasValue)
                return $"本地: {LocalChangedAt:yyyy-MM-dd HH:mm} / 服务器: {ServerChangedAt:yyyy-MM-dd HH:mm}";
            if (LocalChangedAt.HasValue)
                return $"本地: {LocalChangedAt:yyyy-MM-dd HH:mm}";
            if (ServerChangedAt.HasValue)
                return $"服务器: {ServerChangedAt:yyyy-MM-dd HH:mm}";
            return string.Empty;
        }
    }
}

#region SyncViewModel Disposal

partial class SyncViewModel
{
    protected override void OnDisposing()
    {
        _cts.Cancel();
        _cts.Dispose();
        _itemFactory.SetSelectionChangedCallback(null);
        base.OnDisposing();
    }
}

#endregion

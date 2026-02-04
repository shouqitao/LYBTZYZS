using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Sync.ViewModels;

/// <summary>
/// 数据同步主界面 ViewModel
/// OpenSpec: implement-data-sync
/// </summary>
public partial class SyncViewModel : NavigableViewModelBase
{
    private readonly ISyncService _syncService;
    private readonly IDialogService _dialogService;

    #region Observable Properties

    /// <summary>
    /// 支持的实体类型
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _entityTypes = [];

    /// <summary>
    /// 当前选中的实体类型
    /// </summary>
    [ObservableProperty]
    private string? _selectedEntityType;

    /// <summary>
    /// 仅本地有的项（待上传）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _localOnlyItems = [];

    /// <summary>
    /// 仅服务器有的项（待下载）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _serverOnlyItems = [];

    /// <summary>
    /// 冲突项
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _conflictItems = [];

    /// <summary>
    /// 上次同步时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _lastSyncTime;

    /// <summary>
    /// 同步进度（0-100）
    /// </summary>
    [ObservableProperty]
    private int _syncProgress;

    /// <summary>
    /// 是否正在同步
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckDifferencesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteSyncCommand))]
    private bool _isSyncing;

    /// <summary>
    /// 是否已检查差异
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteSyncCommand))]
    private bool _hasCheckedDifferences;

    #endregion

    #region Computed Properties

    /// <summary>
    /// 是否有需要同步的数据
    /// </summary>
    public bool HasDataToSync =>
        LocalOnlyItems.Any(x => x.IsSelected) ||
        ServerOnlyItems.Any(x => x.IsSelected) ||
        ConflictItems.Any(x => x.IsSelected);

    /// <summary>
    /// 待上传数量
    /// </summary>
    public int UploadCount => LocalOnlyItems.Count(x => x.IsSelected);

    /// <summary>
    /// 待下载数量
    /// </summary>
    public int DownloadCount => ServerOnlyItems.Count(x => x.IsSelected);

    /// <summary>
    /// 冲突数量
    /// </summary>
    public int ConflictCount => ConflictItems.Count;

    /// <summary>
    /// 总差异数量
    /// </summary>
    public int TotalDifferenceCount => LocalOnlyItems.Count + ServerOnlyItems.Count + ConflictItems.Count;

    #endregion

    #region Constructor

    public SyncViewModel(
        IViewModelServices services,
        ISyncService syncService,
        IDialogService dialogService)
        : base(services)
    {
        _syncService = syncService;
        _dialogService = dialogService;

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

    /// <summary>
    /// 检查差异命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCheckDifferences))]
    private async Task CheckDifferencesAsync()
    {
        if (string.IsNullOrEmpty(SelectedEntityType))
        {
            SetError("请选择要同步的数据类型");
            return;
        }

        await ExecuteWithErrorHandlingAsync(async () =>
        {
            ClearDifferences();
            IsSyncing = true;

            var result = await _syncService.CheckDifferencesAsync(SelectedEntityType);

            // 填充差异列表
            foreach (var diff in result.LocalOnly)
            {
                LocalOnlyItems.Add(CreateSyncItemViewModel(diff, true));
            }

            foreach (var diff in result.ServerOnly)
            {
                ServerOnlyItems.Add(CreateSyncItemViewModel(diff, true));
            }

            foreach (var diff in result.Conflicts)
            {
                ConflictItems.Add(CreateSyncItemViewModel(diff, false));
            }

            HasCheckedDifferences = true;
            NotifyCountsChanged();

            if (!result.HasDifferences)
            {
                StatusMessage = "数据已同步，无需更新";
            }
            else
            {
                StatusMessage = $"发现 {result.TotalDifferences} 条差异";
            }
        }, "检查差异");

        IsSyncing = false;
    }

    private bool CanCheckDifferences() => !IsSyncing && !string.IsNullOrEmpty(SelectedEntityType);

    /// <summary>
    /// 执行同步命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteSync))]
    private async Task ExecuteSyncAsync()
    {
        if (string.IsNullOrEmpty(SelectedEntityType))
            return;

        // 检查是否有未处理的冲突
        var unresolvedConflicts = ConflictItems.Where(x => x.IsSelected && !x.ResolutionDecision.HasValue).ToList();
        if (unresolvedConflicts.Count > 0)
        {
            // 显示冲突处理对话框
            await ShowConflictResolutionDialogAsync(unresolvedConflicts);
            return;
        }

        await ExecuteWithErrorHandlingAsync(async () =>
        {
            IsSyncing = true;
            SyncProgress = 0;

            var resolution = BuildSyncResolution();

            SyncProgress = 20;
            var result = await _syncService.ExecuteSyncAsync(SelectedEntityType, resolution);
            SyncProgress = 100;

            if (result.IsSuccess)
            {
                LastSyncTime = DateTime.Now;
                StatusMessage = $"同步完成: 上传 {result.UploadedCount} 条, 下载 {result.DownloadedCount} 条";

                // 清除已同步的项
                ClearDifferences();
                HasCheckedDifferences = false;

                await ShowSuccessMessageAsync($"同步成功!\n上传: {result.UploadedCount} 条\n下载: {result.DownloadedCount} 条");
            }
            else
            {
                SetError($"同步失败: {string.Join(", ", result.Errors)}");
            }
        }, "执行同步");

        IsSyncing = false;
        SyncProgress = 0;
    }

    private bool CanExecuteSync() => !IsSyncing && HasCheckedDifferences && HasDataToSync;

    /// <summary>
    /// 全选上传命令
    /// </summary>
    [RelayCommand]
    private void SelectAllUpload()
    {
        foreach (var item in LocalOnlyItems)
        {
            item.IsSelected = true;
        }
        NotifyCountsChanged();
    }

    /// <summary>
    /// 全选下载命令
    /// </summary>
    [RelayCommand]
    private void SelectAllDownload()
    {
        foreach (var item in ServerOnlyItems)
        {
            item.IsSelected = true;
        }
        NotifyCountsChanged();
    }

    /// <summary>
    /// 取消全选命令
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in LocalOnlyItems) item.IsSelected = false;
        foreach (var item in ServerOnlyItems) item.IsSelected = false;
        foreach (var item in ConflictItems) item.IsSelected = false;
        NotifyCountsChanged();
    }

    /// <summary>
    /// 刷新命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(SelectedEntityType))
        {
            await CheckDifferencesAsync();
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadEntityTypesAsync()
    {
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var types = await _syncService.GetSupportedEntityTypesAsync();
            EntityTypes = new ObservableCollection<string>(types);

            if (EntityTypes.Count > 0)
            {
                SelectedEntityType = EntityTypes[0];
            }
        }, "加载实体类型");
    }

    private void ClearDifferences()
    {
        LocalOnlyItems.Clear();
        ServerOnlyItems.Clear();
        ConflictItems.Clear();
        NotifyCountsChanged();
    }

    private SyncItemViewModel CreateSyncItemViewModel(SyncDiffDto diff, bool isSelected)
    {
        var item = new SyncItemViewModel
        {
            EntityId = diff.EntityId,
            EntityType = diff.EntityType,
            EntityName = diff.EntityName ?? diff.EntityId.ToString(),
            DiffType = diff.DiffType,
            LocalChecksum = diff.LocalChecksum,
            ServerChecksum = diff.ServerChecksum,
            LocalChangedAt = diff.LocalChangedAt,
            ServerChangedAt = diff.ServerChangedAt,
            ChangedFields = diff.ChangedFields,
            IsSelected = isSelected
        };

        item.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SyncItemViewModel.IsSelected))
            {
                NotifyCountsChanged();
            }
        };

        return item;
    }

    private SyncResolution BuildSyncResolution()
    {
        var resolution = new SyncResolution();

        // 添加待上传项
        resolution.ToUpload.AddRange(
            LocalOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        // 添加待下载项
        resolution.ToDownload.AddRange(
            ServerOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        // 添加冲突解决
        foreach (var conflict in ConflictItems.Where(x => x.IsSelected && x.ResolutionDecision.HasValue))
        {
            resolution.ConflictResolutions[conflict.EntityId] = conflict.ResolutionDecision!.Value;
        }

        // 添加跳过的冲突
        resolution.Skipped.AddRange(
            ConflictItems.Where(x => !x.IsSelected).Select(x => x.EntityId));

        return resolution;
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
        });

        if (result?.Result == ButtonResult.OK)
        {
            // 对话框已处理冲突决策，更新 ViewModel
            NotifyCountsChanged();

            // 继续执行同步
            await ExecuteSyncAsync();
        }
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

    partial void OnSelectedEntityTypeChanged(string? value)
    {
        // 切换实体类型时清除差异
        ClearDifferences();
        HasCheckedDifferences = false;
    }

    #endregion
}

/// <summary>
/// 同步项 ViewModel
/// </summary>
public partial class SyncItemViewModel : ObservableObject
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体名称（显示用）
    /// </summary>
    [ObservableProperty]
    private string _entityName = string.Empty;

    /// <summary>
    /// 差异类型
    /// </summary>
    public SyncDiffType DiffType { get; set; }

    /// <summary>
    /// 本地 Checksum
    /// </summary>
    public string? LocalChecksum { get; set; }

    /// <summary>
    /// 服务器 Checksum
    /// </summary>
    public string? ServerChecksum { get; set; }

    /// <summary>
    /// 本地修改时间
    /// </summary>
    public DateTime? LocalChangedAt { get; set; }

    /// <summary>
    /// 服务器修改时间
    /// </summary>
    public DateTime? ServerChangedAt { get; set; }

    /// <summary>
    /// 变更字段列表
    /// </summary>
    public List<string>? ChangedFields { get; set; }

    /// <summary>
    /// 是否选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 冲突解决决策（仅用于冲突项）
    /// true: 使用本地版本
    /// false: 使用服务器版本
    /// null: 未决定
    /// </summary>
    [ObservableProperty]
    private bool? _resolutionDecision;

    /// <summary>
    /// 显示的操作类型
    /// </summary>
    public string OperationDisplay => DiffType switch
    {
        SyncDiffType.LocalOnly => "上传",
        SyncDiffType.ServerOnly => "下载",
        SyncDiffType.Modified => "冲突",
        _ => "无"
    };

    /// <summary>
    /// 显示的变更时间
    /// </summary>
    public string ChangedAtDisplay
    {
        get
        {
            if (LocalChangedAt.HasValue && ServerChangedAt.HasValue)
            {
                return $"本地: {LocalChangedAt:yyyy-MM-dd HH:mm} / 服务器: {ServerChangedAt:yyyy-MM-dd HH:mm}";
            }
            if (LocalChangedAt.HasValue)
            {
                return $"本地: {LocalChangedAt:yyyy-MM-dd HH:mm}";
            }
            if (ServerChangedAt.HasValue)
            {
                return $"服务器: {ServerChangedAt:yyyy-MM-dd HH:mm}";
            }
            return string.Empty;
        }
    }
}

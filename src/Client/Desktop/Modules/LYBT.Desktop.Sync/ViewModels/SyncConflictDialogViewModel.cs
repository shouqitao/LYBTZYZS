using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Sync.ViewModels;

/// <summary>
/// 冲突处理对话框 ViewModel
/// OpenSpec: implement-data-sync
/// </summary>
public partial class SyncConflictDialogViewModel : DialogViewModelBase
{
    #region Observable Properties

    /// <summary>
    /// 冲突列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SyncItemViewModel> _conflicts = [];

    /// <summary>
    /// 当前选中的冲突项
    /// </summary>
    [ObservableProperty]
    private SyncItemViewModel? _selectedConflict;

    /// <summary>
    /// 当前冲突索引（1-based，用于显示）
    /// </summary>
    [ObservableProperty]
    private int _currentIndex;

    /// <summary>
    /// 已处理的冲突数量
    /// </summary>
    public int ResolvedCount => Conflicts.Count(c => c.ResolutionDecision.HasValue);

    /// <summary>
    /// 总冲突数量
    /// </summary>
    public int TotalCount => Conflicts.Count;

    /// <summary>
    /// 是否全部已处理
    /// </summary>
    public bool AllResolved => Conflicts.All(c => c.ResolutionDecision.HasValue);

    #endregion

    #region Constructor

    public SyncConflictDialogViewModel(IViewModelServices services)
        : base(services)
    {
        Title = "处理数据冲突";
    }

    #endregion

    #region Dialog Lifecycle

    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        if (parameters == null) return;

        if (parameters.TryGetValue<List<SyncItemViewModel>>("Conflicts", out var conflicts))
        {
            Conflicts = new ObservableCollection<SyncItemViewModel>(conflicts);

            if (Conflicts.Count > 0)
            {
                SelectedConflict = Conflicts[0];
                CurrentIndex = 1;
            }
        }

        NotifyCountsChanged();
    }

    #endregion

    #region Commands

    /// <summary>
    /// 使用本地版本
    /// </summary>
    [RelayCommand]
    private void UseLocal()
    {
        if (SelectedConflict == null) return;

        SelectedConflict.ResolutionDecision = true;
        SelectedConflict.IsSelected = true;
        MoveToNext();
    }

    /// <summary>
    /// 使用服务器版本
    /// </summary>
    [RelayCommand]
    private void UseServer()
    {
        if (SelectedConflict == null) return;

        SelectedConflict.ResolutionDecision = false;
        SelectedConflict.IsSelected = true;
        MoveToNext();
    }

    /// <summary>
    /// 跳过当前冲突
    /// </summary>
    [RelayCommand]
    private void Skip()
    {
        if (SelectedConflict == null) return;

        SelectedConflict.IsSelected = false;
        SelectedConflict.ResolutionDecision = null;
        MoveToNext();
    }

    /// <summary>
    /// 全部使用本地版本
    /// </summary>
    [RelayCommand]
    private void UseAllLocal()
    {
        foreach (var conflict in Conflicts)
        {
            conflict.ResolutionDecision = true;
            conflict.IsSelected = true;
        }
        NotifyCountsChanged();
    }

    /// <summary>
    /// 全部使用服务器版本
    /// </summary>
    [RelayCommand]
    private void UseAllServer()
    {
        foreach (var conflict in Conflicts)
        {
            conflict.ResolutionDecision = false;
            conflict.IsSelected = true;
        }
        NotifyCountsChanged();
    }

    /// <summary>
    /// 上一个冲突
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void Previous()
    {
        var currentIdx = Conflicts.IndexOf(SelectedConflict!);
        if (currentIdx > 0)
        {
            SelectedConflict = Conflicts[currentIdx - 1];
            CurrentIndex = currentIdx; // 1-based
        }
    }

    private bool CanGoPrevious() => SelectedConflict != null && Conflicts.IndexOf(SelectedConflict) > 0;

    /// <summary>
    /// 下一个冲突
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        MoveToNext();
    }

    private bool CanGoNext() => SelectedConflict != null && Conflicts.IndexOf(SelectedConflict) < Conflicts.Count - 1;

    /// <summary>
    /// 完成处理
    /// </summary>
    [RelayCommand]
    private void Complete()
    {
        CloseDialog(ButtonResult.OK);
    }

    /// <summary>
    /// 重写取消命令
    /// </summary>
    protected override void Cancel()
    {
        // 取消时清除所有决策
        foreach (var conflict in Conflicts)
        {
            conflict.ResolutionDecision = null;
            conflict.IsSelected = false;
        }
        base.Cancel();
    }

    #endregion

    #region Private Methods

    private void MoveToNext()
    {
        NotifyCountsChanged();

        var currentIdx = Conflicts.IndexOf(SelectedConflict!);
        if (currentIdx < Conflicts.Count - 1)
        {
            SelectedConflict = Conflicts[currentIdx + 1];
            CurrentIndex = currentIdx + 2; // 1-based
        }

        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCountsChanged()
    {
        OnPropertyChanged(nameof(ResolvedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(AllResolved));
    }

    partial void OnSelectedConflictChanged(SyncItemViewModel? value)
    {
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    #endregion
}

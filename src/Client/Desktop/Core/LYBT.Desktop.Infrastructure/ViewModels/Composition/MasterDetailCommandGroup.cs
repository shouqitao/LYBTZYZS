using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.ViewModels.Composition;

/// <summary>
/// 聚合 Master-Detail 视图的所有命令（列表导航 + CRUD + 批量操作）。
/// 从 MasterDetailViewModelBase 提取，减少基类体积。
/// </summary>
internal sealed partial class MasterDetailCommandGroup<TListItem, TDetail>
    where TListItem : class
    where TDetail : class
{
    private readonly IMasterDetailServices<TListItem, TDetail> _services;
    private readonly ICommandHost<TListItem, TDetail> _host;

    public MasterDetailCommandGroup(
        IMasterDetailServices<TListItem, TDetail> services,
        ICommandHost<TListItem, TDetail> host)
    {
        _services = services;
        _host = host;
    }

    // ========================================================================
    // 列表命令
    // ========================================================================

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _services.Pagination.Reset();
        await _host.LoadListAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await _services.Search.ExecuteSearchAsync(async _ =>
        {
            _services.Pagination.Reset();
            await _host.LoadListAsync();
        });
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        _services.Search.ClearSearch();
        _services.Pagination.Reset();
        await _host.LoadListAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
    private async Task GoToFirstPageAsync()
    {
        _services.Pagination.GoToFirstPage();
        await _host.LoadListAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task GoToPreviousPageAsync()
    {
        _services.Pagination.GoToPreviousPage();
        await _host.LoadListAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task GoToNextPageAsync()
    {
        _services.Pagination.GoToNextPage();
        await _host.LoadListAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
    private async Task GoToLastPageAsync()
    {
        _services.Pagination.GoToLastPage();
        await _host.LoadListAsync();
    }

    private bool CanGoToFirstPage() => _services.Pagination.CanGoToFirstPage;
    private bool CanGoToPreviousPage() => _services.Pagination.CanGoToPreviousPage;
    private bool CanGoToNextPage() => _services.Pagination.CanGoToNextPage;
    private bool CanGoToLastPage() => _services.Pagination.CanGoToLastPage;

    // ========================================================================
    // 详情命令
    // ========================================================================

    [RelayCommand(CanExecute = nameof(CanCreateNew))]
    private async Task CreateNewAsync()
    {
        _services.DetailEditor.CreateNew(_host.CreateNewDetail);
        if (_services.DetailEditor.CurrentDetail != null)
        {
            await _host.OnDetailCreatedAsync(_services.DetailEditor.CurrentDetail);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
        _services.DetailEditor.EnterEditMode();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var detail = _services.DetailEditor.CurrentDetail;
        if (detail == null) return;

        var success = await _host.SaveDetailAsync(detail);
        if (success)
        {
            _services.DetailEditor.ConfirmSaved();
            await RefreshAsync();
            await _host.OnDetailSavedAsync(detail);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task CancelAsync()
    {
        if (_services.DetailEditor.HasUnsavedChanges)
        {
            var confirmed = await _services.Dialog.ShowConfirmAsync(
                "确认取消", "有未保存的更改，确定要取消吗？");
            if (!confirmed) return;
        }
        _services.DetailEditor.CancelEdit();
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        var itemsToDelete = _host.GetSelectedItemsForDelete();
        if (itemsToDelete.Count == 0) return;

        var message = itemsToDelete.Count == 1
            ? "确定要删除选中的记录吗？"
            : $"确定要删除选中的 {itemsToDelete.Count} 条记录吗？";

        var confirmed = await _services.Dialog.ShowConfirmAsync("确认删除", message);
        if (!confirmed) return;

        if (itemsToDelete.Count == 1)
        {
            var success = await _host.DeleteItemAsync(itemsToDelete[0]);
            if (success)
            {
                await RefreshAsync();
                await _host.OnItemDeletedAsync(itemsToDelete[0]);
            }
        }
        else
        {
            await _host.DeleteBatchAsync(itemsToDelete);
            await RefreshAsync();
        }
    }

    // ========================================================================
    // 批量操作命令
    // ========================================================================

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task BatchEnableAsync()
    {
        var items = _host.GetSelectedItemsForDelete();
        if (items.Count == 0) return;

        var confirmed = await _services.Dialog.ShowConfirmAsync(
            "确认启用", $"确定要启用选中的 {items.Count} 条记录吗？");
        if (!confirmed) return;

        await _host.EnableBatchAsync(items);
        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task BatchDisableAsync()
    {
        var items = _host.GetSelectedItemsForDelete();
        if (items.Count == 0) return;

        var confirmed = await _services.Dialog.ShowConfirmAsync(
            "确认禁用", $"确定要禁用选中的 {items.Count} 条记录吗？");
        if (!confirmed) return;

        await _host.DisableBatchAsync(items);
        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync()
    {
        var item = _services.Selection.SelectedItem;
        if (item == null) return;

        var confirmed = await _services.Dialog.ShowConfirmAsync(
            "确认恢复", "确定要恢复选中的记录吗？");
        if (!confirmed) return;

        await _host.RestoreItemAsync(item);
        await _host.InvalidateCachesAsync();
        await RefreshAsync();
    }

    // ========================================================================
    // CanExecute 守卫
    // ========================================================================

    private bool CanCreateNew() => !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;
    private bool CanEdit() => _services.Selection.HasSelection && !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;
    private bool CanSave() => _services.DetailEditor.IsEditMode && _services.DetailEditor.CurrentDetail != null && !_services.Loading.IsBusy;
    private bool CanCancel() => _services.DetailEditor.IsEditMode;
    private bool CanDelete() => _services.Selection.HasSelection && !_services.DetailEditor.IsEditMode && !_services.Loading.IsBusy;
    private bool CanRestore() => _services.Selection.HasSelection && !_services.Loading.IsBusy && _host.IsAdmin;
    private bool HasSelection => _services.Selection.HasSelection;

    // ========================================================================
    // 命令刷新
    // ========================================================================

    public void NotifyCrudCommandsChanged()
    {
        CreateNewCommand.NotifyCanExecuteChanged();
        EditCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        RestoreCommand.NotifyCanExecuteChanged();
    }

    public void NotifyPaginationCommandsChanged()
    {
        GoToFirstPageCommand.NotifyCanExecuteChanged();
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
    }

    public void NotifyAllCommandsChanged()
    {
        NotifyCrudCommandsChanged();
        NotifyPaginationCommandsChanged();
        BatchEnableCommand.NotifyCanExecuteChanged();
        BatchDisableCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// 命令宿主接口 — CommandGroup 通过此接口回调基类的抽象/虚方法。
/// </summary>
internal interface ICommandHost<TListItem, TDetail>
    where TListItem : class
    where TDetail : class
{
    bool IsAdmin { get; }
    Task LoadListAsync();
    TDetail CreateNewDetail();
    Task<bool> SaveDetailAsync(TDetail detail);
    Task<bool> DeleteItemAsync(TListItem item);
    Task DeleteBatchAsync(List<TListItem> items);
    Task EnableBatchAsync(List<TListItem> items);
    Task DisableBatchAsync(List<TListItem> items);
    Task RestoreItemAsync(TListItem item);
    Task InvalidateCachesAsync();
    Task OnDetailCreatedAsync(TDetail detail);
    Task OnDetailSavedAsync(TDetail detail);
    Task OnItemDeletedAsync(TListItem item);
    List<TListItem> GetSelectedItemsForDelete();
}

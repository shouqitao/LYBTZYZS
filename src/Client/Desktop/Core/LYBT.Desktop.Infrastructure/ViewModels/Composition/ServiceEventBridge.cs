using System.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.ViewModels.Composition;

/// <summary>
/// 桥接 IMasterDetailServices 子服务的 PropertyChanged 事件到父 ViewModel。
/// 负责：订阅8个子服务事件、转发属性变更、触发命令刷新、管理生命周期。
/// </summary>
internal sealed class ServiceEventBridge<TListItem, TDetail> : IDisposable
    where TListItem : class
    where TDetail : class
{
    private readonly IMasterDetailServices<TListItem, TDetail> _services;
    private readonly IServiceEventCallback<TListItem, TDetail> _callback;

    public ServiceEventBridge(
        IMasterDetailServices<TListItem, TDetail> services,
        IServiceEventCallback<TListItem, TDetail> callback)
    {
        _services = services;
        _callback = callback;
        Subscribe();
    }

    private void Subscribe()
    {
        _services.Loading.PropertyChanged += OnLoadingPropertyChanged;
        _services.Pagination.PropertyChanged += OnPaginationPropertyChanged;
        _services.Pagination.PageChanged += OnPaginationPageChanged;
        _services.Search.PropertyChanged += ForwardPropertyChanged;
        _services.Selection.PropertyChanged += OnSelectionPropertyChanged;
        _services.Selection.SelectionChanged += OnSelectionSelectionChanged;
        _services.DetailEditor.PropertyChanged += OnDetailEditorPropertyChanged;
        _services.ErrorHandler.PropertyChanged += OnErrorHandlerPropertyChanged;
    }

    private void ForwardPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => _callback.OnPropertyChanged(e.PropertyName);

    private void OnLoadingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILoadingStateManager.IsLoading))
        {
            _callback.OnIsLoadingChanged(_services.Loading.IsLoading);
        }
        else if (e.PropertyName == nameof(ILoadingStateManager.IsBusy))
        {
            _callback.OnIsBusyChanged(_services.Loading.IsBusy);
        }
        else
        {
            _callback.OnPropertyChanged(e.PropertyName);
        }
    }

    private void OnPaginationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _callback.OnPropertyChanged(e.PropertyName);
        if (e.PropertyName is nameof(IPaginationService.CurrentPage)
            or nameof(IPaginationService.TotalPages)
            or nameof(IPaginationService.TotalCount)
            or nameof(IPaginationService.CanGoToFirstPage)
            or nameof(IPaginationService.CanGoToPreviousPage)
            or nameof(IPaginationService.CanGoToNextPage)
            or nameof(IPaginationService.CanGoToLastPage))
        {
            _callback.OnPaginationCommandsChanged();
        }
    }

    private void OnPaginationPageChanged(object? sender, EventArgs e)
    {
        _callback.OnPageChanged().SafeFireAndForget(
            ex => _services.ErrorHandler.HandleException(ex, $"Pagination change failed"));
    }

    private void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _callback.OnPropertyChanged(e.PropertyName);
        if (e.PropertyName == nameof(ISelectionService<TListItem>.SelectedItem))
        {
            _callback.OnSelectionChanged();
        }
    }

    private void OnSelectionSelectionChanged(object? sender, SelectionChangedEventArgs<TListItem> e)
    {
        _callback.OnSelectionItemChanged(e).SafeFireAndForget(
            ex => _services.ErrorHandler.HandleException(ex, $"Selection change failed"));
    }

    private void OnDetailEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDetailEditorService<TDetail>.HasUnsavedChanges))
        {
            _callback.OnHasUnsavedChangesChanged(_services.DetailEditor.HasUnsavedChanges);
        }
        else
        {
            _callback.OnPropertyChanged(e.PropertyName);
        }

        if (e.PropertyName is nameof(IDetailEditorService<TDetail>.IsEditMode)
            or nameof(IDetailEditorService<TDetail>.CurrentDetail)
            or nameof(IDetailEditorService<TDetail>.IsNew))
        {
            if (e.PropertyName == nameof(IDetailEditorService<TDetail>.IsEditMode))
                _callback.OnCrudCommandsChanged();
            _callback.OnDetailStateChanged();
        }
    }

    private void OnErrorHandlerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IErrorHandler.ErrorMessage))
        {
            _callback.OnErrorMessageChanged(_services.ErrorHandler.ErrorMessage ?? string.Empty);
        }
        else
        {
            _callback.OnPropertyChanged(e.PropertyName);
        }
    }

    public void Dispose()
    {
        _services.Loading.PropertyChanged -= OnLoadingPropertyChanged;
        _services.Pagination.PropertyChanged -= OnPaginationPropertyChanged;
        _services.Pagination.PageChanged -= OnPaginationPageChanged;
        _services.Search.PropertyChanged -= ForwardPropertyChanged;
        _services.Selection.PropertyChanged -= OnSelectionPropertyChanged;
        _services.Selection.SelectionChanged -= OnSelectionSelectionChanged;
        _services.DetailEditor.PropertyChanged -= OnDetailEditorPropertyChanged;
        _services.ErrorHandler.PropertyChanged -= OnErrorHandlerPropertyChanged;
    }
}

/// <summary>
/// 回调接口 — ServiceEventBridge 通过此接口通知父 ViewModel 状态变更。
/// </summary>
internal interface IServiceEventCallback<TListItem, TDetail>
    where TListItem : class
    where TDetail : class
{
    void OnPropertyChanged(string? propertyName);
    void OnIsLoadingChanged(bool isLoading);
    void OnIsBusyChanged(bool isBusy);
    void OnPaginationCommandsChanged();
    Task OnPageChanged();
    void OnSelectionChanged();
    Task OnSelectionItemChanged(SelectionChangedEventArgs<TListItem> e);
    void OnHasUnsavedChangesChanged(bool hasUnsavedChanges);
    void OnCrudCommandsChanged();
    void OnDetailStateChanged();
    void OnErrorMessageChanged(string message);
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// Master-Detail视图ViewModel基类V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用IMasterDetailServices进行组合，委托功能给注入的服务
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public abstract partial class MasterDetailViewModelBase<TListItem, TDetail> : ObservableObject, INavigationAware, IRegionMemberLifetime, IDisposable
        where TListItem : class
        where TDetail : class
    {
        private readonly IMasterDetailServices<TListItem, TDetail> _services;
        private bool _disposed;

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Master-Detail服务
        /// </summary>
        protected IMasterDetailServices<TListItem, TDetail> Services => _services;

        /// <summary>
        /// 数据列表
        /// </summary>
        public ObservableCollection<TListItem> Items { get; } = new();

        /// <summary>
        /// 页面标题
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        #region 委托属性 - Loading

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => _services.Loading.IsLoading;

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        public bool IsBusy => _services.Loading.IsBusy;

        /// <summary>
        /// 忙碌提示信息
        /// </summary>
        public string? BusyMessage => _services.Loading.BusyMessage;

        #endregion

        #region 委托属性 - Pagination

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage => _services.Pagination.CurrentPage;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _services.Pagination.PageSize;
            set => _services.Pagination.PageSize = value;
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount => _services.Pagination.TotalCount;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _services.Pagination.TotalPages;

        /// <summary>
        /// 可用的页面大小选项
        /// </summary>
        public IReadOnlyList<int> PageSizes => _services.Pagination.PageSizes;

        #endregion

        #region 委托属性 - Search

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _services.Search.SearchText;
            set => _services.Search.SearchText = value;
        }

        /// <summary>
        /// 是否正在搜索
        /// </summary>
        public bool IsSearching => _services.Search.IsSearching;

        #endregion

        #region 委托属性 - Selection

        /// <summary>
        /// 当前选中项
        /// </summary>
        public TListItem? SelectedItem
        {
            get => _services.Selection.SelectedItem;
            set => _services.Selection.Select(value);
        }

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<TListItem> SelectedItems => _services.Selection.SelectedItems;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => _services.Selection.HasSelection;

        #endregion

        #region 委托属性 - DetailEditor

        /// <summary>
        /// 当前详情
        /// </summary>
        public TDetail? CurrentDetail => _services.DetailEditor.CurrentDetail;

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public bool IsEditMode => _services.DetailEditor.IsEditMode;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges => _services.DetailEditor.HasUnsavedChanges;

        /// <summary>
        /// 是否是新建
        /// </summary>
        public bool IsNew => _services.DetailEditor.IsNew;

        #endregion

        #region 委托属性 - Error

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage => _services.ErrorHandler.ErrorMessage;

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => _services.ErrorHandler.HasErrors;

        #endregion

        public virtual bool KeepAlive => false;

        protected MasterDetailViewModelBase(
            IMasterDetailServices<TListItem, TDetail> services,
            ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            Logger = loggerFactory?.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            // 订阅服务事件以转发属性变更通知
            SubscribeToServiceEvents();
        }

        private void SubscribeToServiceEvents()
        {
            // Loading状态变更
            _services.Loading.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };

            // Pagination变更
            _services.Pagination.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };

            _services.Pagination.PageChanged += async (s, e) =>
            {
                await LoadListAsync();
            };

            // Search变更
            _services.Search.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };

            // Selection变更
            _services.Selection.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };

            _services.Selection.SelectionChanged += async (s, e) =>
            {
                await OnSelectionChangedAsync(e);
            };

            // DetailEditor变更
            _services.DetailEditor.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };

            // Error变更
            _services.ErrorHandler.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };
        }

        /// <summary>
        /// 选择变更时调用
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual async Task OnSelectionChangedAsync(SelectionChangedEventArgs<TListItem> e)
        {
            if (e.NewSelection != null)
            {
                await LoadDetailAsync(e.NewSelection);
            }
        }

        #region 列表命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task RefreshAsync()
        {
            _services.Pagination.Reset();
            await LoadListAsync();
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task SearchAsync()
        {
            await _services.Search.ExecuteSearchAsync(async _ =>
            {
                _services.Pagination.Reset();
                await LoadListAsync();
            });
        }

        /// <summary>
        /// 清除搜索命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task ClearSearchAsync()
        {
            _services.Search.ClearSearch();
            _services.Pagination.Reset();
            await LoadListAsync();
        }

        /// <summary>
        /// 首页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        protected virtual async Task GoToFirstPageAsync()
        {
            _services.Pagination.GoToFirstPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        protected virtual async Task GoToPreviousPageAsync()
        {
            _services.Pagination.GoToPreviousPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        protected virtual async Task GoToNextPageAsync()
        {
            _services.Pagination.GoToNextPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 末页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        protected virtual async Task GoToLastPageAsync()
        {
            _services.Pagination.GoToLastPage();
            await LoadListAsync();
        }

        private bool CanGoToFirstPage() => _services.Pagination.CanGoToFirstPage;
        private bool CanGoToPreviousPage() => _services.Pagination.CanGoToPreviousPage;
        private bool CanGoToNextPage() => _services.Pagination.CanGoToNextPage;
        private bool CanGoToLastPage() => _services.Pagination.CanGoToLastPage;

        #endregion

        #region 详情命令

        /// <summary>
        /// 新建命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCreateNew))]
        protected virtual async Task CreateNewAsync()
        {
            _services.DetailEditor.CreateNew(CreateNewDetail);
            if (CurrentDetail != null)
            {
                await OnDetailCreatedAsync(CurrentDetail);
            }
        }

        /// <summary>
        /// 编辑命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        protected virtual void Edit()
        {
            _services.DetailEditor.EnterEditMode();
        }

        /// <summary>
        /// 保存命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        protected virtual async Task SaveAsync()
        {
            if (CurrentDetail == null) return;

            var success = await SaveDetailAsync(CurrentDetail);
            if (success)
            {
                _services.DetailEditor.ConfirmSaved();
                await RefreshAsync();
                await OnDetailSavedAsync(CurrentDetail);
            }
        }

        /// <summary>
        /// 取消命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCancel))]
        protected virtual async Task CancelAsync()
        {
            if (HasUnsavedChanges)
            {
                var confirmed = await _services.Dialog.ShowConfirmAsync(
                    "确认取消",
                    "有未保存的更改，确定要取消吗？");

                if (!confirmed) return;
            }

            _services.DetailEditor.CancelEdit();
        }

        /// <summary>
        /// 删除命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        protected virtual async Task DeleteAsync()
        {
            if (SelectedItem == null) return;

            var confirmed = await _services.Dialog.ShowConfirmAsync(
                "确认删除",
                "确定要删除选中的记录吗？");

            if (!confirmed) return;

            var success = await DeleteItemAsync(SelectedItem);
            if (success)
            {
                await RefreshAsync();
                await OnItemDeletedAsync(SelectedItem);
            }
        }

        private bool CanCreateNew() => !IsEditMode && !IsBusy;
        private bool CanEdit() => HasSelection && !IsEditMode && !IsBusy;
        private bool CanSave() => IsEditMode && CurrentDetail != null && !IsBusy;
        private bool CanCancel() => IsEditMode;
        private bool CanDelete() => HasSelection && !IsEditMode && !IsBusy;

        #endregion

        #region 抽象方法

        /// <summary>
        /// 加载列表数据 - 子类必须实现
        /// </summary>
        /// <returns>任务</returns>
        protected abstract Task LoadListAsync();

        /// <summary>
        /// 加载详情数据 - 子类必须实现
        /// </summary>
        /// <param name="item">列表项</param>
        /// <returns>任务</returns>
        protected abstract Task LoadDetailAsync(TListItem item);

        /// <summary>
        /// 创建新详情实例 - 子类必须实现
        /// </summary>
        /// <returns>新详情实例</returns>
        protected abstract TDetail CreateNewDetail();

        /// <summary>
        /// 保存详情 - 子类必须实现
        /// </summary>
        /// <param name="detail">详情</param>
        /// <returns>是否成功</returns>
        protected abstract Task<bool> SaveDetailAsync(TDetail detail);

        /// <summary>
        /// 删除项 - 子类必须实现
        /// </summary>
        /// <param name="item">要删除的项</param>
        /// <returns>是否成功</returns>
        protected abstract Task<bool> DeleteItemAsync(TListItem item);

        #endregion

        #region 虚拟方法 - 生命周期钩子

        /// <summary>
        /// 详情创建后调用
        /// </summary>
        /// <param name="detail">新创建的详情</param>
        protected virtual Task OnDetailCreatedAsync(TDetail detail) => Task.CompletedTask;

        /// <summary>
        /// 详情保存后调用
        /// </summary>
        /// <param name="detail">已保存的详情</param>
        protected virtual Task OnDetailSavedAsync(TDetail detail) => Task.CompletedTask;

        /// <summary>
        /// 项删除后调用
        /// </summary>
        /// <param name="item">已删除的项</param>
        protected virtual Task OnItemDeletedAsync(TListItem item) => Task.CompletedTask;

        #endregion

        #region INavigationAware

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

        public virtual async void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到Master-Detail视图: {ViewType}", GetType().Name);
            await LoadListAsync();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _services.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// Master-Detail视图ViewModel基类V2（组合模式）
    ///
    /// 继承NavigableViewModelBase获得导航、日志、EventAggregator、RegionManager等服务，
    /// 通过IMasterDetailServices组合获取列表/详情/分页/搜索等Master-Detail专用服务。
    /// IsLoading/IsBusy/ErrorMessage/HasUnsavedChanges 等属性通过事件订阅从子服务同步到基类。
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
        : NavigableViewModelBase, IAsyncInitializable
        where TListItem : class
        where TDetail : class
    {
        private readonly IMasterDetailServices<TListItem, TDetail> _masterDetailServices;

        /// <summary>
        /// Master-Detail服务
        /// </summary>
        protected IMasterDetailServices<TListItem, TDetail> MasterDetailServices => _masterDetailServices;

        /// <summary>
        /// 数据列表
        /// </summary>
        public ObservableCollection<TListItem> Items { get; } = new();

        #region 委托属性 - Loading

        /// <summary>
        /// 忙碌提示信息
        /// </summary>
        public string? BusyMessage => _masterDetailServices.Loading.BusyMessage;

        #endregion

        #region 委托属性 - Pagination

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage => _masterDetailServices.Pagination.CurrentPage;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _masterDetailServices.Pagination.PageSize;
            set => _masterDetailServices.Pagination.PageSize = value;
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount => _masterDetailServices.Pagination.TotalCount;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _masterDetailServices.Pagination.TotalPages;

        /// <summary>
        /// 可用的页面大小选项
        /// </summary>
        public IReadOnlyList<int> PageSizes => _masterDetailServices.Pagination.PageSizes;

        #endregion

        #region 委托属性 - Search

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _masterDetailServices.Search.SearchText;
            set => _masterDetailServices.Search.SearchText = value;
        }

        /// <summary>
        /// 是否正在搜索
        /// </summary>
        public bool IsSearching => _masterDetailServices.Search.IsSearching;

        #endregion

        #region 委托属性 - Selection

        /// <summary>
        /// 当前选中项
        /// </summary>
        public TListItem? SelectedItem
        {
            get => _masterDetailServices.Selection.SelectedItem;
            set => _masterDetailServices.Selection.Select(value);
        }

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<TListItem> SelectedItems => _masterDetailServices.Selection.SelectedItems;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => _masterDetailServices.Selection.HasSelection;

        /// <summary>
        /// 是否应显示详情面板（有选中项或正在编辑/新建时显示）
        /// </summary>
        public bool ShowDetailPanel => HasSelection || IsEditMode;

        #endregion

        #region 委托属性 - DetailEditor

        /// <summary>
        /// 当前详情
        /// </summary>
        public TDetail? CurrentDetail => _masterDetailServices.DetailEditor.CurrentDetail;

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public bool IsEditMode => _masterDetailServices.DetailEditor.IsEditMode;

        /// <summary>
        /// 是否是新建
        /// </summary>
        public bool IsNew => _masterDetailServices.DetailEditor.IsNew;

        #endregion

        #region 实体元数据与详情标题

        /// <summary>
        /// 实体显示名称 - 子类必须提供 (如 "药材", "患者", "用户")
        /// </summary>
        protected abstract string EntityDisplayName { get; }

        /// <summary>
        /// "新建"动词 - 默认"新建"，子类可重写为"新增"等
        /// </summary>
        protected virtual string NewEntityVerb => "新建";

        /// <summary>
        /// 获取当前详情的显示名称 - 用于标题后缀 (如患者姓名)
        /// 返回 null 时标题不带后缀
        /// </summary>
        protected virtual string? GetDetailDisplayName() => null;

        /// <summary>
        /// 详情面板标题 - 根据状态自动计算
        /// 规则: null→"{Entity}详情", IsNew→"{Verb}{Entity}", Edit→"编辑{Entity} - {Name}", View→"{Entity}详情 - {Name}"
        /// </summary>
        public virtual string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return $"{EntityDisplayName}详情";
                if (IsNew) return $"{NewEntityVerb}{EntityDisplayName}";
                var displayName = GetDetailDisplayName();
                var suffix = displayName != null ? $" - {displayName}" : string.Empty;
                return IsEditMode
                    ? $"编辑{EntityDisplayName}{suffix}"
                    : $"{EntityDisplayName}详情{suffix}";
            }
        }

        /// <summary>
        /// 是否为管理员
        /// </summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        #endregion

        /// <summary>
        /// Master-Detail视图不保持活动状态（每次导航重新创建）
        /// </summary>
        public override bool KeepAlive => false;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected MasterDetailViewModelBase(
            IViewModelServices services,
            IMasterDetailServices<TListItem, TDetail> masterDetailServices)
            : base(services)
        {
            _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));

            // 订阅服务事件以同步属性变更通知
            SubscribeToServiceEvents();
            // 初始化时服务状态都是默认值，不会触发PropertyChanged，需要主动刷新
            NotifyCommandsCanExecuteChanged();
        }

        #region 服务事件订阅与属性同步

        private void SubscribeToServiceEvents()
        {
            // Loading状态变更
            _masterDetailServices.Loading.PropertyChanged += OnLoadingPropertyChanged;

            // Pagination变更
            _masterDetailServices.Pagination.PropertyChanged += OnPaginationPropertyChanged;

            _masterDetailServices.Pagination.PageChanged += OnPaginationPageChanged;

            // Search变更
            _masterDetailServices.Search.PropertyChanged += OnSearchPropertyChanged;

            // Selection变更
            _masterDetailServices.Selection.PropertyChanged += OnSelectionPropertyChanged;

            _masterDetailServices.Selection.SelectionChanged += OnSelectionSelectionChanged;

            // DetailEditor变更
            _masterDetailServices.DetailEditor.PropertyChanged += OnDetailEditorPropertyChanged;

            // Error变更
            _masterDetailServices.ErrorHandler.PropertyChanged += OnErrorHandlerPropertyChanged;
        }

        private void OnLoadingPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // IsLoading/IsBusy 继承自基类，需要从子服务同步
            if (e.PropertyName == nameof(ILoadingStateManager.IsLoading))
            {
                IsLoading = _masterDetailServices.Loading.IsLoading;
            }
            else if (e.PropertyName == nameof(ILoadingStateManager.IsBusy))
            {
                IsBusy = _masterDetailServices.Loading.IsBusy;
                NotifyCommandsCanExecuteChanged();
            }
            else
            {
                // 转发其他属性 (BusyMessage 等)
                OnPropertyChanged(e.PropertyName);
            }
        }

        private void OnPaginationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            // 分页状态变化时刷新分页命令的CanExecute状态
            if (e.PropertyName is nameof(IPaginationService.CurrentPage)
                or nameof(IPaginationService.TotalPages)
                or nameof(IPaginationService.TotalCount)
                or nameof(IPaginationService.CanGoToFirstPage)
                or nameof(IPaginationService.CanGoToPreviousPage)
                or nameof(IPaginationService.CanGoToNextPage)
                or nameof(IPaginationService.CanGoToLastPage))
            {
                NotifyPaginationCommandsCanExecuteChanged();
            }
        }

        private void OnPaginationPageChanged(object? sender, EventArgs e)
        {
            LoadListAsync().SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"Pagination change failed in {GetType().Name}"));
        }

        private void OnSearchPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        private void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(ISelectionService<TListItem>.SelectedItem))
            {
                NotifyCommandsCanExecuteChanged();
                OnPropertyChanged(nameof(ShowDetailPanel));
            }
        }

        private void OnSelectionSelectionChanged(object? sender, SelectionChangedEventArgs<TListItem> e)
        {
            OnSelectionChangedAsync(e).SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"Selection change failed in {GetType().Name}"));
        }

        private void OnDetailEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // HasUnsavedChanges 继承自基类，需要从子服务同步
            if (e.PropertyName == nameof(IDetailEditorService<TDetail>.HasUnsavedChanges))
            {
                HasUnsavedChanges = _masterDetailServices.DetailEditor.HasUnsavedChanges;
            }
            else
            {
                // 转发其他属性 (CurrentDetail, IsEditMode, IsNew 等)
                OnPropertyChanged(e.PropertyName);
            }

            if (e.PropertyName == nameof(IDetailEditorService<TDetail>.IsEditMode))
            {
                NotifyCommandsCanExecuteChanged();
                OnPropertyChanged(nameof(ShowDetailPanel));
            }

            // DetailTitle 依赖 CurrentDetail/IsEditMode/IsNew，自动通知
            if (e.PropertyName is nameof(IDetailEditorService<TDetail>.CurrentDetail)
                or nameof(IDetailEditorService<TDetail>.IsEditMode)
                or nameof(IDetailEditorService<TDetail>.IsNew))
            {
                OnPropertyChanged(nameof(DetailTitle));
            }
        }

        private void OnErrorHandlerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ErrorMessage 继承自基类，需要从子服务同步 (HasError 由基类从 ErrorMessage 计算)
            if (e.PropertyName == nameof(IErrorHandler.ErrorMessage))
            {
                ErrorMessage = _masterDetailServices.ErrorHandler.ErrorMessage ?? string.Empty;
            }
            else
            {
                OnPropertyChanged(e.PropertyName);
            }
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

        #endregion

        #region 列表命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task RefreshAsync()
        {
            _masterDetailServices.Pagination.Reset();
            await LoadListAsync();
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task SearchAsync()
        {
            await _masterDetailServices.Search.ExecuteSearchAsync(async _ =>
            {
                _masterDetailServices.Pagination.Reset();
                await LoadListAsync();
            });
        }

        /// <summary>
        /// 清除搜索命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task ClearSearchAsync()
        {
            _masterDetailServices.Search.ClearSearch();
            _masterDetailServices.Pagination.Reset();
            await LoadListAsync();
        }

        /// <summary>
        /// 首页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        protected virtual async Task GoToFirstPageAsync()
        {
            _masterDetailServices.Pagination.GoToFirstPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        protected virtual async Task GoToPreviousPageAsync()
        {
            _masterDetailServices.Pagination.GoToPreviousPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        protected virtual async Task GoToNextPageAsync()
        {
            _masterDetailServices.Pagination.GoToNextPage();
            await LoadListAsync();
        }

        /// <summary>
        /// 末页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        protected virtual async Task GoToLastPageAsync()
        {
            _masterDetailServices.Pagination.GoToLastPage();
            await LoadListAsync();
        }

        private bool CanGoToFirstPage() => _masterDetailServices.Pagination.CanGoToFirstPage;
        private bool CanGoToPreviousPage() => _masterDetailServices.Pagination.CanGoToPreviousPage;
        private bool CanGoToNextPage() => _masterDetailServices.Pagination.CanGoToNextPage;
        private bool CanGoToLastPage() => _masterDetailServices.Pagination.CanGoToLastPage;

        #endregion

        #region 详情命令

        /// <summary>
        /// 新建命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCreateNew))]
        protected virtual async Task CreateNewAsync()
        {
            _masterDetailServices.DetailEditor.CreateNew(CreateNewDetail);
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
            _masterDetailServices.DetailEditor.EnterEditMode();
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
                _masterDetailServices.DetailEditor.ConfirmSaved();
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
                var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                    "确认取消",
                    "有未保存的更改，确定要取消吗？");

                if (!confirmed) return;
            }

            _masterDetailServices.DetailEditor.CancelEdit();
        }

        /// <summary>
        /// 删除命令 - 支持单选和批量删除
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        protected virtual async Task DeleteAsync()
        {
            var itemsToDelete = GetSelectedItemsForDelete();
            if (itemsToDelete.Count == 0) return;

            var message = itemsToDelete.Count == 1
                ? "确定要删除选中的记录吗？"
                : $"确定要删除选中的 {itemsToDelete.Count} 条记录吗？";

            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync("确认删除", message);
            if (!confirmed) return;

            if (itemsToDelete.Count == 1)
            {
                var success = await DeleteItemAsync(itemsToDelete[0]);
                if (success)
                {
                    await RefreshAsync();
                    await OnItemDeletedAsync(itemsToDelete[0]);
                }
            }
            else
            {
                await DeleteBatchAsync(itemsToDelete);
                await RefreshAsync();
            }
        }

        /// <summary>
        /// 获取用于批量删除的选中项。
        /// 优先使用 DataGrid 复选框选中的项，回退到单个 SelectedItem。
        /// </summary>
        protected virtual List<TListItem> GetSelectedItemsForDelete()
        {
            if (SelectedItems != null && SelectedItems.Count > 0)
                return new List<TListItem>(SelectedItems);

            if (SelectedItem != null)
                return new List<TListItem> { SelectedItem };

            return new List<TListItem>();
        }

        /// <summary>
        /// 批量删除多个项。子类可重写以调用批量 API。
        /// 默认实现逐个删除。
        /// </summary>
        protected virtual async Task DeleteBatchAsync(List<TListItem> items)
        {
            foreach (var item in items)
            {
                var success = await DeleteItemAsync(item);
                if (success)
                    await OnItemDeletedAsync(item);
            }
        }

        /// <summary>
        /// 批量启用命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        protected virtual async Task BatchEnableAsync()
        {
            var items = GetSelectedItemsForDelete();
            if (items.Count == 0) return;

            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                "确认启用", $"确定要启用选中的 {items.Count} 条记录吗？");
            if (!confirmed) return;

            await EnableBatchAsync(items);
            await RefreshAsync();
        }

        /// <summary>
        /// 批量禁用命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        protected virtual async Task BatchDisableAsync()
        {
            var items = GetSelectedItemsForDelete();
            if (items.Count == 0) return;

            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                "确认禁用", $"确定要禁用选中的 {items.Count} 条记录吗？");
            if (!confirmed) return;

            await DisableBatchAsync(items);
            await RefreshAsync();
        }

        /// <summary>
        /// 恢复命令（恢复软删除的记录）
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        protected virtual async Task RestoreAsync()
        {
            var item = SelectedItem;
            if (item == null) return;

            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                "确认恢复", "确定要恢复选中的记录吗？");
            if (!confirmed) return;

            await RestoreItemAsync(item);
            await RefreshAsync();
        }

        /// <summary>
        /// 恢复单项。子类必须重写以调用服务 API。
        /// </summary>
        protected virtual Task RestoreItemAsync(TListItem item)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 批量启用。子类重写以调用服务 API。
        /// </summary>
        protected virtual async Task EnableBatchAsync(List<TListItem> items)
        {
            foreach (var item in items)
                await SetItemEnabledAsync(item, true);
        }

        /// <summary>
        /// 批量禁用。子类重写以调用服务 API。
        /// </summary>
        protected virtual async Task DisableBatchAsync(List<TListItem> items)
        {
            foreach (var item in items)
                await SetItemEnabledAsync(item, false);
        }

        /// <summary>
        /// 设置单项启用/禁用状态。子类必须重写。
        /// </summary>
        protected virtual Task SetItemEnabledAsync(TListItem item, bool enabled)
        {
            return Task.CompletedTask;
        }

        private bool CanCreateNew() => !IsEditMode && !IsBusy;
        private bool CanEdit() => HasSelection && !IsEditMode && !IsBusy;
        private bool CanSave() => IsEditMode && CurrentDetail != null && !IsBusy;
        private bool CanCancel() => IsEditMode;
        private bool CanDelete() => HasSelection && !IsEditMode && !IsBusy;

        /// <summary>
        /// 通知所有命令刷新CanExecute状态
        /// </summary>
        private void NotifyCommandsCanExecuteChanged()
        {
            CreateNewCommand.NotifyCanExecuteChanged();
            EditCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            RestoreCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 通知分页命令刷新CanExecute状态
        /// </summary>
        private void NotifyPaginationCommandsCanExecuteChanged()
        {
            GoToFirstPageCommand.NotifyCanExecuteChanged();
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToNextPageCommand.NotifyCanExecuteChanged();
            GoToLastPageCommand.NotifyCanExecuteChanged();
        }

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

        #region 初始化与导航

        /// <summary>
        /// 初始化ViewModel - 供Control的Loaded事件调用
        /// 当Control通过DI容器解析ViewModel时，OnNavigatedTo不会被调用
        /// 此方法提供替代的初始化入口
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            Logger.LogDebug("初始化Master-Detail视图: {ViewType}", GetType().Name);
            await LoadListAsync();
        }

        /// <summary>
        /// 导航到视图时的核心处理（重写NavigableViewModelBase）
        /// 每次 导航都加载列表数据
        /// </summary>
        protected override void OnNavigatedToCore(NavigationContext navigationContext)
        {
            OnNavigatedToAsync(navigationContext).SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"OnNavigatedTo failed in {GetType().Name}"));
        }

        /// <summary>
        /// 导航到视图时调用的异步实现
        /// 子类应重写此方法而非OnNavigatedToCore
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        /// <returns>异步任务</returns>
        protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到Master-Detail视图: {ViewType}", GetType().Name);
            await LoadListAsync();
        }

        #endregion

        #region 资源清理

        protected override void OnDisposing()
        {
            // 取消订阅服务事件处理器
            _masterDetailServices.Loading.PropertyChanged -= OnLoadingPropertyChanged;
            _masterDetailServices.Pagination.PropertyChanged -= OnPaginationPropertyChanged;
            _masterDetailServices.Pagination.PageChanged -= OnPaginationPageChanged;
            _masterDetailServices.Search.PropertyChanged -= OnSearchPropertyChanged;
            _masterDetailServices.Selection.PropertyChanged -= OnSelectionPropertyChanged;
            _masterDetailServices.Selection.SelectionChanged -= OnSelectionSelectionChanged;
            _masterDetailServices.DetailEditor.PropertyChanged -= OnDetailEditorPropertyChanged;
            _masterDetailServices.ErrorHandler.PropertyChanged -= OnErrorHandlerPropertyChanged;

            _masterDetailServices.Dispose();
        }

        #endregion
    }
}

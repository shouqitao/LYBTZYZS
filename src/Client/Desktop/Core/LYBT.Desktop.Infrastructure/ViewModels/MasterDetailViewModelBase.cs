using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// Master-Detail视图ViewModel基类V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    /// OpenSpec: enhance-viewmodel-architecture - 添加IViewModelServices参数
    ///
    /// 注意：由于项目依赖顺序(Models → Infrastructure)，无法直接继承CoreViewModelBase
    /// 采用组合模式：保持ObservableObject继承 + IViewModelServices参数获取通用服务
    /// FUTURE: 重构项目结构，将CoreViewModelBase移到更底层项目 (ARCH-REFACTOR)
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public abstract partial class MasterDetailViewModelBase<TListItem, TDetail> : ObservableObject, INavigationAware, IRegionMemberLifetime, IDisposable, IAsyncInitializable
        where TListItem : class
        where TDetail : class
    {
        private readonly IMasterDetailServices<TListItem, TDetail> _masterDetailServices;
        private readonly IViewModelServices _viewModelServices;
        private bool _disposed;

        /// <summary>
        /// 日志记录器（来自IViewModelServices）
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 事件聚合器（来自IViewModelServices）
        /// </summary>
        protected IEventAggregator EventAggregator => _viewModelServices.EventAggregator;

        /// <summary>
        /// Region管理器（来自IViewModelServices）
        /// </summary>
        protected IRegionManager RegionManager => _viewModelServices.RegionManager;

        /// <summary>
        /// 会话管理器（来自IViewModelServices）
        /// </summary>
        protected ISessionManager SessionManager => _viewModelServices.SessionManager;

        /// <summary>
        /// 通用对话框服务（来自IViewModelServices）
        /// </summary>
        protected ICommonDialogService CommonDialogService => _viewModelServices.CommonDialogService;

        /// <summary>
        /// Master-Detail服务
        /// </summary>
        protected IMasterDetailServices<TListItem, TDetail> MasterDetailServices => _masterDetailServices;

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
        public bool IsLoading => _masterDetailServices.Loading.IsLoading;

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        public bool IsBusy => _masterDetailServices.Loading.IsBusy;

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
        /// OpenSpec: refactor-masterdetail-command-refresh - 修复新建时DetailContent不显示的问题
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
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges => _masterDetailServices.DetailEditor.HasUnsavedChanges;

        /// <summary>
        /// 是否是新建
        /// </summary>
        public bool IsNew => _masterDetailServices.DetailEditor.IsNew;

        #endregion

        #region 委托属性 - Error

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage => _masterDetailServices.ErrorHandler.ErrorMessage;

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => _masterDetailServices.ErrorHandler.HasErrors;

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

        public virtual bool KeepAlive => false;

        /// <summary>
        /// 构造函数
        /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
        /// </summary>
        protected MasterDetailViewModelBase(
            IViewModelServices services,
            IMasterDetailServices<TListItem, TDetail> masterDetailServices)
        {
            _viewModelServices = services ?? throw new ArgumentNullException(nameof(services));
            _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
            Logger = services.LoggerFactory.CreateLogger(GetType());

            // 订阅服务事件以转发属性变更通知
            SubscribeToServiceEvents();

            // OpenSpec: refactor-masterdetail-command-refresh - 初始化时刷新命令状态
            // 初始化时服务状态都是默认值，不会触发PropertyChanged，需要主动刷新
            NotifyCommandsCanExecuteChanged();
        }

        private void SubscribeToServiceEvents()
        {
            // Loading状态变更
            _masterDetailServices.Loading.PropertyChanged += OnLoadingPropertyChanged;

            // Pagination变更
            // OpenSpec: refactor-masterdetail-command-refresh - 修复翻页按钮不生效问题
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
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(ILoadingStateManager.IsBusy))
            {
                NotifyCommandsCanExecuteChanged();
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

        private async void OnPaginationPageChanged(object? sender, EventArgs e)
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
                // OpenSpec: refactor-masterdetail-command-refresh - 选择变化时刷新ShowDetailPanel
                OnPropertyChanged(nameof(ShowDetailPanel));
            }
        }

        private async void OnSelectionSelectionChanged(object? sender, SelectionChangedEventArgs<TListItem> e)
        {
            OnSelectionChangedAsync(e).SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"Selection change failed in {GetType().Name}"));
        }

        private void OnDetailEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IDetailEditorService<TDetail>.IsEditMode))
            {
                NotifyCommandsCanExecuteChanged();
                // OpenSpec: refactor-masterdetail-command-refresh - 编辑模式变化时刷新ShowDetailPanel
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
            OnPropertyChanged(e.PropertyName);
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
        /// 删除命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        protected virtual async Task DeleteAsync()
        {
            if (SelectedItem == null) return;

            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
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
        }

        /// <summary>
        /// 通知分页命令刷新CanExecute状态
        /// OpenSpec: refactor-masterdetail-command-refresh - 修复翻页按钮不生效问题
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

        #region 初始化

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

        #endregion

        #region INavigationAware

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

        /// <summary>
        /// 导航到视图时调用（同步入口）
        /// OpenSpec: desktop-refactoring - 使用SafeFireAndForget模式替代async void
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            OnNavigatedToAsync(navigationContext).SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"OnNavigatedTo failed in {GetType().Name}"));
        }

        /// <summary>
        /// 导航到视图时调用的异步实现
        /// 子类应重写此方法而非OnNavigatedTo
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        /// <returns>异步任务</returns>
        protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)
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
                // 取消订阅事件处理器
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

            _disposed = true;
        }

        #endregion
    }
}

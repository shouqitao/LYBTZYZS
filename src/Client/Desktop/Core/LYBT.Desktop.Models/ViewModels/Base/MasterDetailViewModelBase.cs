using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// Master-Detail ViewModel基类
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 继承自UnifiedListViewModelBase，添加详情展示和编辑功能
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情类型</typeparam>
    public abstract class MasterDetailViewModelBase<TListItem, TDetail>
        : UnifiedListViewModelBase<TListItem>, IMasterDetailViewModel<TListItem, TDetail>
        where TListItem : class
        where TDetail : class
    {
        #region 私有字段

        private TDetail? _currentDetail;
        private TDetail? _originalDetail;
        private bool _isEditMode;
        private bool _isLoadingDetail;
        private bool _hasUnsavedChanges;
        private CancellationTokenSource? _loadDetailCts;
        private bool _isDisposed;
        private bool _isCreatingNew;  // OpenSpec: refactor-masterdetail-editmode - 防止新建时CurrentDetail被置空

        #endregion

        #region 公开属性

        /// <summary>当前详情数据</summary>
        public TDetail? CurrentDetail
        {
            get => _currentDetail;
            protected set
            {
                if (SetProperty(ref _currentDetail, value))
                {
                    RaisePropertyChanged(nameof(HasSelection));
                    RefreshDetailCommands();
                }
            }
        }

        /// <summary>是否处于编辑模式</summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    RefreshDetailCommands();
                }
            }
        }

        /// <summary>是否正在加载详情</summary>
        public bool IsLoadingDetail
        {
            get => _isLoadingDetail;
            protected set => SetProperty(ref _isLoadingDetail, value);
        }

        /// <summary>是否有未保存的更改</summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            protected set => SetProperty(ref _hasUnsavedChanges, value);
        }

        /// <summary>是否有选中项（重写以支持Detail视图）</summary>
        public new bool HasSelection => CurrentDetail != null;

        #endregion

        #region 命令

        /// <summary>进入编辑模式</summary>
        public DelegateCommand EditCommand { get; private set; } = null!;

        /// <summary>保存命令</summary>
        public DelegateCommand SaveCommand { get; private set; } = null!;

        /// <summary>取消编辑</summary>
        public DelegateCommand CancelCommand { get; private set; } = null!;

        /// <summary>删除当前项</summary>
        public DelegateCommand DeleteCurrentCommand { get; private set; } = null!;

        // 显式接口实现 - 详情命令
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.EditCommand => EditCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.SaveCommand => SaveCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.CancelCommand => CancelCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.DeleteCurrentCommand => DeleteCurrentCommand;

        // 显式接口实现 - 列表属性和命令（桥接UnifiedListViewModelBase类型）
        IEnumerable<TListItem> IMasterDetailViewModel<TListItem, TDetail>.Items => Items;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.SearchCommand => SearchCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.RefreshCommand => RefreshCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.AddCommand => AddCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.PreviousPageCommand => PreviousPageCommand;
        System.Windows.Input.ICommand IMasterDetailViewModel<TListItem, TDetail>.NextPageCommand => NextPageCommand;

        #endregion

        #region 构造函数

        protected MasterDetailViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            InitializeMasterDetailCommands();
        }

        #endregion

        #region 初始化

        private void InitializeMasterDetailCommands()
        {
            EditCommand = new DelegateCommand(ExecuteEdit, CanExecuteEdit);
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel, CanExecuteCancel);
            DeleteCurrentCommand = new DelegateCommand(async () => await ExecuteDeleteCurrentAsync(), CanExecuteDeleteCurrent);
        }

        #endregion

        #region 抽象方法 - 子类必须实现

        /// <summary>加载详情数据</summary>
        /// <param name="item">列表项</param>
        /// <returns>详情数据</returns>
        protected abstract Task<TDetail?> LoadDetailAsync(TListItem item);

        /// <summary>保存详情数据</summary>
        /// <param name="detail">详情数据</param>
        /// <returns>是否保存成功</returns>
        protected abstract Task<bool> SaveDetailAsync(TDetail detail);

        /// <summary>删除当前项</summary>
        /// <param name="detail">详情数据</param>
        /// <returns>是否删除成功</returns>
        protected abstract Task<bool> DeleteDetailAsync(TDetail detail);

        /// <summary>创建新的详情对象</summary>
        /// <returns>新的详情对象</returns>
        protected abstract TDetail CreateNewDetail();

        /// <summary>克隆详情对象（用于取消编辑时恢复）</summary>
        /// <param name="detail">原始详情</param>
        /// <returns>克隆的详情</returns>
        protected abstract TDetail CloneDetail(TDetail detail);

        /// <summary>从详情获取对应的列表项ID</summary>
        /// <param name="detail">详情数据</param>
        /// <returns>列表项ID</returns>
        protected abstract object? GetDetailId(TDetail detail);

        #endregion

        #region 选中项变更处理

        /// <summary>当选中项变更时加载详情</summary>
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            // 当SelectedItem变化时，加载详情
            if (SelectedItem != null)
            {
                // 启动加载详情，但不使用fire-and-forget
                // 使用SafeFireAndForget确保异常被正确处理
                SafeFireAndForgetLoadDetail();
            }
            else if (!_isCreatingNew)
            {
                // OpenSpec: refactor-masterdetail-editmode
                // 新建模式下不清空CurrentDetail，防止P0 Bug
                CancelLoadDetail();
                CurrentDetail = null;
                IsEditMode = false;
            }
        }

        /// <summary>安全地启动详情加载（非阻塞但有异常处理）</summary>
        private async void SafeFireAndForgetLoadDetail()
        {
            try
            {
                await LoadDetailForSelectedItemAsync();
            }
            catch (OperationCanceledException)
            {
                // 操作被取消是正常情况，不需要处理
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载详情时发生未处理异常");
                if (!_isDisposed)
                {
                    RunOnUIThreadSafe(() =>
                    {
                        IsLoadingDetail = false;
                        ErrorMessage = "加载详情失败";
                    });
                }
            }
        }

        /// <summary>取消当前的详情加载操作</summary>
        private void CancelLoadDetail()
        {
            if (_loadDetailCts != null)
            {
                _loadDetailCts.Cancel();
                _loadDetailCts.Dispose();
                _loadDetailCts = null;
            }
        }

        private async Task LoadDetailForSelectedItemAsync()
        {
            if (SelectedItem == null || _isDisposed) return;

            // 取消上一次的加载操作
            CancelLoadDetail();
            _loadDetailCts = new CancellationTokenSource();
            var cancellationToken = _loadDetailCts.Token;

            // 捕获当前选中项，防止加载过程中选中项变化
            var currentSelectedItem = SelectedItem;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Logger.LogInformation("[VM] LoadDetail started - {ItemType}", typeof(TListItem).Name);

            try
            {
                RunOnUIThreadSafe(() => IsLoadingDetail = true);

                // 检查是否已取消
                cancellationToken.ThrowIfCancellationRequested();

                var detail = await LoadDetailAsync(currentSelectedItem);

                // 再次检查是否已取消或已销毁
                cancellationToken.ThrowIfCancellationRequested();
                if (_isDisposed) return;

                RunOnUIThreadSafe(() =>
                {
                    // 确保选中项没有变化
                    if (SelectedItem == currentSelectedItem)
                    {
                        CurrentDetail = detail;
                        IsEditMode = false;
                        HasUnsavedChanges = false;
                    }
                    IsLoadingDetail = false;
                });

                sw.Stop();
                Logger.LogInformation("[VM] LoadDetail completed - {ItemType} Duration={Duration}ms", 
                    typeof(TListItem).Name, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，不需要重置IsLoadingDetail（新的加载会处理）
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "[VM] LoadDetail failed - {ItemType} Duration={Duration}ms", 
                    typeof(TListItem).Name, sw.ElapsedMilliseconds);
                if (!_isDisposed)
                {
                    RunOnUIThreadSafe(() =>
                    {
                        IsLoadingDetail = false;
                        ErrorMessage = "加载详情失败，请重试";
                    });
                }
                // 不重新抛出异常，已经处理了
            }
        }

        /// <summary>安全地在UI线程执行操作（处理已销毁情况）</summary>
        private void RunOnUIThreadSafe(Action action)
        {
            if (_isDisposed) return;

            try
            {
                var app = System.Windows.Application.Current;
                if (app != null && app.Dispatcher != null && !app.Dispatcher.HasShutdownStarted)
                {
                    app.Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "在UI线程执行操作时发生异常（可能是页面已销毁）");
            }
        }

        #endregion

        #region 编辑模式操作

        private void ExecuteEdit()
        {
            if (CurrentDetail == null) return;

            // 保存原始数据用于取消时恢复
            _originalDetail = CloneDetail(CurrentDetail);
            IsEditMode = true;
            HasUnsavedChanges = false;
        }

        private bool CanExecuteEdit() => CurrentDetail != null && !IsEditMode && !IsLoading;

        private async Task ExecuteSaveAsync()
        {
            if (CurrentDetail == null) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Logger.LogInformation("[VM] Save started - {DetailType}", typeof(TDetail).Name);

            await ExecuteSafelyAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    var success = await SaveDetailAsync(CurrentDetail);
                    if (success)
                    {
                        RunOnUIThread(() =>
                        {
                            IsEditMode = false;
                            HasUnsavedChanges = false;
                            _originalDetail = null;
                        });

                        // 刷新列表
                        await RefreshAsync();

                        sw.Stop();
                        Logger.LogInformation("[VM] Save completed - {DetailType} Duration={Duration}ms", 
                            typeof(TDetail).Name, sw.ElapsedMilliseconds);

                        await ShowSuccessMessageAsync("保存成功");
                    }
                    else
                    {
                        sw.Stop();
                        Logger.LogWarning("[VM] Save failed - {DetailType} Duration={Duration}ms Error={Error}", 
                            typeof(TDetail).Name, sw.ElapsedMilliseconds, ErrorMessage ?? "未知错误");

                        // Issue #2261: 保存失败时显示错误提示
                        var errorMsg = string.IsNullOrWhiteSpace(ErrorMessage) ? "保存失败，请重试" : ErrorMessage;
                        await ShowErrorMessageAsync(errorMsg);
                    }
                }
                finally
                {
                    IsLoading = false;
                }
            }, "保存数据");
        }

        private bool CanExecuteSave() => CurrentDetail != null && IsEditMode && !IsLoading;

        private void ExecuteCancel()
        {
            if (_originalDetail != null)
            {
                CurrentDetail = _originalDetail;
                _originalDetail = null;
            }

            IsEditMode = false;
            HasUnsavedChanges = false;
        }

        private bool CanExecuteCancel() => IsEditMode;

        private async Task ExecuteDeleteCurrentAsync()
        {
            if (CurrentDetail == null) return;

            var confirmed = await ShowConfirmationAsync("确认删除此记录吗？\n此操作不可恢复。", "删除确认");
            if (!confirmed) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Logger.LogInformation("[VM] Delete started - {DetailType}", typeof(TDetail).Name);

            await ExecuteSafelyAsync(async () =>
            {
                IsLoading = true;
                try
                {
                    var success = await DeleteDetailAsync(CurrentDetail);
                    if (success)
                    {
                        RunOnUIThread(() =>
                        {
                            CurrentDetail = null;
                            SelectedItem = null;
                            IsEditMode = false;
                        });

                        // 刷新列表
                        await RefreshAsync();

                        sw.Stop();
                        Logger.LogInformation("[VM] Delete completed - {DetailType} Duration={Duration}ms", 
                            typeof(TDetail).Name, sw.ElapsedMilliseconds);

                        await ShowSuccessMessageAsync("删除成功");
                    }
                    else
                    {
                        sw.Stop();
                        Logger.LogWarning("[VM] Delete failed - {DetailType} Duration={Duration}ms Error={Error}", 
                            typeof(TDetail).Name, sw.ElapsedMilliseconds, ErrorMessage ?? "未知错误");

                        // Issue #2261: 删除失败时显示错误提示
                        var errorMsg = string.IsNullOrWhiteSpace(ErrorMessage) ? "删除失败，请重试" : ErrorMessage;
                        await ShowErrorMessageAsync(errorMsg);
                    }
                }
                finally
                {
                    IsLoading = false;
                }
            }, "删除数据");
        }

        private bool CanExecuteDeleteCurrent() => CurrentDetail != null && !IsLoading;

        #endregion

        #region 新增操作

        /// <summary>
        /// 执行新增操作
        /// OpenSpec: refactor-masterdetail-editmode - 使用_isCreatingNew标志防止CurrentDetail被置空
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            // 创建新的详情对象
            var newDetail = CreateNewDetail();

            // 设置标志防止RefreshCanExecuteChanged清空CurrentDetail
            _isCreatingNew = true;
            try
            {
                RunOnUIThread(() =>
                {
                    SelectedItem = null;  // 先清空选中项
                    CurrentDetail = newDetail;  // 后设置详情
                    IsEditMode = true;
                    HasUnsavedChanges = false;
                });
            }
            finally
            {
                _isCreatingNew = false;
            }

            await Task.CompletedTask;
        }

        #endregion

        #region 命令刷新

        private void RefreshDetailCommands()
        {
            EditCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            CancelCommand?.RaiseCanExecuteChanged();
            DeleteCurrentCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 属性变更通知

        /// <summary>标记数据已修改</summary>
        protected void MarkAsModified()
        {
            if (IsEditMode)
            {
                HasUnsavedChanges = true;
            }
        }

        #endregion

        #region 导航与资源清理

        /// <summary>离开页面时取消正在进行的操作</summary>
        public override void OnNavigatedFrom(Prism.Regions.NavigationContext navigationContext)
        {
            // 取消正在进行的加载操作，防止导航后继续更新UI
            CancelLoadDetail();

            // 重置加载状态
            IsLoadingDetail = false;

            base.OnNavigatedFrom(navigationContext);
        }

        /// <summary>释放资源</summary>
        protected override void OnDisposing()
        {
            _isDisposed = true;

            // 取消正在进行的加载操作
            CancelLoadDetail();

            base.OnDisposing();
        }

        #endregion
    }
}

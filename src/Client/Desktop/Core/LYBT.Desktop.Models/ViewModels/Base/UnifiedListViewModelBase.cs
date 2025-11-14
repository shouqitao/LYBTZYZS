using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 统一列表ViewModel基类 - UltraThink架构重构版本
    /// 提供通用的列表操作功能：分页、搜索、选择、批量操作等
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public abstract class UnifiedListViewModelBase<T> : UnifiedViewModelBase
        where T : class
    {
        #region 列表属性

        private ObservableCollection<T> _items = new();
        private ObservableCollection<T> _selectedItems = new();
        private T? _selectedItem;
        private string _searchText = string.Empty;
        private int _totalCount = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private bool _hasSelection = false;
        private string _busyMessage = "正在加载...";

        // 防抖相关字段
        private CancellationTokenSource? _searchCancellationTokenSource;
    

        /// <summary>
        /// 列表项集合
        /// </summary>
        public ObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 选中的项目集合
        /// </summary>
        public ObservableCollection<T> SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (SetProperty(ref _selectedItems, value))
                {
                    HasSelection = value?.Count > 0;
                    RefreshCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 当前选中项
        /// </summary>
        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    RefreshCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchWithDebounceAsync();
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            protected set => SetProperty(ref _totalCount, value);
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadPageAsync();
                }
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>
        /// 是否有选择项
        /// </summary>
        public bool HasSelection
        {
            get => _hasSelection;
            private set => SetProperty(ref _hasSelection, value);
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// 是否可以上一页
        /// </summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以下一页
        /// </summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 忙碌状态消息
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; private set; } = null!;

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; } = null!;

        /// <summary>
        /// 添加命令
        /// </summary>
        public DelegateCommand AddCommand { get; private set; } = null!;

        /// <summary>
        /// 删除命令
        /// </summary>
        public DelegateCommand<T> DeleteCommand { get; private set; } = null!;

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; } = null!;

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        /// <summary>
        /// 批量删除命令
        /// </summary>
        public DelegateCommand BatchDeleteCommand { get; private set; } = null!;

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; private set; } = null!;

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; private set; } = null!;

        /// <summary>
        /// 清除搜索命令
        /// </summary>
        public DelegateCommand ClearSearchCommand { get; private set; } = null!;

        #endregion

        #region 构造函数

        protected UnifiedListViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            InitializeListCommands();
        }

        #endregion

        #region 命令初始化

        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            InitializeListCommands();
        }

        private void InitializeListCommands()
        {
            SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => !IsLoading);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsLoading);
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), CanExecuteAdd);
            DeleteCommand = new DelegateCommand<T>(async item => await ExecuteDeleteAsync(item), CanExecuteDelete);
            BatchDeleteCommand = new DelegateCommand(async () => await ExecuteBatchDeleteAsync(), CanExecuteBatchDelete);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, () => CanGoPreviousPage && !IsLoading);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, () => CanGoNextPage && !IsLoading);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch, () => !string.IsNullOrEmpty(SearchText));
        }

        #endregion

        #region 虚方法 - 子类实现

        /// <summary>
        /// 获取数据项（子类必须实现）
        /// </summary>
        protected abstract Task<IEnumerable<T>> GetItemsAsync(int page, int pageSize, string? searchText);

        /// <summary>
        /// 执行添加操作
        /// </summary>
        protected virtual async Task OnExecuteAddAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected virtual async Task OnExecuteDeleteAsync(T item)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行批量删除操作
        /// </summary>
        protected virtual async Task OnExecuteBatchDeleteAsync(List<T> items)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载当前页数据
        /// </summary>
        public async Task LoadPageAsync(bool showLoading = true)
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 智能加载状态：仅在首次加载或明确要求时显示加载蒙板
                // 搜索时分页通常很快，避免不必要的闪烁
                if (showLoading)
                {
                    IsLoading = true;
                }

                try
                {
                    var items = await GetItemsAsync(CurrentPage, PageSize, SearchText);

                    RunOnUIThread(() =>
                    {
                        // 批量更新Items，避免UI多次重绘
                        var newItems = new ObservableCollection<T>(items);
                        Items = newItems;

                        RefreshPagingProperties();
                    });
                }
                finally
                {
                    if (showLoading)
                    {
                        IsLoading = false;
                    }
                }

            }, "加载数据");
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public async Task SearchAsync()
        {
            CurrentPage = 1; // 重置到第一页
            await LoadPageAsync(false); // 搜索时不显示加载状态，避免UI闪烁
        }

        /// <summary>
        /// 刷新（不显示加载状态）
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadPageAsync(false);
        }

        /// <summary>
        /// 强制刷新（显示加载状态）
        /// </summary>
        public async Task ForceRefreshAsync()
        {
            await LoadPageAsync(true);
        }

        #endregion

        #region 私有方法
        
        /// <summary>
        /// 防抖搜索 - 延迟200ms后执行搜索，避免频繁请求
        /// </summary>
        private async Task SearchWithDebounceAsync()
        {
            // 取消之前的搜索任务
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 等待200ms防抖，减少延迟感
                await Task.Delay(200, _searchCancellationTokenSource.Token);
                
                // 执行搜索
                await SearchAsync();
            }
            catch (OperationCanceledException)
            {
                // 搜索被取消，忽略
            }
        }
        
  
        /// <summary>
        /// 刷新分页属性
        /// </summary>
        private void RefreshPagingProperties()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CanGoPreviousPage));
            RaisePropertyChanged(nameof(CanGoNextPage));
            RefreshCanExecuteChanged();
        }

        /// <summary>
        /// 执行删除
        /// </summary>
        private async Task ExecuteDeleteAsync(T item)
        {
            if (item == null) return;

            await ExecuteSafelyAsync(async () =>
            {
                await OnExecuteDeleteAsync(item);
                await RefreshAsync();
            }, "删除项目");
        }

        /// <summary>
        /// 执行批量删除
        /// </summary>
        private async Task ExecuteBatchDeleteAsync()
        {
            var itemsToDelete = SelectedItems.ToList();
            if (itemsToDelete.Count == 0) return;

            await ExecuteSafelyAsync(async () =>
            {
                await OnExecuteBatchDeleteAsync(itemsToDelete);
                SelectedItems.Clear();
                await RefreshAsync();
            }, $"批量删除{itemsToDelete.Count}个项目");
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void ExecutePreviousPage()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void ExecuteNextPage()
        {
            if (CanGoNextPage)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// 末页
        /// </summary>
        private void ExecuteLastPage()
        {
            if (CanGoNextPage && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        private void ExecuteClearSearch()
        {
            SearchText = string.Empty;
        }

        #endregion

        #region 命令条件判断

        /// <summary>
        /// 是否可以添加
        /// </summary>
        protected virtual bool CanExecuteAdd()
        {
            return !IsLoading;
        }

        /// <summary>
        /// 是否可以删除
        /// </summary>
        protected virtual bool CanExecuteDelete(T item)
        {
            return item != null && !IsLoading;
        }

        /// <summary>
        /// 是否可以批量删除
        /// </summary>
        protected virtual bool CanExecuteBatchDelete()
        {
            return HasSelection && !IsLoading;
        }

        #endregion

        #region 命令刷新

        protected override void RefreshCommands()
        {
            base.RefreshCommands();
            RefreshCanExecuteChanged();
        }

        protected virtual void RefreshCanExecuteChanged()
        {
            SearchCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
            AddCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            BatchDeleteCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
            ClearSearchCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 设置错误和状态

        /// <summary>
        /// 设置错误信息
        /// </summary>
        protected void SetError(string message, string? propertyName = null)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                AddValidationError(propertyName, message);
            }
            else
            {
                ErrorMessage = message;
            }
        }

        /// <summary>
        /// 清除错误信息
        /// </summary>
        protected void ClearError(string? propertyName = null)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                ClearValidationErrors(propertyName);
            }
            else
            {
                ClearError();
            }
        }

        #endregion

        #region 导航处理

        /// <summary>
        /// 页面导航进入时自动加载数据
        /// Issue #1240: 使用 InitializeAsync 替代 OnNavigatedToAsync
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 自动加载第一页数据，不显示加载状态避免页面切换闪烁
            await LoadPageAsync(false);
        }

        #endregion
    }
}

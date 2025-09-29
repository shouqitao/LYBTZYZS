using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 统一的ViewModel基类 - 简化MVP版本
    /// 合并了原有的NavigationViewModelBase、ListViewModelBase和ListPageViewModel的核心功能
    /// 消除重复代码，提供MVP所需的基础功能
    /// </summary>
    public abstract class UnifiedViewModelBase : ModernViewModelBase, INavigationAware, IRegionMemberLifetime
    {
        #region 依赖服务

        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;

        #endregion

        #region 基础属性

        private string _pageTitle = string.Empty;
        private bool _isNavigating = false;

        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        /// <summary>
        /// 是否正在导航
        /// </summary>
        public bool IsNavigating
        {
            get => _isNavigating;
            protected set => SetProperty(ref _isNavigating, value);
        }

        /// <summary>
        /// 是否保持存活（默认为false）
        /// </summary>
        public virtual bool KeepAlive => false;

        #endregion

        #region MVP验证支持 - 简化版本

        /// <summary>
        /// 是否有验证错误 - MVP简化版本
        /// </summary>
        public virtual bool HasErrors => false;

        #endregion

        #region 基础命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; }

        #endregion

        #region 构造函数

        protected UnifiedViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            SessionManager = sessionManager;

            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
        }

        #endregion

        #region INavigationAware实现

        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到页面: {PageType}", GetType().Name);

            ProcessNavigationParameters(navigationContext.Parameters);

            // 异步加载数据
            Task.Run(async () =>
            {
                try
                {
                    IsNavigating = true;
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "页面加载失败");
                    var context = new ErrorContext { Operation = "页面加载", Module = GetType().Name };
                    _ = ErrorHandlingService?.HandleExceptionAsync(ex, context);
                }
                finally
                {
                    IsNavigating = false;
                }
            });
        }

        /// <summary>
        /// 从此页面导航离开时调用
        /// </summary>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("从页面导航离开: {PageType}", GetType().Name);
        }

        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return KeepAlive;
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载页面数据
        /// </summary>
        protected virtual async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();

                await OnLoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载数据失败");
                var context = new ErrorContext { Operation = "加载数据", Module = GetType().Name };
                _ = ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 子类重写以实现具体的数据加载逻辑
        /// </summary>
        protected virtual Task OnLoadDataAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行刷新
        /// </summary>
        protected virtual async Task ExecuteRefreshAsync()
        {
            await LoadDataAsync();
        }

        #endregion

        #region 导航参数处理

        /// <summary>
        /// 处理导航参数
        /// </summary>
        protected virtual void ProcessNavigationParameters(Prism.Regions.NavigationParameters parameters)
        {
            // 尝试获取页面标题
            if (parameters.TryGetValue("title", out object titleObj) && titleObj is string title)
            {
                PageTitle = title;
            }
        }

        #endregion

        #region 导航辅助方法

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        protected void NavigateTo(string regionName, string viewName, Prism.Regions.NavigationParameters? parameters = null)
        {
            RegionManager.RequestNavigate(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), parameters);
        }

        #endregion

        #region 会话支持

        /// <summary>
        /// 获取当前用户
        /// </summary>
        protected LYBT.Shared.Models.Contracts.Users.UserDto? GetCurrentUser()
        {
            return SessionManager?.CurrentUser;
        }

        /// <summary>
        /// 获取当前患者
        /// </summary>
        protected LYBT.Shared.Models.Contracts.Patients.PatientDto? GetCurrentPatient()
        {
            return SessionManager?.CurrentPatient;
        }

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        protected bool IsAuthenticated()
        {
            return SessionManager?.IsLoggedIn ?? false;
        }

        #endregion
    }

    /// <summary>
    /// 统一的列表ViewModel基类 - 简化MVP版本
    /// 提供列表管理的核心功能，避免过度抽象
    /// </summary>
    public abstract class UnifiedListViewModelBase<T> : UnifiedViewModelBase where T : class
    {
        #region 列表字段

        private readonly ObservableCollection<T> _items;
        private readonly ObservableCollection<T> _selectedItems;
        private ICollectionView? _itemsView;
        private T? _selectedItem;
        private string _searchText = string.Empty;

        #endregion

        #region 分页字段

        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;

        #endregion

        #region 列表属性

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items => _items;

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<T> SelectedItems => _selectedItems;

        /// <summary>
        /// 列表视图
        /// </summary>
        public ICollectionView ItemsView => _itemsView ??= CollectionViewSource.GetDefaultView(_items);

        /// <summary>
        /// 当前选中项
        /// </summary>
        public T? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
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
                    _ = Task.Delay(300).ContinueWith(async _ =>
                    {
                        if (_searchText == value)
                        {
                            await SearchAsync();
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems => _selectedItems.Count > 0;

        /// <summary>
        /// 是否为空列表
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        #endregion

        #region 分页属性

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
                    RefreshPaginationProperties();
                    _ = LoadDataAsync();
                }
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            protected set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    RefreshPaginationProperties();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>
        /// 分页信息
        /// </summary>
        public string PaginationInfo
        {
            get
            {
                if (TotalCount == 0) return "无数据";
                var startIndex = (CurrentPage - 1) * PageSize + 1;
                var endIndex = Math.Min(CurrentPage * PageSize, TotalCount);
                return $"第 {startIndex}-{endIndex} 项，共 {TotalCount} 项";
            }
        }

        #endregion

        #region 列表命令

        /// <summary>
        /// 添加命令
        /// </summary>
        public DelegateCommand AddCommand { get; private set; }

        /// <summary>
        /// 删除命令
        /// </summary>
        public DelegateCommand<T> DeleteCommand { get; private set; }

        /// <summary>
        /// 批量删除命令
        /// </summary>
        public DelegateCommand BatchDeleteCommand { get; private set; }

        /// <summary>
        /// 清空搜索命令
        /// </summary>
        public DelegateCommand ClearSearchCommand { get; private set; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; private set; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; private set; }

        #endregion

        #region 构造函数

        protected UnifiedListViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _items = new ObservableCollection<T>();
            _selectedItems = new ObservableCollection<T>();

            InitializeListCommands();
            SetupCollectionEvents();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化列表命令
        /// </summary>
        private void InitializeListCommands()
        {
            AddCommand = new DelegateCommand(async () => await ExecuteAddAsync(), () => !IsLoading);
            DeleteCommand = new DelegateCommand<T>(async item => await ExecuteDeleteAsync(item), item => item != null && !IsLoading);
            BatchDeleteCommand = new DelegateCommand(async () => await ExecuteBatchDeleteAsync(), () => HasSelectedItems && !IsLoading);
            ClearSearchCommand = new DelegateCommand(() => SearchText = string.Empty, () => !string.IsNullOrEmpty(SearchText));
            PreviousPageCommand = new DelegateCommand(() => CurrentPage--, () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(() => CurrentPage++, () => CurrentPage < TotalPages);
        }

        /// <summary>
        /// 设置集合事件
        /// </summary>
        private void SetupCollectionEvents()
        {
            _items.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(IsEmpty));
            };

            _selectedItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(HasSelectedItems));
                RefreshCanExecuteChanged();
            };
        }

        #endregion

        #region 数据加载重写

        /// <summary>
        /// 加载数据
        /// </summary>
        protected override async Task OnLoadDataAsync()
        {
            var items = await GetItemsAsync(CurrentPage, PageSize, SearchText);

            _items.Clear();
            foreach (var item in items)
            {
                _items.Add(item);
            }

            // 如果需要分页，子类可以重写设置TotalCount
        }

        /// <summary>
        /// 搜索
        /// </summary>
        protected virtual async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        /// <summary>
        /// 子类实现：获取数据项
        /// </summary>
        protected abstract Task<IEnumerable<T>> GetItemsAsync(int page, int pageSize, string? searchText);

        #endregion

        #region 命令实现

        /// <summary>
        /// 执行添加
        /// </summary>
        protected virtual async Task ExecuteAddAsync()
        {
            try
            {
                await OnExecuteAddAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "添加失败");
                var context = new ErrorContext { Operation = "添加", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 子类重写：添加逻辑
        /// </summary>
        protected virtual Task OnExecuteAddAsync() => Task.CompletedTask;

        /// <summary>
        /// 执行删除
        /// </summary>
        protected virtual async Task ExecuteDeleteAsync(T item)
        {
            if (item == null) return;

            try
            {
                await OnExecuteDeleteAsync(item);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除失败");
                var context = new ErrorContext { Operation = "删除", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 子类重写：删除逻辑
        /// </summary>
        protected virtual Task OnExecuteDeleteAsync(T item) => Task.CompletedTask;

        /// <summary>
        /// 执行批量删除
        /// </summary>
        protected virtual async Task ExecuteBatchDeleteAsync()
        {
            if (!HasSelectedItems) return;

            try
            {
                var itemsToDelete = SelectedItems.ToList();
                await OnExecuteBatchDeleteAsync(itemsToDelete);

                _selectedItems.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除失败");
                var context = new ErrorContext { Operation = "批量删除", Module = GetType().Name };
                await ErrorHandlingService?.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 子类重写：批量删除逻辑
        /// </summary>
        protected virtual Task OnExecuteBatchDeleteAsync(List<T> items) => Task.CompletedTask;

        #endregion

        #region 辅助方法

        /// <summary>
        /// 刷新分页属性
        /// </summary>
        private void RefreshPaginationProperties()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(PaginationInfo));
        }

        /// <summary>
        /// 刷新命令可执行状态
        /// </summary>
        protected virtual void RefreshCanExecuteChanged()
        {
            AddCommand?.RaiseCanExecuteChanged();
            BatchDeleteCommand?.RaiseCanExecuteChanged();
            ClearSearchCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region MVP兼容方法 - 简化版本

        /// <summary>
        /// 添加验证错误 - MVP简化版本（空实现）
        /// </summary>
        protected virtual void AddValidationError(string propertyName, string errorMessage)
        {
            // MVP简化版本：不实现复杂验证逻辑
        }

        /// <summary>
        /// 清除指定属性的验证错误 - MVP简化版本（空实现）
        /// </summary>
        protected virtual void ClearValidationErrors(string propertyName)
        {
            // MVP简化版本：不实现复杂验证逻辑
        }

        /// <summary>
        /// 清除所有验证错误 - MVP简化版本（空实现）
        /// </summary>
        protected virtual void ClearValidationErrors()
        {
            // MVP简化版本：不实现复杂验证逻辑
        }

        /// <summary>
        /// 添加LoadPageAsync方法以兼容旧代码
        /// </summary>
        protected virtual async Task LoadPageAsync()
        {
            await LoadDataAsync();
        }


        #endregion

        #region 清理

        protected override void OnDisposing()
        {
            _items.Clear();
            _selectedItems.Clear();
            base.OnDisposing();
        }

        #endregion
    }
}
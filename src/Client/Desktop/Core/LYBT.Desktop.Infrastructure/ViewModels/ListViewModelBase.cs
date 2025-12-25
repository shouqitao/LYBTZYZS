using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// 列表视图ViewModel基类（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用IListViewServices进行组合，委托功能给注入的服务
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public abstract partial class ListViewModelBase<T> : ObservableObject, INavigationAware, IRegionMemberLifetime, IDisposable
        where T : class
    {
        private readonly IListViewServices<T> _services;
        private bool _disposed;

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 列表视图服务
        /// </summary>
        protected IListViewServices<T> Services => _services;

        /// <summary>
        /// 数据列表
        /// </summary>
        public ObservableCollection<T> Items { get; } = new();

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
        public T? SelectedItem
        {
            get => _services.Selection.SelectedItem;
            set => _services.Selection.Select(value);
        }

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<T> SelectedItems => _services.Selection.SelectedItems;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => _services.Selection.HasSelection;

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

        protected ListViewModelBase(
            IListViewServices<T> services,
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
                await LoadDataAsync();
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

            _services.Selection.SelectionChanged += OnSelectionChanged;

            // Error变更
            _services.ErrorHandler.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
            };
        }

        /// <summary>
        /// 选择变更时调用
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        protected virtual void OnSelectionChanged(object? sender, SelectionChangedEventArgs<T> e) { }

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        [RelayCommand]
        protected virtual async Task RefreshAsync()
        {
            _services.Pagination.Reset();
            await LoadDataAsync();
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
                await LoadDataAsync();
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
            await LoadDataAsync();
        }

        /// <summary>
        /// 首页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        protected virtual async Task GoToFirstPageAsync()
        {
            _services.Pagination.GoToFirstPage();
            await LoadDataAsync();
        }

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        protected virtual async Task GoToPreviousPageAsync()
        {
            _services.Pagination.GoToPreviousPage();
            await LoadDataAsync();
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        protected virtual async Task GoToNextPageAsync()
        {
            _services.Pagination.GoToNextPage();
            await LoadDataAsync();
        }

        /// <summary>
        /// 末页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        protected virtual async Task GoToLastPageAsync()
        {
            _services.Pagination.GoToLastPage();
            await LoadDataAsync();
        }

        private bool CanGoToFirstPage() => _services.Pagination.CanGoToFirstPage;
        private bool CanGoToPreviousPage() => _services.Pagination.CanGoToPreviousPage;
        private bool CanGoToNextPage() => _services.Pagination.CanGoToNextPage;
        private bool CanGoToLastPage() => _services.Pagination.CanGoToLastPage;

        #endregion

        #region 抽象方法

        /// <summary>
        /// 加载数据 - 子类必须实现
        /// </summary>
        /// <returns>任务</returns>
        protected abstract Task LoadDataAsync();

        #endregion

        #region INavigationAware

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到列表视图: {ViewType}", GetType().Name);
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

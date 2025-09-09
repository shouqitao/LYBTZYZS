using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Core.Controls
{

    /// <summary>
    /// 支持虚拟化和懒加载的数据网格控件
    /// </summary>
    public partial class VirtualizedDataGrid : UserControl
    {
        private bool _isNearBottom;
        private double _lastVerticalOffset;

        public VirtualizedDataGrid()
        {
            InitializeComponent();

            // 设置DataContext为内部ViewModel
            DataContext = new VirtualizedDataGridViewModel();
        }

        #region 依赖属性

        /// <summary>
        /// 数据源属性
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(System.Collections.IEnumerable),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 数据加载委托属性
        /// </summary>
        public static readonly DependencyProperty LoadDataAsyncProperty =
            DependencyProperty.Register(
                nameof(LoadDataAsync),
                typeof(Func<int, int, string, Task<PagedDataResult>>),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null, OnLoadDataAsyncChanged));

        public Func<int, int, string, Task<PagedDataResult>> LoadDataAsync
        {
            get => (Func<int, int, string, Task<PagedDataResult>>)GetValue(LoadDataAsyncProperty);
            set => SetValue(LoadDataAsyncProperty, value);
        }

        /// <summary>
        /// 选中项属性
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        #endregion 依赖属性

        #region 事件处理

        /// <summary>
        /// 数据源改变处理
        /// </summary>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedDataGrid control && control.DataContext is VirtualizedDataGridViewModel viewModel)
            {
                viewModel.SetItemsSource(e.NewValue as System.Collections.IEnumerable);
            }
        }

        /// <summary>
        /// 数据加载委托改变处理
        /// </summary>
        private static void OnLoadDataAsyncChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedDataGrid control && control.DataContext is VirtualizedDataGridViewModel viewModel)
            {
                viewModel.SetLoadDataAsync(e.NewValue as Func<int, int, string, Task<PagedDataResult>>);
            }
        }

        /// <summary>
        /// DataGrid滚动事件处理（实现懒加载）
        /// </summary>
        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Fire-and-forget pattern with exception handling
            _ = Task.Run(async () =>
            {
                try
                {
                    if (!(DataContext is VirtualizedDataGridViewModel viewModel) || viewModel.IsLoading)
                    {
                        return;
                    }

                    var scrollViewer = e.OriginalSource as ScrollViewer;
                    if (scrollViewer == null)
                    {
                        return;
                    }

                    // 计算滚动位置
                    var verticalOffset = scrollViewer.VerticalOffset;
                    var scrollableHeight = scrollViewer.ScrollableHeight;

                    // 接近底部的阈值（距底部20%时开始预加载）
                    var nearBottomThreshold = scrollableHeight * 0.8;
                    var isCurrentlyNearBottom = verticalOffset >= nearBottomThreshold;

                    // 检测向下滚动且接近底部
                    if (isCurrentlyNearBottom && !_isNearBottom && verticalOffset > _lastVerticalOffset)
                    {
                        _isNearBottom = true;

                        // 触发懒加载
                        if (viewModel.CanLoadMore)
                        {
                            await viewModel.LoadNextPageAsync();
                        }
                    }
                    else if (!isCurrentlyNearBottom)
                    {
                        _isNearBottom = false;
                    }

                    _lastVerticalOffset = verticalOffset;

                    // 虚拟化优化：根据滚动速度调整渲染策略
                    var scrollSpeed = Math.Abs(e.VerticalChange);
                    if (scrollSpeed > 50) // 快速滚动时
                    {
                        // 可以在这里添加快速滚动优化逻辑
                        // 例如降低渲染频率、简化数据模板等
                    }
                }
                catch (Exception ex)
                {
                    // 记录滚动处理错误，避免影响用户体验
                    System.Diagnostics.Debug.WriteLine($"滚动处理失败: {ex.Message}");
                }
            });
        }

        #endregion 事件处理
    }

    /// <summary>
    /// 虚拟化数据网格的ViewModel
    /// </summary>
    public class VirtualizedDataGridViewModel : INotifyPropertyChanged
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
        private ILogger<VirtualizedDataGridViewModel>? _logger;
#pragma warning restore CS0649
        private ObservableCollection<object> _items = new();
        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalItems;
        private int _totalPages;
        private object? _selectedItem;
        private Func<int, int, string, Task<PagedDataResult>>? _loadDataAsync;

        public VirtualizedDataGridViewModel()
        {
            Items = new ObservableCollection<object>();

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearch);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage);
            GoToPageCommand = new DelegateCommand<int>(ExecuteGoToPage);
        }

        #region 属性

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<object> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    _ = LoadDataAsync(1); // 重新加载第一页
                }
            }
        }

        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (SetProperty(ref _totalItems, value))
                {
                    TotalPages = (int)Math.Ceiling((double)value / PageSize);
                    OnPropertyChanged(nameof(TotalItemsText));
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// 总项目数文本
        /// </summary>
        public string TotalItemsText => $"共 {TotalItems} 条记录";

        /// <summary>
        /// 是否可以加载更多
        /// </summary>
        public bool CanLoadMore => CurrentPage < TotalPages;

        /// <summary>
        /// 是否可以转到上一页
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以转到下一页
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// 页码列表（用于显示页码按钮）
        /// </summary>
        public ObservableCollection<PageNumberInfo> PageNumbers { get; } = new();

        #endregion 属性

        #region 命令

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand GoToPageCommand { get; }

        #endregion 命令

        #region 公共方法

        /// <summary>
        /// 设置数据源
        /// </summary>
        public void SetItemsSource(System.Collections.IEnumerable? itemsSource)
        {
            Items.Clear();
            if (itemsSource != null)
            {
                foreach (var item in itemsSource)
                {
                    Items.Add(item);
                }
            }
        }

        /// <summary>
        /// 设置数据加载委托
        /// </summary>
        public void SetLoadDataAsync(Func<int, int, string, Task<PagedDataResult>>? loadDataAsync)
        {
            _loadDataAsync = loadDataAsync;
            if (loadDataAsync != null)
            {
                _ = LoadDataAsync(1); // 自动加载第一页
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public async Task LoadDataAsync(int pageIndex)
        {
            if (_loadDataAsync == null || IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                CurrentPage = pageIndex;

                var result = await _loadDataAsync(pageIndex, PageSize, SearchKeyword);

                // 更新数据
                Items.Clear();
                if (result.Items != null)
                {
                    foreach (var item in result.Items)
                    {
                        Items.Add(item);
                    }
                }

                TotalItems = result.TotalCount;
                UpdatePageNumbers();

                _logger?.LogDebug("加载数据完成: 页码 {Page}, 数量 {Count}", pageIndex, Items.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载数据失败: 页码 {Page}", pageIndex);

                // 这里可以显示错误消息
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载下一页（用于懒加载）
        /// </summary>
        public async Task LoadNextPageAsync()
        {
            if (!CanLoadMore || IsLoading)
            {
                return;
            }

            await LoadDataAsync(CurrentPage + 1);
        }

        #endregion 公共方法

        #region 命令实现

        private void ExecuteSearch()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteSearchAsync();
        }

        private async Task ExecuteSearchAsync()
        {
            try
            {
                await LoadDataAsync(1);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "搜索操作失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecuteRefresh()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteRefreshAsync();
        }

        private async Task ExecuteRefreshAsync()
        {
            try
            {
                await LoadDataAsync(CurrentPage);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新操作失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecuteFirstPage()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteFirstPageAsync();
        }

        private async Task ExecuteFirstPageAsync()
        {
            try
            {
                await LoadDataAsync(1);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "首页导航失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecutePreviousPage()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecutePreviousPageAsync();
        }

        private async Task ExecutePreviousPageAsync()
        {
            try
            {
                if (CanGoToPreviousPage)
                {
                    await LoadDataAsync(CurrentPage - 1);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "上一页导航失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecuteNextPage()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteNextPageAsync();
        }

        private async Task ExecuteNextPageAsync()
        {
            try
            {
                if (CanGoToNextPage)
                {
                    await LoadDataAsync(CurrentPage + 1);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "下一页导航失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecuteLastPage()
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteLastPageAsync();
        }

        private async Task ExecuteLastPageAsync()
        {
            try
            {
                await LoadDataAsync(TotalPages);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "末页导航失败");

                // 可以在这里添加用户通知逻辑
            }
        }

        private void ExecuteGoToPage(int pageNumber)
        {
            // Fire-and-forget pattern with proper async handling
            _ = ExecuteGoToPageAsync(pageNumber);
        }

        private async Task ExecuteGoToPageAsync(int pageNumber)
        {
            try
            {
                if (pageNumber >= 1 && pageNumber <= TotalPages)
                {
                    await LoadDataAsync(pageNumber);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "页面导航失败，目标页码: {PageNumber}", pageNumber);

                // 可以在这里添加用户通知逻辑
            }
        }

        #endregion 命令实现

        #region 私有方法

        /// <summary>
        /// 更新页码显示
        /// </summary>
        private void UpdatePageNumbers()
        {
            PageNumbers.Clear();

            // 计算显示的页码范围
            const int maxVisiblePages = 7;
            var start = Math.Max(1, CurrentPage - (maxVisiblePages / 2));
            var end = Math.Min(TotalPages, start + maxVisiblePages - 1);

            // 调整起始页
            if (end - start < maxVisiblePages - 1)
            {
                start = Math.Max(1, end - maxVisiblePages + 1);
            }

            for (int i = start; i <= end; i++)
            {
                PageNumbers.Add(new PageNumberInfo
                {
                    Number = i,
                    IsCurrent = i == CurrentPage
                });
            }
        }

        #endregion 私有方法

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }

    #region 辅助类

    /// <summary>
    /// 分页数据结果
    /// </summary>
    public class PagedDataResult
    {
        public System.Collections.IEnumerable Items { get; set; } = null!;
        public int TotalCount { get; set; } = 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 页码信息
    /// </summary>
    public class PageNumberInfo
    {
        public int Number { get; set; } = 1;
        public bool IsCurrent { get; set; } = false;
    }

    #endregion 辅助类
}

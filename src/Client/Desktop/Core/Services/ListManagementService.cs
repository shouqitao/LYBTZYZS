using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 列表管理服务接口 - 第2阶段架构重构
    /// 使用组合模式替代BaseListViewModel的继承
    /// </summary>
    public interface IListManagementService<T> where T : class
    {
        /// <summary>
        /// 数据集合
        /// </summary>
        ObservableCollection<T> Items { get; }
        
        /// <summary>
        /// 集合视图（支持排序、过滤、分组）
        /// </summary>
        ICollectionView ItemsView { get; }
        
        /// <summary>
        /// 选中的项
        /// </summary>
        T? SelectedItem { get; set; }
        
        /// <summary>
        /// 选中的多个项
        /// </summary>
        ObservableCollection<T> SelectedItems { get; }
        
        /// <summary>
        /// 总记录数
        /// </summary>
        int TotalCount { get; }
        
        /// <summary>
        /// 是否为空
        /// </summary>
        bool IsEmpty { get; }
        
        /// <summary>
        /// 是否正在加载
        /// </summary>
        bool IsLoading { get; }
        
        /// <summary>
        /// 选中项变化事件
        /// </summary>
        event EventHandler<T?>? SelectedItemChanged;
        
        /// <summary>
        /// 加载数据
        /// </summary>
        Task LoadAsync(Func<CancellationToken, Task<IEnumerable<T>>> loadFunc, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        Task RefreshAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 过滤数据
        /// </summary>
        void Filter(Func<T, bool>? predicate);
        
        /// <summary>
        /// 排序数据
        /// </summary>
        void Sort(string propertyName, ListSortDirection direction);
        
        /// <summary>
        /// 清除排序
        /// </summary>
        void ClearSort();
        
        /// <summary>
        /// 清除过滤
        /// </summary>
        void ClearFilter();
        
        /// <summary>
        /// 添加项
        /// </summary>
        void AddItem(T item);
        
        /// <summary>
        /// 移除项
        /// </summary>
        void RemoveItem(T item);
        
        /// <summary>
        /// 清空列表
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 列表管理服务实现
    /// </summary>
    public class ListManagementService<T> : IPaginatedListManagementService<T>, INotifyPropertyChanged where T : class
    {
        private readonly ILogger<ListManagementService<T>> _logger;
        private readonly ObservableCollection<T> _items;
        private readonly ObservableCollection<T> _selectedItems;
        private readonly ICollectionView _itemsView;
        
        private T? _selectedItem;
        private bool _isLoading = false;
        private int _totalCount = 0;
        private Func<CancellationToken, Task<IEnumerable<T>>>? _loadFunc;
        private Func<T, bool>? _currentFilter;
        
        // 分页相关字段
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalItems = 0;

        public ListManagementService(ILogger<ListManagementService<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _items = new ObservableCollection<T>();
            _selectedItems = new ObservableCollection<T>();
            
            // 创建集合视图
            _itemsView = CollectionViewSource.GetDefaultView(_items);
            _itemsView.CollectionChanged += (s, e) =>
            {
                UpdateTotalCount();
                OnPropertyChanged(nameof(IsEmpty));
            };
        }

        #region 属性实现
        
        public ObservableCollection<T> Items => _items;
        
        public ICollectionView ItemsView => _itemsView;
        
        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                    SelectedItemChanged?.Invoke(this, value);
                }
            }
        }
        
        public ObservableCollection<T> SelectedItems => _selectedItems;
        
        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged(nameof(TotalCount));
                }
            }
        }
        
        public bool IsEmpty => !_items.Any();
        
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        
        
        // 分页属性
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value && value > 0)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }
        
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value && value > 0)
                {
                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }
        
        public int TotalPages => _totalItems > 0 && _pageSize > 0 ? (int)Math.Ceiling((double)_totalItems / _pageSize) : 0;
        
        public bool HasPreviousPage => _currentPage > 1;
        
        public bool HasNextPage => _currentPage < TotalPages;
        
        // 分页相关方法需要的附加属性
        public int TotalItems 
        { 
            get => _totalItems;
            private set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnPropertyChanged(nameof(TotalItems));
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }
        
        public bool IsPaginationEnabled { get; set; } = false;
        
        public string SearchText { get; set; } = string.Empty;
        
        public void SetTotalItems(int totalItems)
        {
            TotalItems = totalItems;
        }
        
        #endregion

        #region 事件
        
        public event EventHandler<T?>? SelectedItemChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        
        #endregion

        #region 数据加载
        
        public async Task LoadAsync(Func<CancellationToken, Task<IEnumerable<T>>> loadFunc, CancellationToken cancellationToken = default)
        {
            _loadFunc = loadFunc ?? throw new ArgumentNullException(nameof(loadFunc));
            
            try
            {
                IsLoading = true;
                _logger.LogDebug("开始加载列表数据");
                
                var data = await loadFunc(cancellationToken);
                
                // 在UI线程更新集合
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    _items.Clear();
                    foreach (var item in data)
                    {
                        _items.Add(item);
                    }
                });
                
                UpdateTotalCount();
                _logger.LogDebug("列表数据加载完成，共 {Count} 项", TotalCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("列表数据加载被取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载列表数据失败");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (_loadFunc != null)
            {
                await LoadAsync(_loadFunc, cancellationToken);
                
                // 重新应用过滤器
                if (_currentFilter != null)
                {
                    Filter(_currentFilter);
                }
            }
        }
        
        #endregion

        #region 分页方法
        
        public async Task LoadPageAsync(int pageNumber, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1 || pageNumber > TotalPages)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"页码必须在1到{TotalPages}之间");
            }
            
            CurrentPage = pageNumber;
            
            // 如果有加载函数，重新加载数据
            if (_loadFunc != null)
            {
                await LoadAsync(_loadFunc, cancellationToken);
            }
        }
        
        public async Task NextPageAsync(CancellationToken cancellationToken = default)
        {
            if (HasNextPage)
            {
                await LoadPageAsync(CurrentPage + 1, cancellationToken);
            }
        }
        
        public async Task PreviousPageAsync(CancellationToken cancellationToken = default)
        {
            if (HasPreviousPage)
            {
                await LoadPageAsync(CurrentPage - 1, cancellationToken);
            }
        }
        
        #endregion

        #region 过滤和排序
        
        public void Filter(Func<T, bool>? predicate)
        {
            _currentFilter = predicate;
            
            if (predicate == null)
            {
                _itemsView.Filter = null;
            }
            else
            {
                _itemsView.Filter = obj => obj is T item && predicate(item);
            }
            
            _itemsView.Refresh();
            UpdateTotalCount();
        }
        
        public void Sort(string propertyName, ListSortDirection direction)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("属性名不能为空", nameof(propertyName));
            }
            
            _itemsView.SortDescriptions.Clear();
            _itemsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            _itemsView.Refresh();
        }
        
        public void ClearSort()
        {
            _itemsView.SortDescriptions.Clear();
            _itemsView.Refresh();
        }
        
        public void ClearFilter()
        {
            Filter(null);
        }
        
        #endregion

        #region 集合操作
        
        public void AddItem(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _items.Add(item);
            });
            
            UpdateTotalCount();
        }
        
        public void RemoveItem(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _items.Remove(item);
                _selectedItems.Remove(item);
                
                if (_selectedItem == item)
                {
                    SelectedItem = default;
                }
            });
            
            UpdateTotalCount();
        }
        
        public void Clear()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _items.Clear();
                _selectedItems.Clear();
                SelectedItem = default;
            });
            
            UpdateTotalCount();
        }
        
        #endregion

        #region 辅助方法
        
        private void UpdateTotalCount()
        {
            TotalCount = _itemsView.Cast<T>().Count();
        }
        
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }

    /// <summary>
    /// 分页列表管理服务扩展
    /// </summary>
    public interface IPaginatedListManagementService<T> : IListManagementService<T>, INotifyPropertyChanged where T : class
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        int CurrentPage { get; set; }
        
        /// <summary>
        /// 每页大小
        /// </summary>
        int PageSize { get; set; }
        
        /// <summary>
        /// 总页数
        /// </summary>
        int TotalPages { get; }
        
        /// <summary>
        /// 是否有上一页
        /// </summary>
        bool HasPreviousPage { get; }
        
        /// <summary>
        /// 是否有下一页
        /// </summary>
        bool HasNextPage { get; }
        
        /// <summary>
        /// 加载指定页
        /// </summary>
        Task LoadPageAsync(int pageNumber, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 下一页
        /// </summary>
        Task NextPageAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 上一页
        /// </summary>
        Task PreviousPageAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 搜索文本
        /// </summary>
        string SearchText { get; set; }
        
        /// <summary>
        /// 是否启用分页
        /// </summary>
        bool IsPaginationEnabled { get; set; }
        
        /// <summary>
        /// 总项目数
        /// </summary>
        int TotalItems { get; }
        
        /// <summary>
        /// 设置总项目数
        /// </summary>
        void SetTotalItems(int totalItems);
    }
}
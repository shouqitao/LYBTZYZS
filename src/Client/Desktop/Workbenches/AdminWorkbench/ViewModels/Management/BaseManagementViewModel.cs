using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Shared;

namespace LYBT.Desktop.Workbench.Admin.ViewModels.Management
{
    /// <summary>
    /// 管理模块基础视图模型
    /// 提供通用的CRUD操作功能
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseManagementViewModel<T> : BindableBase where T : class
    {
        #region Fields

        private ObservableCollection<T> _items;
        private ICollectionView _itemsView;
        private T _selectedItem;
        private string _searchText;
        private bool _isLoading;
        private bool _isRefreshing;
        private int _totalCount;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private string _statusMessage;

        #endregion

        #region Properties

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 数据项视图（支持筛选、排序）
        /// </summary>
        public ICollectionView ItemsView
        {
            get => _itemsView;
            private set => SetProperty(ref _itemsView, value);
        }

        /// <summary>
        /// 当前选中项
        /// </summary>
        public T SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnSelectedItemChanged();
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
                    ApplyFilter();
                }
            }
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
        /// 是否正在刷新
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
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
                    _ = LoadDataAsync();
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
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

        #endregion

        #region Commands

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand ClearSearchCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand<int> GoToPageCommand { get; }

        #endregion

        #region Constructor

        protected BaseManagementViewModel()
        {
            Items = new ObservableCollection<T>();
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            
            // 初始化命令
            LoadCommand = new DelegateCommand(async () => await LoadDataAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());
            AddCommand = new DelegateCommand(async () => await AddItemAsync(), CanAddItem);
            EditCommand = new DelegateCommand(async () => await EditItemAsync(), CanEditItem);
            DeleteCommand = new DelegateCommand(async () => await DeleteItemAsync(), CanDeleteItem);
            ExportCommand = new DelegateCommand(async () => await ExportDataAsync());
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            PreviousPageCommand = new DelegateCommand(GoPreviousPage, () => CanGoPreviousPage);
            NextPageCommand = new DelegateCommand(GoNextPage, () => CanGoNextPage);
            GoToPageCommand = new DelegateCommand<int>(GoToPage);

            // 初始化数据
            _ = LoadDataAsync();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 加载数据
        /// </summary>
        protected abstract Task<(IEnumerable<T> items, int totalCount)> LoadDataInternalAsync();

        /// <summary>
        /// 添加项目
        /// </summary>
        protected abstract Task AddItemInternalAsync();

        /// <summary>
        /// 编辑项目
        /// </summary>
        protected abstract Task EditItemInternalAsync(T item);

        /// <summary>
        /// 删除项目
        /// </summary>
        protected abstract Task DeleteItemInternalAsync(T item);

        /// <summary>
        /// 导出数据
        /// </summary>
        protected abstract Task ExportDataInternalAsync();

        /// <summary>
        /// 筛选条件
        /// </summary>
        protected abstract bool FilterItem(T item);

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 选中项改变时触发
        /// </summary>
        protected virtual void OnSelectedItemChanged()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 是否可以添加
        /// </summary>
        protected virtual bool CanAddItem() => !IsLoading;

        /// <summary>
        /// 是否可以编辑
        /// </summary>
        protected virtual bool CanEditItem() => !IsLoading && SelectedItem != null;

        /// <summary>
        /// 是否可以删除
        /// </summary>
        protected virtual bool CanDeleteItem() => !IsLoading && SelectedItem != null;

        #endregion

        #region Methods

        /// <summary>
        /// 加载数据
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载数据...";

                var (items, totalCount) = await LoadDataInternalAsync();
                
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }

                TotalCount = totalCount;
                StatusMessage = $"共 {TotalCount} 条记录";

                // 刷新分页按钮状态
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载数据失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (IsRefreshing) return;

            try
            {
                IsRefreshing = true;
                StatusMessage = "正在刷新数据...";
                await LoadDataAsync();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        private async Task AddItemAsync()
        {
            try
            {
                await AddItemInternalAsync();
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"添加失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 编辑项目
        /// </summary>
        private async Task EditItemAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                await EditItemInternalAsync(SelectedItem);
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"编辑失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        private async Task DeleteItemAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                await DeleteItemInternalAsync(SelectedItem);
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 导出数据
        /// </summary>
        private async Task ExportDataAsync()
        {
            try
            {
                StatusMessage = "正在导出数据...";
                await ExportDataInternalAsync();
                StatusMessage = "导出成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 应用筛选
        /// </summary>
        private void ApplyFilter()
        {
            if (ItemsView != null)
            {
                ItemsView.Filter = item => item is T typedItem && FilterItem(typedItem);
            }
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void GoPreviousPage()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void GoNextPage()
        {
            if (CanGoNextPage)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        private void GoToPage(int pageNumber)
        {
            if (pageNumber > 0 && pageNumber <= TotalPages)
            {
                CurrentPage = pageNumber;
            }
        }

        #endregion
    }
}
using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Prism.Commands;
using Prism.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// UltraThink Phase 5.1: 增强的服务管理ViewModel基类
    /// 集成工作台功能、权限控制、分页、搜索、CRUD操作
    /// </summary>
    public abstract class EnhancedServiceManagementViewModel<TModel, TService> : ServiceViewModel
        where TModel : class
        where TService : class
    {
        protected readonly TService Service;
        
        private ObservableCollection<TModel> _items = new();
        private ICollectionView _itemsView;
        private TModel? _selectedItem;
        private string _searchText = string.Empty;
        private bool _isRefreshing;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount;
        private int _totalPages;

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<TModel> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 集合视图（支持过滤和排序）
        /// </summary>
        public ICollectionView ItemsView
        {
            get => _itemsView;
            private set => SetProperty(ref _itemsView, value);
        }

        /// <summary>
        /// 选中的项
        /// </summary>
        public TModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnSelectedItemChanged();
                    RefreshCommandStates();
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
                    OnSearchTextChanged();
                }
            }
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
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 页面大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
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
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 分页信息显示
        /// </summary>
        public string PaginationInfo => $"{CurrentPage} / {TotalPages} 页，共 {TotalCount} 条记录";

        /// <summary>
        /// 模块名称
        /// </summary>
        protected abstract string ModuleName { get; }

        #region 命令

        public DelegateCommand AddCommand { get; protected set; }
        public DelegateCommand EditCommand { get; protected set; }
        public DelegateCommand DeleteCommand { get; protected set; }
        public DelegateCommand SearchCommand { get; protected set; }
        public DelegateCommand ClearSearchCommand { get; protected set; }
        public DelegateCommand FirstPageCommand { get; protected set; }
        public DelegateCommand PreviousPageCommand { get; protected set; }
        public DelegateCommand NextPageCommand { get; protected set; }
        public DelegateCommand LastPageCommand { get; protected set; }
        public DelegateCommand<int> GoToPageCommand { get; protected set; }

        #endregion

        public EnhancedServiceManagementViewModel(IEventAggregator eventAggregator,
                                                IErrorHandlingService errorHandlingService,
                                                IUserSessionManager userSessionManager,
                                                IPermissionService permissionService,
                                                TService service)
            : base(eventAggregator, errorHandlingService)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
            
            // 注意：UserSessionManager 和 PermissionService 需要在基类中支持或者在子类中单独处理
            // 这里暂时忽略，因为基类不支持这些参数
            
            InitializeCommands();
            InitializeCollectionView();
        }

        private void InitializeCommands()
        {
            AddCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            EditCommand = new DelegateCommand(ExecuteEdit, CanEdit);
            DeleteCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SearchCommand = new DelegateCommand(ExecuteSearch, CanSearch);
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch, CanClearSearch);
            
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanGoToFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanGoToPreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanGoToNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanGoToLastPage);
            GoToPageCommand = new DelegateCommand<int>(ExecuteGoToPage, CanGoToPage);
        }

        private void InitializeCollectionView()
        {
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItems;
        }

        #region 抽象方法 - 子类需要实现

        /// <summary>
        /// 加载数据
        /// </summary>
        protected abstract Task<PagedResult<TModel>> LoadDataAsync(int page, int pageSize, string searchText);

        /// <summary>
        /// 检查添加权限
        /// </summary>
        protected abstract bool CanAddItem();

        /// <summary>
        /// 检查编辑权限
        /// </summary>
        protected abstract bool CanEditItem(TModel item);

        /// <summary>
        /// 检查删除权限
        /// </summary>
        protected abstract bool CanDeleteItem(TModel item);

        /// <summary>
        /// 添加项
        /// </summary>
        protected abstract Task AddItemAsync();

        /// <summary>
        /// 编辑项
        /// </summary>
        protected abstract Task EditItemAsync(TModel item);

        /// <summary>
        /// 删除项
        /// </summary>
        protected abstract Task DeleteItemAsync(TModel item);

        #endregion

        #region 虚方法 - 子类可以重写

        /// <summary>
        /// 过滤项
        /// </summary>
        protected virtual bool FilterItems(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText) || item is not TModel model)
                return true;

            return FilterItem(model, SearchText);
        }

        /// <summary>
        /// 具体的过滤逻辑
        /// </summary>
        protected virtual bool FilterItem(TModel item, string searchText)
        {
            // 默认不过滤，子类重写实现具体逻辑
            return true;
        }

        /// <summary>
        /// 选中项变化时调用
        /// </summary>
        protected virtual void OnSelectedItemChanged()
        {
            // 子类可以重写
        }

        /// <summary>
        /// 搜索文本变化时调用
        /// </summary>
        protected virtual void OnSearchTextChanged()
        {
            ItemsView.Refresh();
        }

        #endregion

        #region 数据操作

        /// <summary>
        /// 刷新数据
        /// </summary>
        protected override async Task ExecuteRefreshAsync()
        {
            await LoadDataWithPaginationAsync();
        }

        /// <summary>
        /// 带分页的数据加载
        /// </summary>
        protected async Task LoadDataWithPaginationAsync()
        {
            await ExecuteAsync(async () =>
            {
                IsRefreshing = true;
                
                var result = await LoadDataAsync(CurrentPage, PageSize, SearchText);
                
                Items.Clear();
                foreach (var item in result.Items)
                {
                    Items.Add(item);
                }
                
                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                
                RaisePropertyChanged(nameof(PaginationInfo));
                RefreshCommandStates();
                
            }, "加载数据");
            
            IsRefreshing = false;
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        protected void RefreshCommandStates()
        {
            AddCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            SearchCommand.RaiseCanExecuteChanged();
            ClearSearchCommand.RaiseCanExecuteChanged();
            
            FirstPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        protected virtual void ExecuteAdd()
        {
            _ = ExecuteAsync(async () => await AddItemAsync(), "添加");
        }

        protected virtual bool CanAdd()
        {
            return !IsLoading && CanAddItem();
        }

        protected virtual void ExecuteEdit()
        {
            if (SelectedItem != null)
            {
                _ = ExecuteAsync(async () => await EditItemAsync(SelectedItem), "编辑");
            }
        }

        protected virtual bool CanEdit()
        {
            return !IsLoading && SelectedItem != null && CanEditItem(SelectedItem);
        }

        protected virtual void ExecuteDelete()
        {
            if (SelectedItem != null)
            {
                _ = ExecuteAsync(async () => await DeleteItemAsync(SelectedItem), "删除");
            }
        }

        protected virtual bool CanDelete()
        {
            return !IsLoading && SelectedItem != null && CanDeleteItem(SelectedItem);
        }

        protected virtual void ExecuteSearch()
        {
            CurrentPage = 1;
            _ = LoadDataWithPaginationAsync();
        }

        protected virtual bool CanSearch()
        {
            return !IsLoading;
        }

        protected virtual void ExecuteClearSearch()
        {
            SearchText = string.Empty;
            ExecuteSearch();
        }

        protected virtual bool CanClearSearch()
        {
            return !IsLoading && !string.IsNullOrEmpty(SearchText);
        }

        #endregion

        #region 分页命令

        protected virtual void ExecuteFirstPage()
        {
            CurrentPage = 1;
            _ = LoadDataWithPaginationAsync();
        }

        protected virtual bool CanGoToFirstPage()
        {
            return !IsLoading && CurrentPage > 1;
        }

        protected virtual void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                _ = LoadDataWithPaginationAsync();
            }
        }

        protected virtual bool CanGoToPreviousPage()
        {
            return !IsLoading && CurrentPage > 1;
        }

        protected virtual void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                _ = LoadDataWithPaginationAsync();
            }
        }

        protected virtual bool CanGoToNextPage()
        {
            return !IsLoading && CurrentPage < TotalPages;
        }

        protected virtual void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            _ = LoadDataWithPaginationAsync();
        }

        protected virtual bool CanGoToLastPage()
        {
            return !IsLoading && CurrentPage < TotalPages;
        }

        protected virtual void ExecuteGoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                _ = LoadDataWithPaginationAsync();
            }
        }

        protected virtual bool CanGoToPage(int page)
        {
            return !IsLoading && page >= 1 && page <= TotalPages && page != CurrentPage;
        }

        #endregion

        protected override async Task OnInitializeAsync()
        {
            await LoadDataWithPaginationAsync();
        }

    }
}
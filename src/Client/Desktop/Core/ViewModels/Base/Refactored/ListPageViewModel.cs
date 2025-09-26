using System;
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

namespace LYBT.Desktop.Core.ViewModels.Base.Refactored
{
    /// <summary>
    /// 简化的列表页面ViewModel基类 - Phase 1架构重构
    /// 减少依赖复杂度，提供核心列表管理功能
    /// 内置分页、搜索、选择等基础功能
    /// </summary>
    /// <typeparam name="T">列表项数据类型</typeparam>
    public abstract class ListPageViewModel<T> : PageViewModel where T : class
    {
        #region 字段
        
        private readonly ObservableCollection<T> _items;
        private readonly ObservableCollection<T> _selectedItems;
        private ICollectionView? _itemsView;
        
        private string _searchText = string.Empty;
        private T? _selectedItem;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount;
        
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
        /// 列表视图（用于筛选和排序）
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
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems => _selectedItems.Count > 0;
        
        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount => _selectedItems.Count;
        
        /// <summary>
        /// 是否为空列表
        /// </summary>
        public bool IsEmpty => _items.Count == 0;
        
        /// <summary>
        /// 是否非空列表
        /// </summary>
        public bool IsNotEmpty => _items.Count > 0;
        
        #endregion

        #region 搜索属性
        
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
                    // 延迟执行搜索以避免过于频繁的请求
                    _ = Task.Delay(300).ContinueWith(async _ => 
                    {
                        if (_searchText == value) // 确保搜索文本没有再次变化
                        {
                            await SearchAsync();
                        }
                    });
                }
            }
        }
        
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
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1; // 重置到第一页
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
        /// 是否可以上一页
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;
        
        /// <summary>
        /// 是否可以下一页
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        
        /// <summary>
        /// 分页信息文本
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

        #region 命令
        
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
        /// 清空选择命令
        /// </summary>
        public DelegateCommand ClearSelectionCommand { get; private set; }
        
        /// <summary>
        /// 第一页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; }
        
        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; private set; }
        
        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; private set; }
        
        /// <summary>
        /// 最后页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; }
        
        #endregion

        #region 构造函数
        
        protected ListPageViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _items = new ObservableCollection<T>();
            _selectedItems = new ObservableCollection<T>();
            
            InitializeCommands();
            SetupCollectionEvents();
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await ExecuteAddAsync(), CanExecuteAdd);
            DeleteCommand = new DelegateCommand<T>(async item => await ExecuteDeleteAsync(item), CanExecuteDelete);
            BatchDeleteCommand = new DelegateCommand(async () => await ExecuteBatchDeleteAsync(), CanExecuteBatchDelete);
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch, () => !string.IsNullOrEmpty(SearchText));
            ClearSelectionCommand = new DelegateCommand(ExecuteClearSelection, () => HasSelectedItems);
            
            FirstPageCommand = new DelegateCommand(() => CurrentPage = 1, () => CanGoToPreviousPage);
            PreviousPageCommand = new DelegateCommand(() => CurrentPage--, () => CanGoToPreviousPage);
            NextPageCommand = new DelegateCommand(() => CurrentPage++, () => CanGoToNextPage);
            LastPageCommand = new DelegateCommand(() => CurrentPage = TotalPages, () => CanGoToNextPage);
        }
        
        /// <summary>
        /// 设置集合事件
        /// </summary>
        private void SetupCollectionEvents()
        {
            _items.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(IsEmpty));
                RaisePropertyChanged(nameof(IsNotEmpty));
            };
            
            _selectedItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(HasSelectedItems));
                RaisePropertyChanged(nameof(SelectedItemsCount));
                RefreshCanExecuteChanged();
            };
        }
        
        #endregion

        #region 数据加载
        
        /// <summary>
        /// 初始化数据
        /// </summary>
        protected override async Task OnInitializeDataAsync()
        {
            await LoadPageAsync();
        }
        
        /// <summary>
        /// 加载当前页数据
        /// </summary>
        protected virtual async Task LoadPageAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var result = await LoadPagedDataAsync(CurrentPage, PageSize, SearchText);
                
                _items.Clear();
                foreach (var item in result.Items)
                {
                    _items.Add(item);
                }
                
                TotalCount = result.TotalCount;
                
                Logger.LogDebug("已加载第{CurrentPage}页数据，共{ItemCount}项", CurrentPage, result.Items.Count);
            }, "加载数据");
        }
        
        /// <summary>
        /// 搜索数据
        /// </summary>
        protected virtual async Task SearchAsync()
        {
            CurrentPage = 1; // 搜索时重置到第一页
            await LoadPageAsync();
        }
        
        /// <summary>
        /// 子类实现：加载分页数据
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="searchText">搜索关键词</param>
        /// <returns>分页结果</returns>
        protected abstract Task<PagedResult<T>> LoadPagedDataAsync(int page, int pageSize, string? searchText);
        
        #endregion

        #region 命令实现
        
        /// <summary>
        /// 执行添加
        /// </summary>
        protected virtual async Task ExecuteAddAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                await OnExecuteAddAsync();
                await LoadPageAsync(); // 刷新数据
            }, "添加记录");
        }
        
        /// <summary>
        /// 子类重写：添加逻辑
        /// </summary>
        protected abstract Task OnExecuteAddAsync();
        
        /// <summary>
        /// 是否可以添加
        /// </summary>
        protected virtual bool CanExecuteAdd() => !IsLoading;
        
        /// <summary>
        /// 执行删除
        /// </summary>
        protected virtual async Task ExecuteDeleteAsync(T item)
        {
            if (item == null) return;
            
            await ExecuteSafelyAsync(async () =>
            {
                await OnExecuteDeleteAsync(item);
                await LoadPageAsync(); // 刷新数据
            }, "删除记录");
        }
        
        /// <summary>
        /// 子类重写：删除逻辑
        /// </summary>
        protected virtual Task OnExecuteDeleteAsync(T item) => Task.CompletedTask;
        
        /// <summary>
        /// 是否可以删除
        /// </summary>
        protected virtual bool CanExecuteDelete(T item) => item != null && !IsLoading;
        
        /// <summary>
        /// 执行批量删除
        /// </summary>
        protected virtual async Task ExecuteBatchDeleteAsync()
        {
            if (!HasSelectedItems) return;
            
            await ExecuteSafelyAsync(async () =>
            {
                var itemsToDelete = SelectedItems.ToList();
                await OnExecuteBatchDeleteAsync(itemsToDelete);
                
                _selectedItems.Clear();
                await LoadPageAsync(); // 刷新数据
            }, "批量删除");
        }
        
        /// <summary>
        /// 子类重写：批量删除逻辑
        /// </summary>
        protected virtual Task OnExecuteBatchDeleteAsync(List<T> items) => Task.CompletedTask;
        
        /// <summary>
        /// 是否可以批量删除
        /// </summary>
        protected virtual bool CanExecuteBatchDelete() => HasSelectedItems && !IsLoading;
        
        /// <summary>
        /// 清空搜索
        /// </summary>
        private void ExecuteClearSearch()
        {
            SearchText = string.Empty;
        }
        
        /// <summary>
        /// 清空选择
        /// </summary>
        private void ExecuteClearSelection()
        {
            _selectedItems.Clear();
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 刷新分页相关属性
        /// </summary>
        private void RefreshPaginationProperties()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CanGoToPreviousPage));
            RaisePropertyChanged(nameof(CanGoToNextPage));
            RaisePropertyChanged(nameof(PaginationInfo));
        }
        
        /// <summary>
        /// 刷新命令可执行状态
        /// </summary>
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            
            AddCommand?.RaiseCanExecuteChanged();
            BatchDeleteCommand?.RaiseCanExecuteChanged();
            ClearSearchCommand?.RaiseCanExecuteChanged();
            ClearSelectionCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// 选中指定项
        /// </summary>
        public void SelectItem(T item)
        {
            if (item != null && !_selectedItems.Contains(item))
            {
                _selectedItems.Add(item);
            }
        }
        
        /// <summary>
        /// 取消选中指定项
        /// </summary>
        public void UnselectItem(T item)
        {
            _selectedItems.Remove(item);
        }
        
        /// <summary>
        /// 切换选中状态
        /// </summary>
        public void ToggleSelection(T item)
        {
            if (_selectedItems.Contains(item))
            {
                UnselectItem(item);
            }
            else
            {
                SelectItem(item);
            }
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
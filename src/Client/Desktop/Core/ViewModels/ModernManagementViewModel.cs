using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels
{
    /// <summary>
    /// UltraThink Phase 3.1: 现代化管理界面ViewModel基类
    /// 
    /// 专门针对列表管理场景优化:
    /// 1. 标准CRUD Command集合
    /// 2. 分页和搜索支持
    /// 3. 选择项管理
    /// 4. 零DelegateCommand警告
    /// </summary>
    public abstract class ModernManagementViewModel<T> : ModernViewModelBase
        where T : class
    {
        #region 数据集合属性

        private ObservableCollection<T> _items = new();
        private T? _selectedItem;
        private string _searchKeyword = string.Empty;
        private int _totalCount = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnSelectedItemChanged(value);
                    RaiseCanExecuteChanged();
                }
            }
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
            set => SetProperty(ref _currentPage, value);
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
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItem => SelectedItem != null;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        #endregion

        #region 管理Command集合 (零警告)

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 添加命令
        /// </summary>
        public DelegateCommand AddCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 删除命令
        /// </summary>
        public DelegateCommand DeleteCommand { get; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand ViewDetailsCommand { get; }

        /// <summary>
        /// 导出命令
        /// </summary>
        public DelegateCommand ExportCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 标准构造函数
        /// </summary>
        protected ModernManagementViewModel(
            IEventAggregator eventAggregator, 
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            // 零警告Command初始化
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
            AddCommand = new DelegateCommand(async () => await ExecuteAddAsync(), CanExecuteAdd);
            EditCommand = new DelegateCommand(async () => await ExecuteEditAsync(), CanExecuteEdit);
            DeleteCommand = new DelegateCommand(async () => await ExecuteDeleteAsync(), CanExecuteDelete);
            ViewDetailsCommand = new DelegateCommand(async () => await ExecuteViewDetailsAsync(), CanExecuteViewDetails);
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync(), CanExecuteExport);
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        protected ModernManagementViewModel(IEventAggregator eventAggregator)
            : this(eventAggregator, null)
        {
        }

        /// <summary>
        /// 简化构造函数
        /// </summary>
        protected ModernManagementViewModel()
            : this(new EventAggregator(), null)
        {
        }

        #endregion

        #region 虚方法（子类实现具体业务逻辑）

        /// <summary>
        /// 加载数据 - 子类必须实现
        /// </summary>
        protected abstract Task<ServiceResult<PagedResult<T>>> LoadDataAsync(int page, int pageSize, string? keyword = null);

        /// <summary>
        /// 添加项 - 子类可重写
        /// </summary>
        protected virtual Task OnAddAsync() => Task.CompletedTask;

        /// <summary>
        /// 编辑项 - 子类可重写
        /// </summary>
        protected virtual Task OnEditAsync(T item) => Task.CompletedTask;

        /// <summary>
        /// 删除项 - 子类可重写
        /// </summary>
        protected virtual Task OnDeleteAsync(T item) => Task.CompletedTask;

        /// <summary>
        /// 查看详情 - 子类可重写
        /// </summary>
        protected virtual Task OnViewDetailsAsync(T item) => Task.CompletedTask;

        /// <summary>
        /// 导出数据 - 子类可重写
        /// </summary>
        protected virtual Task OnExportAsync() => Task.CompletedTask;

        /// <summary>
        /// 选中项变化 - 子类可重写
        /// </summary>
        protected virtual void OnSelectedItemChanged(T? item)
        {
            // 默认实现：无操作
        }

        #endregion

        #region Command CanExecute方法（子类可重写）

        protected virtual bool CanExecuteSearch() => !IsLoading;
        protected virtual bool CanExecuteAdd() => !IsLoading;
        protected virtual bool CanExecuteEdit() => !IsLoading && HasSelectedItem;
        protected virtual bool CanExecuteDelete() => !IsLoading && HasSelectedItem;
        protected virtual bool CanExecuteViewDetails() => !IsLoading && HasSelectedItem;
        protected virtual bool CanExecuteExport() => !IsLoading && Items.Any();
        protected virtual bool CanExecutePreviousPage() => !IsLoading && CurrentPage > 1;
        protected virtual bool CanExecuteNextPage() => !IsLoading && CurrentPage < TotalPages;

        #endregion

        #region Command执行方法

        private async Task ExecuteSearchAsync()
        {
            CurrentPage = 1; // 搜索时重置到第一页
            await LoadDataWithHandlingAsync("搜索");
        }

        private async Task ExecuteAddAsync()
        {
            try
            {
                await OnAddAsync();
                await LoadDataWithHandlingAsync("刷新数据");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("添加", ex);
            }
        }

        private async Task ExecuteEditAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                await OnEditAsync(SelectedItem);
                await LoadDataWithHandlingAsync("刷新数据");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("编辑", ex);
            }
        }

        private async Task ExecuteDeleteAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                await OnDeleteAsync(SelectedItem);
                await LoadDataWithHandlingAsync("刷新数据");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("删除", ex);
            }
        }

        private async Task ExecuteViewDetailsAsync()
        {
            if (SelectedItem == null) return;

            try
            {
                await OnViewDetailsAsync(SelectedItem);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("查看详情", ex);
            }
        }

        private async Task ExecuteExportAsync()
        {
            try
            {
                await OnExportAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("导出", ex);
            }
        }

        private async Task ExecutePreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataWithHandlingAsync("加载上一页");
            }
        }

        private async Task ExecuteNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataWithHandlingAsync("加载下一页");
            }
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 重写刷新逻辑
        /// </summary>
        protected override async Task OnRefreshAsync()
        {
            await LoadDataWithHandlingAsync("刷新数据");
        }

        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            
            SearchCommand.RaiseCanExecuteChanged();
            AddCommand.RaiseCanExecuteChanged();
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            ViewDetailsCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 带错误处理的数据加载
        /// </summary>
        private async Task LoadDataWithHandlingAsync(string operationName)
        {
            var serviceResult = await ExecuteAsync(
                async () => await LoadDataAsync(CurrentPage, PageSize, SearchKeyword),
                operationName);

            if (serviceResult?.IsSuccess == true && serviceResult.Data != null)
            {
                var pagedResult = serviceResult.Data;
                Items = new ObservableCollection<T>(pagedResult.Items ?? Enumerable.Empty<T>());
                TotalCount = pagedResult.TotalCount;
                
                // 确保选中项仍然有效
                if (SelectedItem != null && !Items.Contains(SelectedItem))
                {
                    SelectedItem = null;
                }

                SetStatus($"加载完成，共 {TotalCount} 条记录");
            }
        }

        #endregion
    }
}
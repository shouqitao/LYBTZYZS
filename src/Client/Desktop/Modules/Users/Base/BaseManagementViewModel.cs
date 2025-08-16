using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Users.Base
{
    /// <summary>
    /// 管理视图模型基类 - 提供标准的CRUD功能和分页支持
    /// </summary>
    /// <typeparam name="TModel">实体模型类型</typeparam>
    /// <typeparam name="TService">服务接口类型</typeparam>
    public abstract class BaseManagementViewModel<TModel, TService> : BindableBase
        where TModel : class
        where TService : class
    {
        protected readonly TService Service;

        #region 属性

        private string _searchKeyword = string.Empty;
        private TModel? _selectedItem;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public ObservableCollection<TModel> Items { get; }
        public ICollectionView ItemsView { get; }

        /// <summary>模块名称（用于显示）</summary>
        protected abstract string ModuleName { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的项</summary>
        public TModel? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePaginationStatus();
                }
            }
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdatePaginationStatus();
                }
            }
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>状态文本</summary>
        public virtual string StatusText => $"第 {CurrentPage} 页，共 {TotalPages} 页，总计 {TotalCount} 条记录";

        /// <summary>是否可以跳转到第一页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>是否可以跳转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>是否可以跳转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>是否可以跳转到最后一页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<TModel> EditCommand { get; }
        public DelegateCommand<TModel> ViewCommand { get; }
        public DelegateCommand<TModel> DeleteCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        #endregion

        protected BaseManagementViewModel(TService service)
        {
            Service = service;

            Items = new ObservableCollection<TModel>();
            ItemsView = CollectionViewSource.GetDefaultView(Items);

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await LoadDataAsync());
            AddCommand = new DelegateCommand(ExecuteAdd);
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand<TModel>(ExecuteEdit, CanExecuteEdit);
            ViewCommand = new DelegateCommand<TModel>(ExecuteView, CanExecuteView);
            DeleteCommand = new DelegateCommand<TModel>(async (item) => await ExecuteDeleteAsync(item), CanExecuteDelete);

            FirstPageCommand = new DelegateCommand(async () => { CurrentPage = 1; await LoadDataAsync(); }, () => CanGoFirstPage);
            PreviousPageCommand = new DelegateCommand(async () => { CurrentPage--; await LoadDataAsync(); }, () => CanGoPreviousPage);
            NextPageCommand = new DelegateCommand(async () => { CurrentPage++; await LoadDataAsync(); }, () => CanGoNextPage);
            LastPageCommand = new DelegateCommand(async () => { CurrentPage = TotalPages; await LoadDataAsync(); }, () => CanGoLastPage);

            // 初始化扩展
            OnInitialize();

            // 加载初始数据
            _ = LoadDataAsync();
        }

        #region 抽象方法 - 子类必须实现

        /// <summary>
        /// 加载数据的具体实现
        /// </summary>
        protected abstract Task<ServiceResult<PagedResult<TModel>>> LoadDataFromServiceAsync(PagedQueryBaseDto request);

        /// <summary>
        /// 删除数据的具体实现
        /// </summary>
        protected abstract Task<ServiceResult<bool>> DeleteFromServiceAsync(TModel item);

        /// <summary>
        /// 获取项目的显示名称（用于删除确认等）
        /// </summary>
        protected abstract string GetItemDisplayName(TModel item);

        #endregion

        #region 虚方法 - 子类可选重写

        /// <summary>
        /// 初始化扩展（子类可重写以添加额外初始化）
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 执行新增操作
        /// </summary>
        protected virtual void ExecuteAdd()
        {
            MessageBox.Show($"新增{ModuleName}功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 执行编辑操作
        /// </summary>
        protected virtual void ExecuteEdit(TModel item)
        {
            if (item == null) return;
            MessageBox.Show($"编辑{ModuleName}功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 执行查看操作
        /// </summary>
        protected virtual void ExecuteView(TModel item)
        {
            if (item == null) return;
            MessageBox.Show($"{ModuleName}详情查看功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 是否可以执行编辑
        /// </summary>
        protected virtual bool CanExecuteEdit(TModel item) => item != null;

        /// <summary>
        /// 是否可以执行查看
        /// </summary>
        protected virtual bool CanExecuteView(TModel item) => item != null;

        /// <summary>
        /// 是否可以执行删除
        /// </summary>
        protected virtual bool CanExecuteDelete(TModel item) => item != null;

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                Items.Clear();

                var request = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize
                };

                var result = await LoadDataFromServiceAsync(request);
                if (result.IsSuccess && result.Data != null)
                {
                    TotalCount = result.Data.TotalCount;
                    foreach (var item in result.Data.Items)
                    {
                        Items.Add(item);
                    }
                }
                else
                {
                    MessageBox.Show($"加载{ModuleName}列表失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载{ModuleName}列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        private async Task ExecuteDeleteAsync(TModel item)
        {
            if (item == null) return;

            var itemName = GetItemDisplayName(item);
            var confirmResult = MessageBox.Show($"确定要删除{ModuleName} {itemName} 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var result = await DeleteFromServiceAsync(item);
                if (result.IsSuccess)
                {
                    await LoadDataAsync();
                    MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"删除失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更新分页状态
        /// </summary>
        private void UpdatePaginationStatus()
        {
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(CanGoFirstPage));
            RaisePropertyChanged(nameof(CanGoPreviousPage));
            RaisePropertyChanged(nameof(CanGoNextPage));
            RaisePropertyChanged(nameof(CanGoLastPage));

            FirstPageCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
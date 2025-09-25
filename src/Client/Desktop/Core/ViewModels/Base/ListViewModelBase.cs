using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 列表视图模型基类 - UltraThink架构重构版本
    /// 去除ServiceLocator反模式，使用依赖注入和组合模式
    /// </summary>
    /// <typeparam name="T">列表项数据类型</typeparam>
    public abstract class ListViewModelBase<T> : NavigationViewModelBase where T : class
    {
        private readonly IPaginatedListManagementService<T> _listManager;
        private string _pageTitle = "列表页面";
        private object? _filterContent;
        private object? _listContent;

        #region 构造函数

        /// <summary>
        /// 标准构造函数 - 使用依赖注入，无ServiceLocator
        /// </summary>
        protected ListViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IErrorHandlingService errorHandlingService,
            IPaginatedListManagementService<T>? listManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, errorHandlingService)
        {
            // 使用提供的或创建新的列表管理服务
            _listManager = listManager ?? new ListManagementService<T>(loggerFactory.CreateLogger<ListManagementService<T>>());
            
            // 初始化命令
            InitializeCommands();
            
            // 订阅搜索文本变化
            _listManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IPaginatedListManagementService<T>.SearchText))
                {
                    _ = SearchAsync();
                }
            };
        }

        #endregion

        #region 属性

        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _listManager.SearchText;
            set
            {
                _listManager.SearchText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => !_listManager.Items.Any();

        /// <summary>
        /// 是否显示分页
        /// </summary>
        public bool ShowPagination
        {
            get => _listManager.IsPaginationEnabled;
            set
            {
                _listManager.IsPaginationEnabled = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPage
        {
            get => _listManager.CurrentPage;
            set
            {
                _listManager.CurrentPage = value;
                RaisePropertyChanged();
                _ = LoadDataAsync();
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _listManager.PageSize;
            set
            {
                _listManager.PageSize = value;
                RaisePropertyChanged();
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _listManager.TotalItems;
            set
            {
                _listManager.SetTotalItems(value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalPages));
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => (_listManager.TotalCount + _listManager.PageSize - 1) / _listManager.PageSize;

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems => _listManager.SelectedItems.Any();

        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount => _listManager.SelectedItems.Count;

        /// <summary>
        /// 筛选内容
        /// </summary>
        public object? FilterContent
        {
            get => _filterContent;
            set => SetProperty(ref _filterContent, value);
        }

        /// <summary>
        /// 列表内容
        /// </summary>
        public object? ListContent
        {
            get => _listContent;
            set => SetProperty(ref _listContent, value);
        }

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items => _listManager.Items;

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<T> SelectedItems => _listManager.SelectedItems;

        /// <summary>
        /// 选中项
        /// </summary>
        public T? SelectedItem
        {
            get => _listManager.SelectedItem;
            set
            {
                _listManager.SelectedItem = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 列表视图
        /// </summary>
        public ICollectionView ItemsView => _listManager.ItemsView;

        #endregion

        #region 命令

        public ICommand AddCommand { get; private set; } = null!;
        public new ICommand RefreshCommand { get; private set; } = null!;
        public ICommand BatchDisableCommand { get; private set; } = null!;
        public ICommand FirstPageCommand { get; private set; } = null!;
        public ICommand PreviousPageCommand { get; private set; } = null!;
        public ICommand NextPageCommand { get; private set; } = null!;
        public ICommand LastPageCommand { get; private set; } = null!;
        public ICommand ClearFilterCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;

        #endregion

        #region 方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await ExecuteAddAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            BatchDisableCommand = new DelegateCommand(
                async () => await ExecuteBatchDisableAsync(), 
                CanExecuteBatchDisable)
                .ObservesProperty(() => HasSelectedItems);

            FirstPageCommand = new DelegateCommand(
                () => CurrentPage = 1, 
                () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
                
            PreviousPageCommand = new DelegateCommand(
                () => CurrentPage--, 
                () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
                
            NextPageCommand = new DelegateCommand(
                () => CurrentPage++, 
                () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);
                
            LastPageCommand = new DelegateCommand(
                () => CurrentPage = TotalPages, 
                () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);

            ClearFilterCommand = new DelegateCommand(() =>
            {
                _listManager.ClearFilter();
                SearchText = string.Empty;
            });

            ClearSelectionCommand = new DelegateCommand(() => _listManager.SelectedItems.Clear());

            // 监听选中项变化
            _listManager.SelectedItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(HasSelectedItems));
                RaisePropertyChanged(nameof(SelectedItemsCount));
            };
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public virtual async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                _listManager.Items.Clear();

                var data = await GetDataAsync();
                foreach (var item in data)
                {
                    _listManager.Items.Add(item);
                }

                RaisePropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载数据失败");
                var context = new ErrorContext { Operation = "加载数据", Module = GetType().Name };
                await ErrorHandlingService.HandleExceptionAsync(ex, context);
            }
            finally
            {
                IsLoading = false;
            }
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
        /// 获取数据（由子类实现）
        /// </summary>
        protected abstract Task<IEnumerable<T>> GetDataAsync();

        /// <summary>
        /// 执行新增（由子类实现）
        /// </summary>
        protected abstract Task ExecuteAddAsync();

        /// <summary>
        /// 执行批量禁用
        /// </summary>
        protected virtual async Task ExecuteBatchDisableAsync()
        {
            try
            {
                await PerformBatchDisableAsync(SelectedItems.ToList());
                _listManager.SelectedItems.Clear();
                await LoadDataAsync();
                // ShowStatusMessage removed - should be handled by EventAggregator
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量禁用失败");
                var context = new ErrorContext { Operation = "批量禁用", Module = GetType().Name };
                await ErrorHandlingService.HandleExceptionAsync(ex, context);
            }
        }

        /// <summary>
        /// 执行批量禁用操作（由子类实现）
        /// </summary>
        protected virtual Task PerformBatchDisableAsync(IList<T> items)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 是否可以执行批量禁用
        /// </summary>
        protected virtual bool CanExecuteBatchDisable()
        {
            return HasSelectedItems;
        }

        /// <summary>
        /// 导航到时
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            
            // 自动加载数据
            _ = LoadDataAsync();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        protected override void OnDisposing()
        {
            // Clear collections but don't dispose the service - it's managed by DI container
            _listManager?.SelectedItems?.Clear();
            _listManager?.Items?.Clear();
            
            base.OnDisposing();
        }

        #endregion
    }
}
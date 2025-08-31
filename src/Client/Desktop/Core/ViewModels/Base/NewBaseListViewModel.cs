using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
// UltraThink v2.0: 添加SessionAware相关依赖
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 基础列表ViewModel - 专注于列表数据管理
    /// UltraThink v2.0: 重构为SessionAware架构，集成统一会话管理
    /// 单一职责原则，只负责列表展示和基本操作，替代臃肿的BaseServiceManagementViewModel
    /// </summary>
    /// <typeparam name="TItem">列表项类型</typeparam>
    [Obsolete("使用 ModernManagementViewModel<T> 替代。此类将在架构统一完成后删除。")]
    public abstract class NewBaseListViewModel<TItem> : SessionAwareViewModel
        where TItem : class
    {
        #region Fields

        protected readonly IPaginationCoordinator PaginationCoordinator;
        protected readonly ISearchManager SearchManager;

        private ObservableCollection<TItem> _items = new();
        private TItem? _selectedItem;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// 数据集合
        /// </summary>
        public ObservableCollection<TItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public TItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            protected set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            protected set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 是否有数据
        /// </summary>
        public bool HasData => Items.Count > 0;

        /// <summary>
        /// 分页协调器
        /// </summary>
        public IPaginationCoordinator Pagination => PaginationCoordinator;

        /// <summary>
        /// 搜索管理器
        /// </summary>
        public ISearchManager Search => SearchManager;

        #endregion

        #region Commands

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; } = null!;

        /// <summary>
        /// 清除错误命令
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; private set; } = null!;

        /// <summary>
        /// 选择项命令
        /// </summary>
        public DelegateCommand<TItem> SelectItemCommand { get; private set; } = null!;

        #endregion

        #region Constructor

        protected NewBaseListViewModel(
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger)
        {
            PaginationCoordinator = paginationCoordinator ?? new PaginationCoordinator();
            SearchManager = searchManager ?? new SearchManager();

            InitializeCommands();
            InitializeEventHandlers();
            
            LogInfo("NewBaseListViewModel 已初始化，使用 UltraThink SessionManager 架构");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化命令
        /// </summary>
        protected virtual void InitializeCommands()
        {
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            ClearErrorCommand = new DelegateCommand(() => ErrorMessage = string.Empty);
            SelectItemCommand = new DelegateCommand<TItem>(OnItemSelected);
        }

        /// <summary>
        /// 执行刷新命令 - 修复async void问题
        /// </summary>
        private async void ExecuteRefresh()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 RefreshCommand 被点击 - ExecuteRefresh执行");
                await RefreshDataAsync();
                System.Diagnostics.Debug.WriteLine("✅ RefreshCommand 执行完成");
            }
            catch (Exception ex)
            {
                LogError(ex, "刷新数据失败");
                ShowError($"刷新数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ RefreshCommand 执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化事件处理器
        /// </summary>
        protected virtual void InitializeEventHandlers()
        {
            // 监听分页变化
            PaginationCoordinator.PageChanged += OnPageChanged;

            // 监听搜索事件
            SearchManager.SearchExecuted += OnSearchExecuted;
            SearchManager.SearchCleared += OnSearchCleared;
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// 加载数据 - 子类必须实现
        /// </summary>
        protected abstract Task<ServiceResult<PagedResult<TItem>>> LoadDataAsync(PagedQueryBaseDto request);

        /// <summary>
        /// 刷新数据
        /// </summary>
        public virtual async Task RefreshDataAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔄 RefreshDataAsync 被调用");
            await LoadItemsAsync();
        }

        /// <summary>
        /// 加载列表项
        /// </summary>
        protected virtual async Task LoadItemsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var request = new PagedQueryBaseDto
                {
                    CurrentPage = PaginationCoordinator.CurrentPage,
                    PageSize = PaginationCoordinator.PageSize,
                    SearchKeyword = SearchManager.SearchKeyword
                };

                var result = await LoadDataAsync(request);

                if (result.IsSuccess && result.Data != null)
                {
                    // 🎯 UltraThink UI线程修复: 确保ObservableCollection更新在UI线程上执行
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        Items = new ObservableCollection<TItem>(result.Data.Items);
                        // 强制触发UI更新通知
                        RaisePropertyChanged(nameof(Items));
                    });
                    
                    PaginationCoordinator.UpdatePagination(result.Data.TotalCount);
                    OnDataLoaded(result.Data);
                }
                else
                {
                    // 🎯 UltraThink UI线程修复: 确保UI更新在UI线程上执行
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        Items.Clear();
                        ErrorMessage = result.ErrorMessage ?? "加载数据失败";
                    });
                    
                    PaginationCoordinator.Reset();
                    OnDataLoadFailed(result.ErrorMessage ?? "加载数据失败");
                }

                RaisePropertyChanged(nameof(HasData));
            }
            catch (Exception ex)
            {
                LogError(ex, "加载数据时发生错误");
                
                // 🎯 UltraThink UI线程修复: 确保UI更新在UI线程上执行
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    ErrorMessage = $"加载数据时发生错误：{ex.Message}";
                    Items.Clear();
                });
                
                // UltraThink SessionAware: 使用通知服务显示错误
                ShowError($"加载数据失败：{ex.Message}");
                OnDataLoadFailed(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 分页变化处理
        /// </summary>
        protected virtual async void OnPageChanged(object? sender, PageChangedEventArgs e)
        {
            // 使用适当的async void事件处理器模式
            try
            {
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "分页变化处理失败");
            }
        }

        /// <summary>
        /// 搜索执行处理
        /// </summary>
        protected virtual async void OnSearchExecuted(object? sender, SearchExecutedEventArgs e)
        {
            // 使用适当的async void事件处理器模式
            try
            {
                // 搜索时重置到第一页
                PaginationCoordinator.CurrentPage = 1;
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "搜索执行失败");
            }
        }

        /// <summary>
        /// 搜索清除处理
        /// </summary>
        protected virtual async void OnSearchCleared(object? sender, EventArgs e)
        {
            // 使用适当的async void事件处理器模式
            try
            {
                PaginationCoordinator.CurrentPage = 1;
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                LogError(ex, "搜索清除失败");
            }
        }

        /// <summary>
        /// 项目选择处理
        /// </summary>
        protected virtual void OnItemSelected(TItem item)
        {
            SelectedItem = item;
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 数据加载成功后的处理 - 子类可重写
        /// </summary>
        protected virtual void OnDataLoaded(PagedResult<TItem> data)
        {
            // 子类可重写以执行特定逻辑
        }

        /// <summary>
        /// 数据加载失败后的处理 - 子类可重写
        /// </summary>
        protected virtual void OnDataLoadFailed(string errorMessage)
        {
            // 子类可重写以执行特定逻辑
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        protected virtual void Cleanup()
        {
            PaginationCoordinator.PageChanged -= OnPageChanged;
            SearchManager.SearchExecuted -= OnSearchExecuted;
            SearchManager.SearchCleared -= OnSearchCleared;

            if (SearchManager is IDisposable disposableSearchManager)
            {
                disposableSearchManager.Dispose();
            }
        }

        #endregion
    }
}
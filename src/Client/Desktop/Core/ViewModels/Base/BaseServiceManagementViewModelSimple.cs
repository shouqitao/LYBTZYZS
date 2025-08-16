using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 简化的服务管理基类视图模型
    /// 用于Shared项目中的管理视图模型
    /// </summary>
    /// <typeparam name="TModel">数据模型类型</typeparam>
    public abstract class BaseServiceManagementViewModel<TModel> : BindableBase
        where TModel : class, new()
    {
        protected readonly ILogger Logger;

        #region Properties

        private ObservableCollection<TModel> _items = new();
        /// <summary>
        /// 数据集合
        /// </summary>
        public ObservableCollection<TModel> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private string _title = string.Empty;
        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _errorMessage = string.Empty;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private int _currentPage = 1;
        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;
        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private int _totalPages;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 是否可以转到上一页
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以转到下一页
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        #endregion

        #region Commands

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; protected set; }

        /// <summary>
        /// 清除错误命令
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; protected set; }

        /// <summary>
        /// 清除命令
        /// </summary>
        public DelegateCommand ClearCommand { get; protected set; }

        /// <summary>
        /// 第一页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; protected set; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; protected set; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; protected set; }

        /// <summary>
        /// 最后一页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; protected set; }

        #endregion

        #region Constructor

        protected BaseServiceManagementViewModel(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeCommands();
        }

        #endregion

        #region Methods

        /// <summary>
        /// 初始化命令
        /// </summary>
        protected virtual void InitializeCommands()
        {
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            ClearErrorCommand = new DelegateCommand(() => ErrorMessage = string.Empty);
            ClearCommand = new DelegateCommand(() =>
            {
                Items.Clear();
                ErrorMessage = string.Empty;
            });

            // 分页命令
            FirstPageCommand = new DelegateCommand(
                async () => { CurrentPage = 1; await LoadDataAsync(); },
                () => CanGoToPreviousPage
            ).ObservesProperty(() => CurrentPage);

            PreviousPageCommand = new DelegateCommand(
                async () => { CurrentPage--; await LoadDataAsync(); },
                () => CanGoToPreviousPage
            ).ObservesProperty(() => CurrentPage);

            NextPageCommand = new DelegateCommand(
                async () => { CurrentPage++; await LoadDataAsync(); },
                () => CanGoToNextPage
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);

            LastPageCommand = new DelegateCommand(
                async () => { CurrentPage = TotalPages; await LoadDataAsync(); },
                () => CanGoToNextPage
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);
        }

        /// <summary>
        /// 加载数据（子类必须实现）
        /// </summary>
        protected abstract Task LoadDataAsync();

        /// <summary>
        /// 执行带加载状态的异步操作
        /// </summary>
        protected async Task ExecuteWithLoadingAsync(Func<Task> action)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                await action();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行操作时发生错误");
                ErrorMessage = $"操作失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}
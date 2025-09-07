using System.Collections.ObjectModel;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Base
{

    /// <summary>
    /// 系统管理模块基础视图模型（使用服务层）
    /// 统一的基类，所有管理模块都应该继承此类
    /// </summary>
    /// <typeparam name="TModel">数据模型类型</typeparam>
    /// <typeparam name="TService">服务接口类型</typeparam>
    public abstract class BaseServiceManagementViewModel<TModel, TService> : NavigationViewModelBase
        where TModel : class, new()
        where TService : class
    {
        protected readonly TService Service;

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

        private TModel? _selectedItem;

        /// <summary>
        /// 选中项
        /// </summary>
        public TModel? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private string _searchKeyword = string.Empty;

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private bool _isLoading;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private int _currentPage = 1;

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;

        /// <summary>
        /// 每页大小
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
        /// 模块名称
        /// </summary>
        protected abstract string ModuleName { get; }

        #endregion Properties

        #region Commands

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; set; } = null!;

        /// <summary>
        /// 刷新命令
        /// </summary>
        public new DelegateCommand RefreshCommand { get; set; } = null!;

        /// <summary>
        /// 添加命令
        /// </summary>
        public DelegateCommand AddCommand { get; set; } = null!;

        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand<TModel> EditCommand { get; set; } = null!;

        /// <summary>
        /// 删除命令
        /// </summary>
        public DelegateCommand<TModel> DeleteCommand { get; set; } = null!;

        /// <summary>
        /// 第一页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; set; } = null!;

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; set; } = null!;

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; set; } = null!;

        /// <summary>
        /// 最后一页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; set; } = null!;

        #endregion Commands

        #region Constructor

        protected BaseServiceManagementViewModel(TService service, IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            AddCommand = new DelegateCommand(async () => await AddAsync());
            EditCommand = new DelegateCommand<TModel>(async item => await EditAsync(item));
            DeleteCommand = new DelegateCommand<TModel>(async item => await DeleteAsync(item));

            // 分页命令
            FirstPageCommand = new DelegateCommand(
                async () => await GoToPageAsync(1),
                () => CurrentPage > 1
            ).ObservesProperty(() => CurrentPage);

            PreviousPageCommand = new DelegateCommand(
                async () => await GoToPageAsync(CurrentPage - 1),
                () => CurrentPage > 1
            ).ObservesProperty(() => CurrentPage);

            NextPageCommand = new DelegateCommand(
                async () => await GoToPageAsync(CurrentPage + 1),
                () => CurrentPage < TotalPages
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);

            LastPageCommand = new DelegateCommand(
                async () => await GoToPageAsync(TotalPages),
                () => CurrentPage < TotalPages
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);
        }

        #endregion Constructor

        #region Navigation

        public override void OnNavigatedTo(Dictionary<string, object>? parameters = null)
        {
            base.OnNavigatedTo(parameters);
            _ = LoadDataAsync();
        }

        #endregion Navigation

        #region Methods

        /// <summary>
        /// 加载数据
        /// </summary>
        protected virtual async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var request = new PagedQueryBaseDto
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize,
                    SearchKeyword = SearchKeyword
                };

                var result = await LoadDataFromServiceAsync(request);

                if (result.IsSuccess && result.Data != null)
                {
                    Items = new ObservableCollection<TModel>(result.Data.Items);
                    TotalCount = result.Data.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                }
                else
                {
                    Items.Clear();
                    TotalCount = 0;
                    TotalPages = 0;
                    ShowError(result.ErrorMessage ?? "加载数据失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载数据时发生错误：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从服务加载数据（由子类实现）
        /// </summary>
        protected abstract Task<ServiceResult<LYBT.Shared.Models.Contracts.Common.PagedResult<TModel>>> LoadDataFromServiceAsync(PagedQueryBaseDto request);

        /// <summary>
        /// 搜索
        /// </summary>
        protected virtual async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        /// <summary>
        /// 刷新
        /// </summary>
        protected virtual async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 添加（由子类实现）
        /// </summary>
        protected abstract Task AddAsync();

        /// <summary>
        /// 编辑（由子类实现）
        /// </summary>
        protected abstract Task EditAsync(TModel item);

        /// <summary>
        /// 删除（由子类实现）
        /// </summary>
        protected abstract Task DeleteAsync(TModel item);

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        protected virtual async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages)
            {
                return;
            }

            CurrentPage = page;
            await LoadDataAsync();
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected virtual async Task ShowSuccessAsync(string message)
        {
            StatusMessage = message;
            if (ErrorHandlingService?.CustomDialogService != null)
            {
                try
                {
                    await ErrorHandlingService.CustomDialogService.ShowInformationAsync(ModuleName, message);
                }
                catch
                {
                    // 静默处理，已经设置StatusMessage
                }
            }
        }

        /// <summary>
        /// 显示成功消息（同步版本，兼容性）
        /// </summary>
        protected virtual void ShowSuccess(string message)
        {
            _ = ShowSuccessAsync(message);
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual async Task ShowErrorAsync(string message)
        {
            await this.HandleErrorAsync(ModuleName, new InvalidOperationException(message));
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual void ShowError(string message)
        {
            _ = ShowErrorAsync(message);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowConfirmAsync(string message)
        {
            if (ErrorHandlingService?.CustomDialogService != null)
            {
                try
                {
                    return await ErrorHandlingService.CustomDialogService.ShowConfirmationAsync(message, ModuleName);
                }
                catch
                {
                    // 对话框服务失败时默认返回false（取消操作）
                    return false;
                }
            }
            // 如果没有对话框服务，默认返回false（保守操作）
            return false;
        }

        /// <summary>
        /// 显示确认对话框（同步版本，兼容性）
        /// </summary>
        protected virtual bool ShowConfirm(string message)
        {
            try
            {
                return ShowConfirmAsync(message).GetAwaiter().GetResult();
            }
            catch
            {
                // 同步调用失败时默认返回false（保守操作）
                return false;
            }
        }

        #endregion Methods
    }
}

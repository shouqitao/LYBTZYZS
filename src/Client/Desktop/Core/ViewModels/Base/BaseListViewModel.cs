using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Base {

    /// <summary>
    /// 列表页面基础视图模型
    /// </summary>
    /// <typeparam name="T">列表项数据类型</typeparam>
    public abstract class BaseListViewModel<T> : ServiceViewModel where T : class {

        #region 私有字段

        private string _pageTitle = "列表页面";
        private string _searchText = string.Empty;
        private bool _isLoading;
        private bool _isEmpty;
        private bool _showPagination = true;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount;
        private int _totalPages;
        private bool _hasSelectedItems;
        private int _selectedItemsCount;
        private object? _filterContent;
        private object? _listContent;

        #endregion 私有字段

        #region 构造函数

        protected BaseListViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
            // 初始化集合
            Items = new ObservableCollection<T>();
            SelectedItems = new ObservableCollection<T>();

            // 初始化命令
            InitializeCommands();

            // 订阅搜索文本变化
            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SearchText)) {
                    _ = SearchAsync();
                }
            };
        }

        /// <summary>
        /// 简化构造函数（使用ContainerLocator）
        /// </summary>
        protected BaseListViewModel() : base(GetEventAggregator(), GetErrorHandlingService()) {
            // 初始化集合
            Items = new ObservableCollection<T>();
            SelectedItems = new ObservableCollection<T>();

            // 初始化命令
            InitializeCommands();

            // 订阅搜索文本变化
            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SearchText)) {
                    _ = SearchAsync();
                }
            };
        }

        /// <summary>
        /// 获取EventAggregator实例
        /// </summary>
        private static IEventAggregator GetEventAggregator() {
            try {
                return (IEventAggregator?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IEventAggregator))
                    ?? new EventAggregator();
            } catch {
                return new EventAggregator();
            }
        }

        private static IErrorHandlingService GetErrorHandlingService() {
            try {
                return (IErrorHandlingService?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IErrorHandlingService))
                    ?? throw new InvalidOperationException("ErrorHandlingService未注册");
            } catch {
                throw new InvalidOperationException("无法解析ErrorHandlingService");
            }
        }

        #endregion 构造函数

        #region 属性

        /// <summary>
        /// 页面标题
        /// </summary>
        public string PageTitle {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public new bool IsLoading {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        /// <summary>
        /// 是否显示分页
        /// </summary>
        public bool ShowPagination {
            get => _showPagination;
            set => SetProperty(ref _showPagination, value);
        }

        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPage {
            get => _currentPage;
            set {
                if (SetProperty(ref _currentPage, value)) {
                    _ = LoadDataAsync();
                }
            }
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize {
            get => _pageSize;
            set {
                if (SetProperty(ref _pageSize, value)) {
                    CurrentPage = 1;
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount {
            get => _totalCount;
            set {
                if (SetProperty(ref _totalCount, value)) {
                    TotalPages = (int)Math.Ceiling((double)value / PageSize);
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages {
            get => _totalPages;
            private set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelectedItems {
            get => _hasSelectedItems;
            set => SetProperty(ref _hasSelectedItems, value);
        }

        /// <summary>
        /// 选中项数量
        /// </summary>
        public int SelectedItemsCount {
            get => _selectedItemsCount;
            set => SetProperty(ref _selectedItemsCount, value);
        }

        /// <summary>
        /// 筛选内容
        /// </summary>
        public object? FilterContent {
            get => _filterContent;
            set => SetProperty(ref _filterContent, value);
        }

        /// <summary>
        /// 列表内容
        /// </summary>
        public object? ListContent {
            get => _listContent;
            set => SetProperty(ref _listContent, value);
        }

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<T> Items { get; }

        /// <summary>
        /// 选中项集合
        /// </summary>
        public ObservableCollection<T> SelectedItems { get; }

        #endregion 属性

        #region 命令

        public ICommand AddCommand { get; private set; } = null!;
        public new ICommand RefreshCommand { get; private set; } = null!;
        public ICommand BatchDisableCommand { get; private set; } = null!;
        public ICommand FirstPageCommand { get; private set; } = null!;
        public ICommand PreviousPageCommand { get; private set; } = null!;
        public ICommand NextPageCommand { get; private set; } = null!;
        public ICommand LastPageCommand { get; private set; } = null!;

        #endregion 命令

        #region 方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitializeCommands() {
            AddCommand = new DelegateCommand(async () => await ExecuteAddAsync());
            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync());
            BatchDisableCommand = new DelegateCommand(async () => await ExecuteBatchDisableAsync(), CanExecuteBatchDisable)
                .ObservesProperty(() => HasSelectedItems);

            FirstPageCommand = new DelegateCommand(() => CurrentPage = 1, () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
            PreviousPageCommand = new DelegateCommand(() => CurrentPage--, () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
            NextPageCommand = new DelegateCommand(() => CurrentPage++, () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);
            LastPageCommand = new DelegateCommand(() => CurrentPage = TotalPages, () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);

            // 监听选中项变化
            SelectedItems.CollectionChanged += (s, e) => {
                HasSelectedItems = SelectedItems.Any();
                SelectedItemsCount = SelectedItems.Count;
            };
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public virtual async Task LoadDataAsync() {
            try {
                IsLoading = true;
                Items.Clear();

                var data = await GetDataAsync();
                foreach (var item in data) {
                    Items.Add(item);
                }

                IsEmpty = !Items.Any();
            } catch (Exception ex) {
                await HandleErrorAsync(ex);
            } finally {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        protected virtual async Task SearchAsync() {
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
        protected virtual async Task ExecuteBatchDisableAsync() {
            // 使用错误处理服务进行确认对话框
            var confirmed = await ExecuteSafelyAsync(async () => {
                // 这里应该使用对话框服务，但BaseListViewModel不直接依赖ICustomDialogService
                // 所以我们通过事件或其他方式来处理
                StatusMessage = $"请确认批量禁用选中的 {SelectedItemsCount} 项";

                // 简化实现：直接执行操作，子类可以重写此方法来添加确认对话框
                await PerformBatchDisableAsync(SelectedItems.ToList());
                SelectedItems.Clear();
                await LoadDataAsync();
                StatusMessage = "批量禁用操作成功！";
            }, "批量禁用操作");
        }

        /// <summary>
        /// 执行批量禁用操作（由子类实现）
        /// </summary>
        protected virtual Task PerformBatchDisableAsync(IList<T> items) {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 是否可以执行批量禁用
        /// </summary>
        protected virtual bool CanExecuteBatchDisable() {
            return HasSelectedItems;
        }

        /// <summary>
        /// 处理错误
        /// </summary>
        protected virtual async Task HandleErrorAsync(Exception ex) {
            await base.HandleErrorAsync("操作", ex);
        }

        #endregion 方法
    }
}

using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>统一列表ViewModel基类 - 提供分页、搜索、选择、批量操作等功能</summary>
    public abstract class UnifiedListViewModelBase<T> : UnifiedViewModelBase where T : class
    {
        private ObservableCollection<T> _items = new();
        private ObservableCollection<T> _selectedItems = new();
        private T? _selectedItem;
        private string _searchText = string.Empty;
        private int _totalCount = 0;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private bool _hasSelection = false;
        private string _busyMessage = "正在加载...";
        private CancellationTokenSource? _searchCancellationTokenSource;

        /// <summary>标记是否已完成初始化，防止初始化期间属性变化触发重复查询</summary>
        private bool _isInitialized = false;

        public ObservableCollection<T> Items { get => _items; set => SetProperty(ref _items, value); }
        public ObservableCollection<T> SelectedItems { get => _selectedItems; set { if (SetProperty(ref _selectedItems, value)) { HasSelection = value?.Count > 0; RefreshCanExecuteChanged(); } } }
        public T? SelectedItem { get => _selectedItem; set { if (SetProperty(ref _selectedItem, value)) RefreshCanExecuteChanged(); } }
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value) && _isInitialized) _ = SearchWithDebounceAsync(); } }
        public int TotalCount { get => _totalCount; protected set => SetProperty(ref _totalCount, value); }
        public int CurrentPage { get => _currentPage; set { if (SetProperty(ref _currentPage, value) && _isInitialized) _ = LoadPageAsync(); } }
        public int PageSize { get => _pageSize; set { if (SetProperty(ref _pageSize, value)) { _currentPage = 1; RaisePropertyChanged(nameof(CurrentPage)); if (_isInitialized) _ = LoadPageAsync(); } } }
        public bool HasSelection { get => _hasSelection; private set => SetProperty(ref _hasSelection, value); }

        /// <summary>可选的分页大小列表</summary>
        public int[] PageSizes { get; } = [10, 20, 50, 100];
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool CanGoPreviousPage => CurrentPage > 1;
        public bool CanGoNextPage => CurrentPage < TotalPages;
        public string BusyMessage { get => _busyMessage; set => SetProperty(ref _busyMessage, value); }

        public DelegateCommand SearchCommand { get; private set; } = null!;
        public DelegateCommand RefreshCommand { get; private set; } = null!;
        public DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<T> DeleteCommand { get; private set; } = null!;
        public DelegateCommand FirstPageCommand { get; private set; } = null!;
        public DelegateCommand LastPageCommand { get; private set; } = null!;
        public DelegateCommand BatchDeleteCommand { get; private set; } = null!;
        public DelegateCommand PreviousPageCommand { get; private set; } = null!;
        public DelegateCommand NextPageCommand { get; private set; } = null!;
        public DelegateCommand ClearSearchCommand { get; private set; } = null!;

        protected UnifiedListViewModelBase(
            IEventAggregator eventAggregator, ILoggerFactory loggerFactory, IRegionManager regionManager,
            ISessionManager? sessionManager = null, IUserNotificationService? userNotificationService = null, ICommonDialogService? commonDialogService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            InitializeListCommands();
            _selectedItems.CollectionChanged += (s, e) => { HasSelection = _selectedItems.Count > 0; RefreshCanExecuteChanged(); };
        }

        protected override void InitializeCommands() { base.InitializeCommands(); InitializeListCommands(); }

        private void InitializeListCommands()
        {
            SearchCommand = new DelegateCommand(async () => await SearchAsync(), () => !IsLoading);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsLoading);
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), CanExecuteAdd);
            DeleteCommand = new DelegateCommand<T>(async item => await ExecuteDeleteAsync(item), CanExecuteDelete);
            BatchDeleteCommand = new DelegateCommand(async () => await ExecuteBatchDeleteAsync(), CanExecuteBatchDelete);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, () => CanGoPreviousPage && !IsLoading);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, () => CanGoNextPage && !IsLoading);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);
            ClearSearchCommand = new DelegateCommand(ExecuteClearSearch, () => !string.IsNullOrEmpty(SearchText));
        }

        protected abstract Task<IEnumerable<T>> GetItemsAsync(int page, int pageSize, string? searchText);
        protected virtual async Task OnExecuteAddAsync() => await Task.CompletedTask;
        protected virtual async Task OnExecuteDeleteAsync(T item) => await Task.CompletedTask;
        protected abstract Task OnExecuteBatchDeleteAsync(List<T> items);

        public async Task LoadPageAsync(bool showLoading = true)
        {
            await ExecuteSafelyAsync(async () =>
            {
                if (showLoading) IsLoading = true;
                try
                {
                    var items = await GetItemsAsync(CurrentPage, PageSize, SearchText);
                    RunOnUIThread(() => { Items = new ObservableCollection<T>(items); RefreshPagingProperties(); });
                }
                finally { if (showLoading) IsLoading = false; }
            }, "加载数据");
        }

        public async Task SearchAsync() { CurrentPage = 1; await LoadPageAsync(false); }
        public async Task RefreshAsync() => await LoadPageAsync(false);
        public async Task ForceRefreshAsync() => await LoadPageAsync(true);

        private async Task SearchWithDebounceAsync()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            try { await Task.Delay(200, _searchCancellationTokenSource.Token); await SearchAsync(); }
            catch (OperationCanceledException) { }
        }

        private void RefreshPagingProperties() { RaisePropertyChanged(nameof(TotalPages)); RaisePropertyChanged(nameof(CanGoPreviousPage)); RaisePropertyChanged(nameof(CanGoNextPage)); RefreshCanExecuteChanged(); }

        private async Task ExecuteDeleteAsync(T item)
        {
            if (item == null) return;
            await ExecuteSafelyAsync(async () => { await OnExecuteDeleteAsync(item); await RefreshAsync(); }, "删除项目");
        }

        private async Task ExecuteBatchDeleteAsync()
        {
            if (SelectedItems == null || SelectedItems.Count == 0) return;
            var confirmed = await ShowConfirmationAsync($"确认删除选中的 {SelectedItems.Count} 个项目吗？\n此操作不可恢复。", "批量删除确认");
            if (!confirmed) return;
            var itemsToDelete = SelectedItems.ToList();
            await ExecuteSafelyAsync(async () => { await OnExecuteBatchDeleteAsync(itemsToDelete); SelectedItems.Clear(); await RefreshAsync(); }, $"批量删除{itemsToDelete.Count}个项目");
        }

        private void ExecutePreviousPage() { if (CanGoPreviousPage) CurrentPage--; }
        private void ExecuteNextPage() { if (CanGoNextPage) CurrentPage++; }
        private void ExecuteFirstPage() { if (CanGoPreviousPage) CurrentPage = 1; }
        private void ExecuteLastPage() { if (CanGoNextPage && TotalPages > 0) CurrentPage = TotalPages; }
        private void ExecuteClearSearch() => SearchText = string.Empty;

        protected virtual bool CanExecuteAdd() => !IsLoading;
        protected virtual bool CanExecuteDelete(T item) => item != null && !IsLoading;
        protected virtual bool CanExecuteBatchDelete() => HasSelection && !IsLoading;

        protected override void RefreshCommands() { base.RefreshCommands(); RefreshCanExecuteChanged(); }

        protected virtual void RefreshCanExecuteChanged()
        {
            SearchCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
            AddCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            BatchDeleteCommand?.RaiseCanExecuteChanged();
            PreviousPageCommand?.RaiseCanExecuteChanged();
            NextPageCommand?.RaiseCanExecuteChanged();
            ClearSearchCommand?.RaiseCanExecuteChanged();
        }

        protected void SetError(string message, string? propertyName = null) { if (!string.IsNullOrEmpty(propertyName)) AddValidationError(propertyName, message); else ErrorMessage = message; }
        protected void ClearError(string? propertyName = null) { if (!string.IsNullOrEmpty(propertyName)) ClearValidationErrors(propertyName); else ClearError(); }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadPageAsync(false);
            _isInitialized = true;  // 初始化完成后设置标志，此后属性变化才触发查询
        }
    }
}

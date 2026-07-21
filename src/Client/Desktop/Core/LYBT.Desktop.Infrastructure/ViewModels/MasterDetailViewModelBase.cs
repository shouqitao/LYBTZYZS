using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// Master-Detail视图ViewModel基类V2（组合模式）
    ///
    /// 继承NavigableViewModelBase获得导航、日志、EventAggregator、RegionManager等服务，
    /// 通过IMasterDetailServices组合获取列表/详情/分页/搜索等Master-Detail专用服务。
    /// 事件桥接和命令逻辑已提取到 ServiceEventBridge 和 MasterDetailCommandGroup。
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public abstract partial class MasterDetailViewModelBase<TListItem, TDetail>
        : NavigableViewModelBase, IAsyncInitializable,
          IServiceEventCallback<TListItem, TDetail>,
          ICommandHost<TListItem, TDetail>
        where TListItem : class
        where TDetail : class
    {
        private readonly IMasterDetailServices<TListItem, TDetail> _masterDetailServices;
        private readonly ServiceEventBridge<TListItem, TDetail> _eventBridge;
        private readonly MasterDetailCommandGroup<TListItem, TDetail> _commands;

        /// <summary>Master-Detail服务</summary>
        protected IMasterDetailServices<TListItem, TDetail> MasterDetailServices => _masterDetailServices;

        /// <summary>数据列表</summary>
        public ObservableCollection<TListItem> Items { get; } = new();

        #region 委托属性 - Loading

        public string? BusyMessage => _masterDetailServices.Loading.BusyMessage;

        #endregion

        #region 委托属性 - Pagination

        public int CurrentPage => _masterDetailServices.Pagination.CurrentPage;
        public int PageSize
        {
            get => _masterDetailServices.Pagination.PageSize;
            set => _masterDetailServices.Pagination.PageSize = value;
        }
        public int TotalCount => _masterDetailServices.Pagination.TotalCount;
        public int TotalPages => _masterDetailServices.Pagination.TotalPages;
        public IReadOnlyList<int> PageSizes => _masterDetailServices.Pagination.PageSizes;

        #endregion

        #region 委托属性 - Search

        public string SearchText
        {
            get => _masterDetailServices.Search.SearchText;
            set => _masterDetailServices.Search.SearchText = value;
        }
        public bool IsSearching => _masterDetailServices.Search.IsSearching;

        #endregion

        #region 委托属性 - Selection

        public TListItem? SelectedItem
        {
            get => _masterDetailServices.Selection.SelectedItem;
            set => _masterDetailServices.Selection.Select(value);
        }
        public ObservableCollection<TListItem> SelectedItems => _masterDetailServices.Selection.SelectedItems;
        public bool HasSelection => _masterDetailServices.Selection.HasSelection;
        public bool ShowDetailPanel => HasSelection || IsEditMode;

        #endregion

        #region 委托属性 - DetailEditor

        public TDetail? CurrentDetail => _masterDetailServices.DetailEditor.CurrentDetail;
        public bool IsEditMode => _masterDetailServices.DetailEditor.IsEditMode;
        public bool IsNew => _masterDetailServices.DetailEditor.IsNew;

        #endregion

        #region 实体元数据与详情标题

        protected abstract string EntityDisplayName { get; }
        protected virtual string NewEntityVerb => "新建";
        protected virtual string? GetDetailDisplayName() => null;

        public virtual string DetailTitle
        {
            get
            {
                if (CurrentDetail == null) return $"{EntityDisplayName}详情";
                if (IsNew) return $"{NewEntityVerb}{EntityDisplayName}";
                var displayName = GetDetailDisplayName();
                var suffix = displayName != null ? $" - {displayName}" : string.Empty;
                return IsEditMode
                    ? $"编辑{EntityDisplayName}{suffix}"
                    : $"{EntityDisplayName}详情{suffix}";
            }
        }

        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        #endregion

        public override bool KeepAlive => false;

        protected MasterDetailViewModelBase(
            IViewModelServices services,
            IMasterDetailServices<TListItem, TDetail> masterDetailServices)
            : base(services)
        {
            _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
            _eventBridge = new ServiceEventBridge<TListItem, TDetail>(masterDetailServices, this);
            _commands = new MasterDetailCommandGroup<TListItem, TDetail>(masterDetailServices, this);
            _commands.NotifyAllCommandsChanged();
        }

        #region 命令委托（委托给 CommandGroup）

        [RelayCommand] protected Task RefreshAsync() => _commands.RefreshCommand.ExecuteAsync(null);
        [RelayCommand] protected Task SearchAsync() => _commands.SearchCommand.ExecuteAsync(null);
        [RelayCommand] protected Task ClearSearchAsync() => _commands.ClearSearchCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))] protected Task GoToFirstPageAsync() => _commands.GoToFirstPageCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))] protected Task GoToPreviousPageAsync() => _commands.GoToPreviousPageCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanGoToNextPage))] protected Task GoToNextPageAsync() => _commands.GoToNextPageCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanGoToLastPage))] protected Task GoToLastPageAsync() => _commands.GoToLastPageCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanCreateNew))] protected Task CreateNewAsync() => _commands.CreateNewCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanEdit))] protected void Edit() => _commands.EditCommand.Execute(null);
        [RelayCommand(CanExecute = nameof(CanSave))] protected Task SaveAsync() => _commands.SaveCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanCancel))] protected Task CancelAsync() => _commands.CancelCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanDelete))] protected Task DeleteAsync() => _commands.DeleteCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(HasSelection))] protected Task BatchEnableAsync() => _commands.BatchEnableCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(HasSelection))] protected Task BatchDisableAsync() => _commands.BatchDisableCommand.ExecuteAsync(null);
        [RelayCommand(CanExecute = nameof(CanRestore))] protected Task RestoreAsync() => _commands.RestoreCommand.ExecuteAsync(null);

        private bool CanGoToFirstPage() => _masterDetailServices.Pagination.CanGoToFirstPage;
        private bool CanGoToPreviousPage() => _masterDetailServices.Pagination.CanGoToPreviousPage;
        private bool CanGoToNextPage() => _masterDetailServices.Pagination.CanGoToNextPage;
        private bool CanGoToLastPage() => _masterDetailServices.Pagination.CanGoToLastPage;
        private bool CanCreateNew() => !IsEditMode && !IsBusy;
        private bool CanEdit() => HasSelection && !IsEditMode && !IsBusy;
        private bool CanSave() => IsEditMode && CurrentDetail != null && !IsBusy;
        private bool CanCancel() => IsEditMode;
        private bool CanDelete() => HasSelection && !IsEditMode && !IsBusy;
        private bool CanRestore() => HasSelection && !IsBusy && IsAdmin;

        #endregion

        #region IServiceEventCallback 实现

        void IServiceEventCallback<TListItem, TDetail>.OnPropertyChanged(string? propertyName)
            => OnPropertyChanged(propertyName);

        void IServiceEventCallback<TListItem, TDetail>.OnIsLoadingChanged(bool isLoading)
            => IsLoading = isLoading;

        void IServiceEventCallback<TListItem, TDetail>.OnIsBusyChanged(bool isBusy)
        {
            IsBusy = isBusy;
            _commands.NotifyCrudCommandsChanged();
        }

        void IServiceEventCallback<TListItem, TDetail>.OnPaginationCommandsChanged()
            => _commands.NotifyPaginationCommandsChanged();

        Task IServiceEventCallback<TListItem, TDetail>.OnPageChanged()
            => LoadListAsync();

        void IServiceEventCallback<TListItem, TDetail>.OnSelectionChanged()
        {
            _commands.NotifyCrudCommandsChanged();
            OnPropertyChanged(nameof(ShowDetailPanel));
        }

        async Task IServiceEventCallback<TListItem, TDetail>.OnSelectionItemChanged(SelectionChangedEventArgs<TListItem> e)
        {
            if (e.NewSelection != null)
                await LoadDetailAsync(e.NewSelection);
        }

        void IServiceEventCallback<TListItem, TDetail>.OnHasUnsavedChangesChanged(bool hasUnsavedChanges)
            => HasUnsavedChanges = hasUnsavedChanges;

        void IServiceEventCallback<TListItem, TDetail>.OnCrudCommandsChanged()
            => _commands.NotifyCrudCommandsChanged();

        void IServiceEventCallback<TListItem, TDetail>.OnDetailStateChanged()
        {
            OnPropertyChanged(nameof(ShowDetailPanel));
            OnPropertyChanged(nameof(DetailTitle));
        }

        void IServiceEventCallback<TListItem, TDetail>.OnErrorMessageChanged(string message)
            => ErrorMessage = message;

        #endregion

        #region ICommandHost 实现

        bool ICommandHost<TListItem, TDetail>.IsAdmin => IsAdmin;
        Task ICommandHost<TListItem, TDetail>.LoadListAsync() => LoadListAsync();
        TDetail ICommandHost<TListItem, TDetail>.CreateNewDetail() => CreateNewDetail();
        Task<bool> ICommandHost<TListItem, TDetail>.SaveDetailAsync(TDetail detail) => SaveDetailAsync(detail);
        Task<bool> ICommandHost<TListItem, TDetail>.DeleteItemAsync(TListItem item) => DeleteItemAsync(item);
        Task ICommandHost<TListItem, TDetail>.DeleteBatchAsync(List<TListItem> items) => DeleteBatchAsync(items);
        Task ICommandHost<TListItem, TDetail>.EnableBatchAsync(List<TListItem> items) => EnableBatchAsync(items);
        Task ICommandHost<TListItem, TDetail>.DisableBatchAsync(List<TListItem> items) => DisableBatchAsync(items);
        Task ICommandHost<TListItem, TDetail>.RestoreItemAsync(TListItem item) => RestoreItemAsync(item);
        Task ICommandHost<TListItem, TDetail>.InvalidateCachesAsync() => InvalidateCachesAsync();
        Task ICommandHost<TListItem, TDetail>.OnDetailCreatedAsync(TDetail detail) => OnDetailCreatedAsync(detail);
        Task ICommandHost<TListItem, TDetail>.OnDetailSavedAsync(TDetail detail) => OnDetailSavedAsync(detail);
        Task ICommandHost<TListItem, TDetail>.OnItemDeletedAsync(TListItem item) => OnItemDeletedAsync(item);

        List<TListItem> ICommandHost<TListItem, TDetail>.GetSelectedItemsForDelete()
        {
            if (SelectedItems != null && SelectedItems.Count > 0)
                return new List<TListItem>(SelectedItems);
            if (SelectedItem != null)
                return new List<TListItem> { SelectedItem };
            return new List<TListItem>();
        }

        #endregion

        #region 抽象方法

        protected abstract Task LoadListAsync();
        protected abstract Task LoadDetailAsync(TListItem item);
        protected abstract TDetail CreateNewDetail();
        protected abstract Task<bool> SaveDetailAsync(TDetail detail);
        protected abstract Task<bool> DeleteItemAsync(TListItem item);

        #endregion

        #region 虚拟方法 - 生命周期钩子

        protected virtual Task OnDetailCreatedAsync(TDetail detail) => Task.CompletedTask;
        protected virtual Task OnDetailSavedAsync(TDetail detail) => Task.CompletedTask;
        protected virtual Task OnItemDeletedAsync(TListItem item) => Task.CompletedTask;
        protected virtual async Task DeleteBatchAsync(List<TListItem> items)
        {
            foreach (var item in items)
            {
                var success = await DeleteItemAsync(item);
                if (success) await OnItemDeletedAsync(item);
            }
        }
        protected virtual async Task EnableBatchAsync(List<TListItem> items)
        {
            foreach (var item in items) await SetItemEnabledAsync(item, true);
        }
        protected virtual async Task DisableBatchAsync(List<TListItem> items)
        {
            foreach (var item in items) await SetItemEnabledAsync(item, false);
        }
        protected virtual Task RestoreItemAsync(TListItem item) => Task.CompletedTask;
        protected virtual Task InvalidateCachesAsync() => Task.CompletedTask;
        protected virtual Task SetItemEnabledAsync(TListItem item, bool enabled) => Task.CompletedTask;

        #endregion

        #region 初始化与导航

        public virtual async Task InitializeAsync()
        {
            Logger.LogDebug("初始化Master-Detail视图: {ViewType}", GetType().Name);
            await LoadListAsync();
        }

        protected override void OnNavigatedToCore(NavigationContext navigationContext)
        {
            OnNavigatedToAsync(navigationContext).SafeFireAndForget(
                ex => MasterDetailServices.ErrorHandler.HandleException(ex, $"OnNavigatedTo failed in {GetType().Name}"));
        }

        protected virtual async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            Logger.LogDebug("导航到Master-Detail视图: {ViewType}", GetType().Name);
            await LoadListAsync();
        }

        #endregion

        #region 资源清理

        protected override void OnDisposing()
        {
            _eventBridge.Dispose();
            _masterDetailServices.Dispose();
        }

        #endregion
    }
}

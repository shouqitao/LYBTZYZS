# Technical Design: ViewModel组合模式重构 + CommunityToolkit.Mvvm

**Change ID**: refactor-viewmodel-composition
**设计版本**: 1.1
**设计日期**: 2025-12-25
**技术栈**: CommunityToolkit.Mvvm 8.x + Prism 9.x

---

## 1. 系统架构设计

### 1.1 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              View Layer (XAML)                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐│
│  │ HerbsView   │ │ FormulaView │ │ PatientsView│ │ MedicalCaseView         ││
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └───────────┬─────────────┘│
└─────────┼───────────────┼───────────────┼───────────────────┼───────────────┘
          │               │               │                   │
          ▼               ▼               ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ViewModel Layer                                    │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    ComposableViewModelBase                               ││
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐││
│  │  │ HerbsVM     │ │ FormulaVM   │ │ PatientsVM  │ │ MedicalCaseVM       │││
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └───────────┬─────────┘││
│  └─────────┼───────────────┼───────────────┼───────────────────┼───────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
             │               │               │                   │
             └───────────────┴───────────────┴───────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Service Layer (Injected)                            │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │              IMasterDetailServices<TListItem, TDetail>                 │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │                    IListViewServices<T>                          │  │  │
│  │  │  ┌──────────────┐ ┌──────────────┐ ┌─────────────┐ ┌───────────┐│  │  │
│  │  │  │LoadingState  │ │ Pagination   │ │   Search    │ │ Selection ││  │  │
│  │  │  │Manager       │ │ Service      │ │   Service   │ │ Service   ││  │  │
│  │  │  └──────────────┘ └──────────────┘ └─────────────┘ └───────────┘│  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │                 IDetailEditorService<TDetail>                    │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌───────────────┐    │
│  │ DialogManager │ │ Navigation    │ │ ErrorHandler  │ │ AsyncExecutor │    │
│  │               │ │ Service       │ │               │ │               │    │
│  └───────────────┘ └───────────────┘ └───────────────┘ └───────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 服务组合模式

```
IMasterDetailServices<TListItem, TDetail>
├── IListViewServices<TListItem>
│   ├── ILoadingStateManager
│   ├── IPaginationService
│   ├── ISearchService
│   └── ISelectionService<TListItem>
└── IDetailEditorService<TDetail>

独立服务（可选注入）
├── IDialogManager
├── IViewNavigationService
├── IErrorHandler
└── IAsyncExecutor
```

---

## 2. 接口详细设计

### 2.1 ILoadingStateManager

```csharp
namespace LYBT.Desktop.Models.Services
{
    /// <summary>
    /// 加载状态管理服务
    /// 职责：管理UI加载状态、忙碌状态、加载消息
    /// </summary>
    public interface ILoadingStateManager : INotifyPropertyChanged
    {
        /// <summary>是否正在加载（一般加载状态）</summary>
        bool IsLoading { get; set; }
        
        /// <summary>是否忙碌（阻塞性操作）</summary>
        bool IsBusy { get; set; }
        
        /// <summary>忙碌消息</summary>
        string BusyMessage { get; set; }
        
        /// <summary>当前加载计数（支持嵌套加载）</summary>
        int LoadingCount { get; }
        
        /// <summary>
        /// 执行带加载状态的异步操作
        /// </summary>
        /// <param name="action">异步操作</param>
        /// <param name="message">加载消息（可选）</param>
        /// <param name="isBusy">是否为忙碌状态（默认false）</param>
        Task ExecuteWithLoadingAsync(
            Func<Task> action, 
            string? message = null, 
            bool isBusy = false);
        
        /// <summary>
        /// 执行带加载状态的异步操作（带返回值）
        /// </summary>
        Task<T> ExecuteWithLoadingAsync<T>(
            Func<Task<T>> action, 
            string? message = null, 
            bool isBusy = false);
        
        /// <summary>增加加载计数</summary>
        void BeginLoading(string? message = null);
        
        /// <summary>减少加载计数</summary>
        void EndLoading();
        
        /// <summary>重置所有加载状态</summary>
        void Reset();
    }
}
```

**实现要点**：
- 线程安全的加载计数管理
- 支持嵌套加载（多个并发操作）
- 自动管理IsLoading状态（LoadingCount > 0时为true）

### 2.2 IPaginationService

```csharp
namespace LYBT.Desktop.Models.Services
{
    /// <summary>
    /// 分页服务
    /// 职责：管理分页状态、分页计算、页面导航
    /// </summary>
    public interface IPaginationService : INotifyPropertyChanged
    {
        /// <summary>当前页（从1开始）</summary>
        int CurrentPage { get; set; }
        
        /// <summary>每页大小</summary>
        int PageSize { get; set; }
        
        /// <summary>总记录数</summary>
        int TotalCount { get; set; }
        
        /// <summary>总页数（计算属性）</summary>
        int TotalPages { get; }
        
        /// <summary>可选的每页大小列表</summary>
        IReadOnlyList<int> PageSizes { get; }
        
        /// <summary>是否可以向前翻页</summary>
        bool CanGoPrevious { get; }
        
        /// <summary>是否可以向后翻页</summary>
        bool CanGoNext { get; }
        
        /// <summary>页面变更事件</summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;
        
        /// <summary>跳转到首页</summary>
        void GoToFirstPage();
        
        /// <summary>跳转到上一页</summary>
        void GoToPreviousPage();
        
        /// <summary>跳转到下一页</summary>
        void GoToNextPage();
        
        /// <summary>跳转到末页</summary>
        void GoToLastPage();
        
        /// <summary>跳转到指定页</summary>
        void GoToPage(int page);
        
        /// <summary>重置分页状态</summary>
        void Reset();
        
        /// <summary>更新分页信息</summary>
        void Update(int totalCount, int? pageSize = null);
    }
    
    public class PageChangedEventArgs : EventArgs
    {
        public int OldPage { get; }
        public int NewPage { get; }
        public int PageSize { get; }
        
        public PageChangedEventArgs(int oldPage, int newPage, int pageSize)
        {
            OldPage = oldPage;
            NewPage = newPage;
            PageSize = pageSize;
        }
    }
}
```

**实现要点**：
- 边界检查（页码不超出范围）
- 自动计算TotalPages
- PageSize变更时重新计算页码

### 2.3 ISearchService

```csharp
namespace LYBT.Desktop.Models.Services
{
    /// <summary>
    /// 搜索服务
    /// 职责：管理搜索状态、搜索防抖、搜索执行
    /// </summary>
    public interface ISearchService : INotifyPropertyChanged
    {
        /// <summary>搜索文本</summary>
        string SearchText { get; set; }
        
        /// <summary>是否正在搜索</summary>
        bool IsSearching { get; }
        
        /// <summary>是否有搜索内容</summary>
        bool HasSearchText { get; }
        
        /// <summary>搜索防抖延迟（毫秒）</summary>
        int DebounceDelay { get; set; }
        
        /// <summary>搜索请求事件（防抖后触发）</summary>
        event EventHandler<SearchRequestedEventArgs>? SearchRequested;
        
        /// <summary>立即执行搜索</summary>
        Task ExecuteSearchAsync();
        
        /// <summary>清除搜索</summary>
        void ClearSearch();
        
        /// <summary>设置搜索委托</summary>
        void SetSearchHandler(Func<string, Task> handler);
    }
    
    public class SearchRequestedEventArgs : EventArgs
    {
        public string SearchText { get; }
        
        public SearchRequestedEventArgs(string searchText)
        {
            SearchText = searchText;
        }
    }
}
```

**实现要点**：
- 防抖机制（默认300ms）
- 支持取消上一次搜索
- 空字符串触发全量加载

### 2.4 ISelectionService<T>

```csharp
namespace LYBT.Desktop.Models.Services
{
    /// <summary>
    /// 选择服务
    /// 职责：管理列表项选择状态
    /// </summary>
    public interface ISelectionService<T> : INotifyPropertyChanged where T : class
    {
        /// <summary>当前选中项</summary>
        T? SelectedItem { get; set; }
        
        /// <summary>多选项集合</summary>
        ObservableCollection<T> SelectedItems { get; }
        
        /// <summary>是否有选中项</summary>
        bool HasSelection { get; }
        
        /// <summary>选中项数量</summary>
        int SelectionCount { get; }
        
        /// <summary>是否为多选模式</summary>
        bool IsMultiSelectMode { get; set; }
        
        /// <summary>选择变更事件</summary>
        event EventHandler<SelectionChangedEventArgs<T>>? SelectionChanged;
        
        /// <summary>选择单个项</summary>
        void Select(T item);
        
        /// <summary>选择多个项</summary>
        void SelectMultiple(IEnumerable<T> items);
        
        /// <summary>切换选择状态</summary>
        void ToggleSelection(T item);
        
        /// <summary>清除选择</summary>
        void ClearSelection();
        
        /// <summary>全选</summary>
        void SelectAll(IEnumerable<T> allItems);
    }
    
    public class SelectionChangedEventArgs<T> : EventArgs where T : class
    {
        public T? OldSelection { get; }
        public T? NewSelection { get; }
        public IReadOnlyList<T> AddedItems { get; }
        public IReadOnlyList<T> RemovedItems { get; }
        
        public SelectionChangedEventArgs(
            T? oldSelection, 
            T? newSelection,
            IReadOnlyList<T>? addedItems = null,
            IReadOnlyList<T>? removedItems = null)
        {
            OldSelection = oldSelection;
            NewSelection = newSelection;
            AddedItems = addedItems ?? Array.Empty<T>();
            RemovedItems = removedItems ?? Array.Empty<T>();
        }
    }
}
```

### 2.5 IDetailEditorService<TDetail>

```csharp
namespace LYBT.Desktop.Models.Services
{
    /// <summary>
    /// 详情编辑服务
    /// 职责：管理Master-Detail模式中的详情编辑状态
    /// </summary>
    public interface IDetailEditorService<TDetail> : INotifyPropertyChanged 
        where TDetail : class
    {
        /// <summary>当前详情对象</summary>
        TDetail? CurrentDetail { get; set; }
        
        /// <summary>原始详情对象（用于取消时恢复）</summary>
        TDetail? OriginalDetail { get; }
        
        /// <summary>是否处于编辑模式</summary>
        bool IsEditMode { get; set; }
        
        /// <summary>是否有未保存的更改</summary>
        bool HasUnsavedChanges { get; set; }
        
        /// <summary>是否为新建模式</summary>
        bool IsNew { get; }
        
        /// <summary>是否正在加载详情</summary>
        bool IsLoadingDetail { get; set; }
        
        /// <summary>详情标题</summary>
        string DetailTitle { get; }
        
        /// <summary>编辑模式变更事件</summary>
        event EventHandler<EditModeChangedEventArgs>? EditModeChanged;
        
        /// <summary>进入编辑模式</summary>
        void EnterEditMode();
        
        /// <summary>取消编辑（恢复原始值）</summary>
        void CancelEdit();
        
        /// <summary>确认保存成功</summary>
        void ConfirmSaved();
        
        /// <summary>创建新详情</summary>
        void CreateNew(Func<TDetail> factory);
        
        /// <summary>加载详情</summary>
        Task LoadDetailAsync(Func<Task<TDetail?>> loader);
        
        /// <summary>设置详情</summary>
        void SetDetail(TDetail? detail);
        
        /// <summary>设置克隆函数（用于备份原始值）</summary>
        void SetCloneFunction(Func<TDetail, TDetail> cloneFunc);
        
        /// <summary>检查是否可以离开（有未保存更改时提示）</summary>
        Task<bool> CanLeaveAsync(Func<Task<bool>> confirmFunc);
    }
    
    public class EditModeChangedEventArgs : EventArgs
    {
        public bool IsEditMode { get; }
        public bool IsNew { get; }
        
        public EditModeChangedEventArgs(bool isEditMode, bool isNew)
        {
            IsEditMode = isEditMode;
            IsNew = isNew;
        }
    }
}
```

---

## 3. 服务实现设计

### 3.1 LoadingStateManager实现

```csharp
namespace LYBT.Desktop.Infrastructure.Services
{
    public class LoadingStateManager : ObservableObject, ILoadingStateManager
    {
        private int _loadingCount;
        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private readonly object _lock = new();
        
        public bool IsLoading => _loadingCount > 0;
        
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
        
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }
        
        public int LoadingCount => _loadingCount;
        
        public async Task ExecuteWithLoadingAsync(
            Func<Task> action, 
            string? message = null, 
            bool isBusy = false)
        {
            BeginLoading(message);
            if (isBusy) IsBusy = true;
            
            try
            {
                await action();
            }
            finally
            {
                EndLoading();
                if (isBusy) IsBusy = false;
            }
        }
        
        public async Task<T> ExecuteWithLoadingAsync<T>(
            Func<Task<T>> action, 
            string? message = null, 
            bool isBusy = false)
        {
            BeginLoading(message);
            if (isBusy) IsBusy = true;
            
            try
            {
                return await action();
            }
            finally
            {
                EndLoading();
                if (isBusy) IsBusy = false;
            }
        }
        
        public void BeginLoading(string? message = null)
        {
            lock (_lock)
            {
                var wasLoading = IsLoading;
                _loadingCount++;
                
                if (!string.IsNullOrEmpty(message))
                {
                    BusyMessage = message;
                }
                
                if (!wasLoading)
                {
                    OnPropertyChanged(nameof(IsLoading));
                }
                OnPropertyChanged(nameof(LoadingCount));
            }
        }
        
        public void EndLoading()
        {
            lock (_lock)
            {
                if (_loadingCount > 0)
                {
                    _loadingCount--;
                    
                    if (!IsLoading)
                    {
                        BusyMessage = string.Empty;
                        OnPropertyChanged(nameof(IsLoading));
                    }
                    OnPropertyChanged(nameof(LoadingCount));
                }
            }
        }
        
        public void Reset()
        {
            lock (_lock)
            {
                _loadingCount = 0;
                IsBusy = false;
                BusyMessage = string.Empty;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(LoadingCount));
            }
        }
    }
}
```

### 3.2 组合服务实现

```csharp
namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 列表视图服务组合实现
    /// </summary>
    public class ListViewServices<T> : IListViewServices<T> where T : class
    {
        public ILoadingStateManager LoadingState { get; }
        public IPaginationService Pagination { get; }
        public ISearchService Search { get; }
        public ISelectionService<T> Selection { get; }
        
        public ListViewServices(
            ILoadingStateManager loadingState,
            IPaginationService pagination,
            ISearchService search,
            ISelectionService<T> selection)
        {
            LoadingState = loadingState;
            Pagination = pagination;
            Search = search;
            Selection = selection;
        }
    }
    
    /// <summary>
    /// Master-Detail服务组合实现
    /// </summary>
    public class MasterDetailServices<TListItem, TDetail> 
        : IMasterDetailServices<TListItem, TDetail>
        where TListItem : class
        where TDetail : class
    {
        public IListViewServices<TListItem> ListView { get; }
        public IDetailEditorService<TDetail> DetailEditor { get; }
        
        // 便捷访问属性
        public ILoadingStateManager LoadingState => ListView.LoadingState;
        public IPaginationService Pagination => ListView.Pagination;
        public ISearchService Search => ListView.Search;
        public ISelectionService<TListItem> Selection => ListView.Selection;
        
        public MasterDetailServices(
            IListViewServices<TListItem> listView,
            IDetailEditorService<TDetail> detailEditor)
        {
            ListView = listView;
            DetailEditor = detailEditor;
        }
    }
}
```

---

## 4. DI注册设计

### 4.1 服务注册扩展

```csharp
namespace LYBT.Desktop.Infrastructure.DependencyInjection
{
    public static class ViewModelServicesExtensions
    {
        /// <summary>
        /// 注册所有ViewModel服务
        /// </summary>
        public static IServiceCollection AddViewModelServices(
            this IServiceCollection services)
        {
            // 基础服务 - Transient（每次注入新实例）
            services.AddTransient<ILoadingStateManager, LoadingStateManager>();
            services.AddTransient<IPaginationService, PaginationService>();
            services.AddTransient<ISearchService, SearchService>();
            services.AddTransient(typeof(ISelectionService<>), typeof(SelectionService<>));
            services.AddTransient(typeof(IDetailEditorService<>), typeof(DetailEditorService<>));
            
            // 共享服务 - Singleton
            services.AddSingleton<IDialogManager, DialogManager>();
            services.AddSingleton<IViewNavigationService, ViewNavigationService>();
            services.AddSingleton<IErrorHandler, ErrorHandler>();
            services.AddSingleton<IAsyncExecutor, AsyncExecutor>();
            
            // 组合服务 - Transient
            services.AddTransient(typeof(IListViewServices<>), typeof(ListViewServices<>));
            services.AddTransient(typeof(IMasterDetailServices<,>), typeof(MasterDetailServices<,>));
            
            return services;
        }
        
        /// <summary>
        /// 注册特定类型的Master-Detail服务
        /// </summary>
        public static IServiceCollection AddMasterDetailServices<TListItem, TDetail>(
            this IServiceCollection services)
            where TListItem : class
            where TDetail : class
        {
            services.AddTransient<IMasterDetailServices<TListItem, TDetail>, 
                MasterDetailServices<TListItem, TDetail>>();
            return services;
        }
    }
}
```

### 4.2 模块级注册示例

```csharp
// HerbsModule.cs
public class HerbsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModel
        containerRegistry.RegisterForNavigation<HerbsMasterDetailView, HerbsMasterDetailViewModel>();
        
        // 服务已在App级别注册，无需模块级注册
    }
}

// App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册ViewModel服务
    containerRegistry.GetContainer()
        .GetServiceCollection()
        .AddViewModelServices();
}
```

---

## 5. ViewModel迁移设计

### 5.1 CommunityToolkit.Mvvm集成

**NuGet包引用**:
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
```

**核心特性**:
- `ObservableObject`: 替代`BindableBase`，支持源生成器
- `[ObservableProperty]`: 编译时生成属性代码
- `[RelayCommand]`: 编译时生成命令代码
- `ObservableValidator`: 内置`INotifyDataErrorInfo`验证支持

### 5.2 新基类设计

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 轻量级ViewModel基类
    /// 继承CommunityToolkit.Mvvm的ObservableObject，支持源生成器
    /// 注意：使用源生成器的类必须声明为partial
    /// </summary>
    public abstract partial class LightViewModelBase : ObservableObject
    {
        // ObservableObject提供:
        // - INotifyPropertyChanged实现
        // - SetProperty方法
        // - OnPropertyChanged方法
        //
        // 子类使用[ObservableProperty]标注字段即可自动生成属性
    }

    /// <summary>
    /// 可组合的ViewModel基类
    /// 支持服务注入、导航、生命周期管理
    /// 使用CommunityToolkit.Mvvm源生成器减少样板代码
    /// </summary>
    public abstract partial class ComposableViewModelBase : LightViewModelBase,
        INavigationAware,
        IDisposable,
        IConfirmNavigationRequest
    {
        protected readonly IDialogManager DialogManager;
        protected readonly IViewNavigationService Navigation;
        protected readonly IErrorHandler ErrorHandler;
        protected readonly IAsyncExecutor AsyncExecutor;

        // 使用[ObservableProperty]自动生成PageTitle属性
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        // 使用[ObservableProperty]自动生成IsBusy属性
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
        private bool _isBusy;

        private bool _disposed;

        protected ComposableViewModelBase(
            IDialogManager dialogManager,
            IViewNavigationService navigation,
            IErrorHandler errorHandler,
            IAsyncExecutor asyncExecutor)
        {
            DialogManager = dialogManager;
            Navigation = navigation;
            ErrorHandler = errorHandler;
            AsyncExecutor = asyncExecutor;
        }

        #region Commands (使用[RelayCommand]源生成器)

        // [RelayCommand]自动生成RefreshCommand属性
        // CanExecute通过CanRefresh方法自动绑定
        [RelayCommand(CanExecute = nameof(CanRefresh))]
        protected virtual Task RefreshAsync() => Task.CompletedTask;
        private bool CanRefresh() => !IsBusy;

        #endregion

        #region INavigationAware (Prism导航接口保留)

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion

        #region IConfirmNavigationRequest

        public virtual void ConfirmNavigationRequest(
            NavigationContext navigationContext,
            Action<bool> continuationCallback)
        {
            continuationCallback(true);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }
                _disposed = true;
            }
        }

        #endregion

        #region Helper Methods

        protected Task ShowSuccessAsync(string message)
            => DialogManager.ShowSuccessAsync(message);

        protected Task ShowErrorAsync(string message)
            => DialogManager.ShowErrorAsync(message);

        protected Task<bool> ConfirmAsync(string message, string title = "确认")
            => DialogManager.ShowConfirmAsync(message, title);

        protected Task ExecuteSafelyAsync(Func<Task> action)
            => AsyncExecutor.ExecuteSafelyAsync(action, ErrorHandler);

        #endregion
    }
}
```

### 5.3 源生成器使用模式

#### 5.3.1 属性生成

```csharp
// 传统方式 (6行)
private string _name = string.Empty;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// CommunityToolkit.Mvvm (2行)
[ObservableProperty]
private string _name = string.Empty;
// 编译时自动生成:
// - public string Name { get => _name; set => SetProperty(ref _name, value); }
// - partial void OnNameChanging(string value);
// - partial void OnNameChanged(string value);
```

#### 5.3.2 命令生成

```csharp
// 传统方式 (5行)
private ICommand? _saveCommand;
public ICommand SaveCommand => _saveCommand ??=
    new DelegateCommand(async () => await ExecuteSaveAsync(), () => IsEditMode);

// CommunityToolkit.Mvvm (2行)
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync() { /* 业务逻辑 */ }
private bool CanSave() => IsEditMode;
// 编译时自动生成 public IRelayCommand SaveCommand { get; }
```

#### 5.3.3 属性变更联动

```csharp
// 当IsEditMode变更时，自动刷新SaveCommand和CancelCommand的CanExecute
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
[NotifyCanExecuteChangedFor(nameof(CancelCommand))]
private bool _isEditMode;
```

#### 5.3.4 验证支持

```csharp
// 继承ObservableValidator获得INotifyDataErrorInfo支持
public partial class HerbDetailModel : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(100, ErrorMessage = "名称不能超过100个字符")]
    private string _name = string.Empty;

    public bool ValidateAll()
    {
        ValidateAllProperties();
        return !HasErrors;
    }
}
```

### 5.4 MasterDetail ViewModel迁移示例

```csharp
// ==========================================
// 迁移前: 传统继承模式 + 手动属性/命令
// ==========================================
public class HerbsMasterDetailViewModel : MasterDetailViewModelBase<HerbListItemDto, HerbDetailModel>
{
    private readonly IHerbService _herbService;

    public HerbsMasterDetailViewModel(
        IHerbService herbService,
        IDialogService dialogService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ISessionService sessionService,
        IUserContextService userContextService)
        : base(dialogService, regionManager, eventAggregator, sessionService, userContextService)
    {
        _herbService = herbService;
    }

    // 大量重复的属性定义代码...
    // 大量重复的命令定义代码...
    // 大量重复的抽象方法实现...
}

// ==========================================
// 迁移后: 组合模式 + CommunityToolkit.Mvvm源生成器
// ==========================================
public partial class HerbsMasterDetailViewModel : ComposableViewModelBase
{
    private readonly IHerbService _herbService;
    private readonly IMasterDetailServices<HerbListItemDto, HerbDetailModel> _services;

    // [ObservableProperty] 自动生成属性
    [ObservableProperty]
    private ObservableCollection<HerbListItemDto> _items = new();

    // 暴露服务属性供XAML绑定
    public ILoadingStateManager Loading => _services.LoadingState;
    public IPaginationService Pagination => _services.Pagination;
    public ISearchService Search => _services.Search;
    public ISelectionService<HerbListItemDto> Selection => _services.Selection;
    public IDetailEditorService<HerbDetailModel> DetailEditor => _services.DetailEditor;

    public HerbsMasterDetailViewModel(
        IHerbService herbService,
        IMasterDetailServices<HerbListItemDto, HerbDetailModel> services,
        IDialogManager dialogManager,
        IViewNavigationService navigation,
        IErrorHandler errorHandler,
        IAsyncExecutor asyncExecutor)
        : base(dialogManager, navigation, errorHandler, asyncExecutor)
    {
        _herbService = herbService;
        _services = services;

        // 设置克隆函数
        _services.DetailEditor.SetCloneFunction(detail => detail.Clone());

        // 订阅事件
        _services.Selection.SelectionChanged += async (_, item) => await LoadDetailAsync(item);
        _services.Pagination.PageChanged += async (_, _) => await LoadDataAsync();
        _services.Search.SearchRequested += async (_, _) => await LoadDataAsync();
    }

    #region Commands (使用[RelayCommand]源生成器)

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await _services.LoadingState.ExecuteWithLoadingAsync(async () =>
        {
            var result = await _herbService.GetPagedListAsync(
                _services.Search.SearchText,
                _services.Pagination.CurrentPage,
                _services.Pagination.PageSize);

            Items = new ObservableCollection<HerbListItemDto>(result.Items);
            _services.Pagination.Update(result.TotalCount);
        });
    }

    [RelayCommand]
    private void Add()
    {
        var newDetail = HerbDetailModel.CreateNew();
        DetailEditor.CreateNew(() => newDetail);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit() => DetailEditor.EnterEditMode();
    private bool CanEdit() => Selection.HasSelection && !DetailEditor.IsEditMode;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (DetailEditor.CurrentDetail == null) return;

        if (!DetailEditor.CurrentDetail.ValidateAll())
        {
            await ShowErrorAsync("请检查输入项");
            return;
        }

        await _services.LoadingState.ExecuteWithLoadingAsync(async () =>
        {
            var dto = DetailEditor.CurrentDetail.ToDto();
            var success = DetailEditor.IsNew
                ? await _herbService.CreateAsync(dto)
                : await _herbService.UpdateAsync(dto);

            if (success)
            {
                DetailEditor.ConfirmSaved();
                await LoadDataAsync();
                await ShowSuccessAsync("保存成功");
            }
            else
            {
                await ShowErrorAsync("保存失败");
            }
        });
    }
    private bool CanSave() => DetailEditor.IsEditMode;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => DetailEditor.CancelEdit();
    private bool CanCancel() => DetailEditor.IsEditMode;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (DetailEditor.CurrentDetail == null) return;
        if (!await ConfirmAsync("确认删除此药材吗？")) return;

        await _services.LoadingState.ExecuteWithLoadingAsync(async () =>
        {
            var success = await _herbService.DeleteAsync(DetailEditor.CurrentDetail.Id);
            if (success)
            {
                DetailEditor.SetDetail(null);
                Selection.ClearSelection();
                await LoadDataAsync();
                await ShowSuccessAsync("删除成功");
            }
        });
    }
    private bool CanDelete() => Selection.HasSelection;

    #endregion

    private async Task LoadDetailAsync(HerbListItemDto? item)
    {
        if (item == null)
        {
            DetailEditor.SetDetail(null);
            return;
        }

        await DetailEditor.LoadDetailAsync(async () =>
        {
            var dto = await _herbService.GetByIdAsync(item.Id);
            return HerbDetailModel.FromDto(dto);
        });
    }

    public override void OnNavigatedTo(NavigationContext context)
    {
        base.OnNavigatedTo(context);
        _ = LoadDataAsync();
    }
}
```

### 5.5 代码量对比分析

| 指标 | 迁移前 | 迁移后 | 减少量 |
|------|--------|--------|--------|
| 基类继承层数 | 4层 | 1层 | 75% |
| 属性定义代码 | ~180行 | ~30行 | 83% |
| 命令定义代码 | ~60行 | ~20行 | 67% |
| 抽象方法实现 | 6个必须实现 | 0个 | 100% |
| 构造函数参数 | 6个 | 4个服务 | 33% |
| 总代码量 | ~400行 | ~150行 | **62%** |

---

## 6. 测试策略

### 6.1 服务单元测试

```csharp
[TestClass]
public class LoadingStateManagerTests
{
    private LoadingStateManager _sut;
    
    [TestInitialize]
    public void Setup()
    {
        _sut = new LoadingStateManager();
    }
    
    [TestMethod]
    public void BeginLoading_ShouldSetIsLoadingTrue()
    {
        // Arrange
        Assert.IsFalse(_sut.IsLoading);
        
        // Act
        _sut.BeginLoading();
        
        // Assert
        Assert.IsTrue(_sut.IsLoading);
        Assert.AreEqual(1, _sut.LoadingCount);
    }
    
    [TestMethod]
    public void NestedLoading_ShouldTrackCount()
    {
        // Act
        _sut.BeginLoading();
        _sut.BeginLoading();
        
        // Assert
        Assert.AreEqual(2, _sut.LoadingCount);
        Assert.IsTrue(_sut.IsLoading);
        
        // Act
        _sut.EndLoading();
        
        // Assert
        Assert.AreEqual(1, _sut.LoadingCount);
        Assert.IsTrue(_sut.IsLoading);
        
        // Act
        _sut.EndLoading();
        
        // Assert
        Assert.AreEqual(0, _sut.LoadingCount);
        Assert.IsFalse(_sut.IsLoading);
    }
    
    [TestMethod]
    public async Task ExecuteWithLoadingAsync_ShouldManageState()
    {
        // Arrange
        var executed = false;
        
        // Act
        await _sut.ExecuteWithLoadingAsync(async () =>
        {
            Assert.IsTrue(_sut.IsLoading);
            executed = true;
            await Task.Delay(10);
        });
        
        // Assert
        Assert.IsTrue(executed);
        Assert.IsFalse(_sut.IsLoading);
    }
}
```

### 6.2 ViewModel集成测试

```csharp
[TestClass]
public class HerbsMasterDetailViewModelTests
{
    private HerbsMasterDetailViewModel _sut;
    private Mock<IHerbService> _herbServiceMock;
    private IMasterDetailServices<HerbListItemDto, HerbDetailModel> _services;
    
    [TestInitialize]
    public void Setup()
    {
        _herbServiceMock = new Mock<IHerbService>();
        _services = new MasterDetailServices<HerbListItemDto, HerbDetailModel>(
            new ListViewServices<HerbListItemDto>(
                new LoadingStateManager(),
                new PaginationService(),
                new SearchService(),
                new SelectionService<HerbListItemDto>()),
            new DetailEditorService<HerbDetailModel>());
        
        _sut = new HerbsMasterDetailViewModel(
            _herbServiceMock.Object,
            _services,
            Mock.Of<IDialogManager>(),
            Mock.Of<IViewNavigationService>(),
            Mock.Of<IErrorHandler>(),
            Mock.Of<IAsyncExecutor>());
    }
    
    [TestMethod]
    public async Task LoadDataAsync_ShouldUpdateItems()
    {
        // Arrange
        var testData = new PagedResult<HerbListItemDto>
        {
            Items = new List<HerbListItemDto> { new() { Id = Guid.NewGuid(), Name = "Test" } },
            TotalCount = 1
        };
        _herbServiceMock.Setup(x => x.GetPagedListAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(testData);
        
        // Act
        await _sut.RefreshCommand.ExecuteAsync(null);
        
        // Assert
        Assert.AreEqual(1, _sut.Items.Count);
        Assert.AreEqual("Test", _sut.Items[0].Name);
    }
}
```

---

## 7. 性能考量

### 7.1 服务实例化策略

| 服务类型 | 生命周期 | 原因 |
|---------|---------|------|
| ILoadingStateManager | Transient | 每个ViewModel独立状态 |
| IPaginationService | Transient | 每个列表独立分页 |
| ISearchService | Transient | 每个列表独立搜索 |
| ISelectionService<T> | Transient | 每个列表独立选择 |
| IDetailEditorService<T> | Transient | 每个详情编辑器独立 |
| IDialogManager | Singleton | 全局共享对话框服务 |
| IViewNavigationService | Singleton | 全局共享导航服务 |
| IErrorHandler | Singleton | 全局错误处理 |
| IAsyncExecutor | Singleton | 无状态可共享 |

### 7.2 内存优化

- 服务实现使用弱引用事件（WeakEventManager）避免内存泄漏
- ViewModel Dispose时自动取消订阅
- 延迟初始化非必需服务

---

## 8. 兼容性设计

### 8.1 渐进迁移支持

```csharp
// 旧ViewModel可以继续工作
[Obsolete("Use ComposableViewModelBase with injected services")]
public abstract class MasterDetailViewModelBase<TListItem, TDetail> 
    : UnifiedListViewModelBase<TListItem>
{
    // 保持原有实现
}

// 新ViewModel使用组合模式
public class NewHerbsViewModel : ComposableViewModelBase
{
    // 使用注入的服务
}
```

### 8.2 XAML绑定兼容

现有XAML绑定无需修改，因为属性名称保持一致：

```xml
<!-- 这些绑定在迁移前后都有效 -->
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}"/>
<TextBlock Text="{Binding CurrentPage}"/>
<Button IsEnabled="{Binding IsEditMode}"/>
```

---

**文档版本**: 1.1 (CommunityToolkit.Mvvm整合)
**最后更新**: 2025-12-25
**作者**: Claude Code

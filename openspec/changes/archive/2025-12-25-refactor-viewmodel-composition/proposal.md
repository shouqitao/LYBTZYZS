# Proposal: ViewModel组合模式重构 + CommunityToolkit.Mvvm

**Change ID**: `refactor-viewmodel-composition`
**Type**: Architecture Refactoring
**Priority**: P2 (Post-Release)
**Status**: Draft
**Author**: Claude Code
**Created**: 2025-12-25
**Updated**: 2025-12-25
**Target Version**: v1.1.0+

---

## 1. Executive Summary

### 1.1 问题陈述

当前Desktop层ViewModel采用**4层深度继承架构**：

```
BindableBase → ViewModelBase → UnifiedViewModelBase → UnifiedListViewModelBase → MasterDetailViewModelBase
```

这种架构存在以下问题：

| 问题 | 影响 | 严重程度 |
|------|------|----------|
| 继承深度过深 | 理解和调试困难，新开发者学习曲线陡峭 | 中 |
| 职责耦合 | ViewModelBase承担过多职责（验证、错误处理、HTTP处理、异步执行） | 中 |
| 构造函数爆炸 | 6个参数导致DI配置复杂，测试困难 | 中 |
| 功能强制继承 | 简单列表ViewModel被迫继承MasterDetail的全部抽象方法 | 中 |
| 可测试性差 | 需要完整Prism基础设施才能单元测试 | 高 |
| 大量样板代码 | 每个属性需要6行代码，每个命令需要3-5行 | 高 |

### 1.2 提案目标

采用**组合优于继承 + CommunityToolkit.Mvvm源生成器**双重策略重构ViewModel层：

1. **扁平化继承**：从4层减少到1层（ObservableObject → ViewModel）
2. **服务组合**：通过DI注入可复用的功能服务
3. **按需组合**：ViewModel只注入需要的功能
4. **源生成器**：使用`[ObservableProperty]`和`[RelayCommand]`消除样板代码
5. **高可测试性**：每个服务可独立Mock
6. **符合SOLID**：单一职责、依赖倒置

### 1.3 技术栈变更

| 组件 | 当前 | 变更后 | 说明 |
|------|------|--------|------|
| 基类 | `Prism.BindableBase` | `CommunityToolkit.Mvvm.ObservableObject` | 支持源生成器 |
| 属性 | 手动SetProperty | `[ObservableProperty]` | 编译时生成 |
| 命令 | `DelegateCommand` | `[RelayCommand]` | 编译时生成 |
| 验证 | 自定义 | `ObservableValidator` | 内置INotifyDataErrorInfo |
| 导航 | `Prism.INavigationAware` | **保留** | Prism核心功能 |
| 区域 | `Prism.IRegionManager` | **保留** | Prism核心功能 |
| 对话框 | `Prism.IDialogService` | **保留** | Prism核心功能 |
| DI容器 | `DryIoc` | **保留** | Prism核心功能 |

### 1.4 预期收益

| 收益 | 量化指标 |
|------|----------|
| 代码可测试性 | 单元测试覆盖率可从0%提升到80%+ |
| 新ViewModel开发效率 | **减少90%样板代码** |
| 属性定义代码量 | 从6行减少到1行 (**-83%**) |
| 命令定义代码量 | 从5行减少到1行 (**-80%**) |
| 理解成本 | 无需理解4层继承链，只看构造函数注入 |
| 灵活性 | 支持按需组合，不再强制继承 |
| 编译时安全 | CanExecute绑定编译时检查 |

---

## 2. Current State Analysis（现状分析）

### 2.1 继承层次详解

#### Layer 1: ViewModelBase (351行)

**职责**：
- `INotifyPropertyChanged` (继承自BindableBase)
- `INotifyDataErrorInfo` 验证错误管理
- `IDisposable` 资源清理
- 异步执行 (`ExecuteSafelyAsync`, `SafeExecuteAsync`)
- HTTP状态码处理 (401/409/504)
- 错误消息管理
- UI线程调度 (`RunOnUIThread`)

**依赖**：
- `IEventAggregator` (Prism事件聚合器)
- `ILoggerFactory` (日志工厂)

**问题**：
- 职责过多，违反SRP
- HTTP处理与ViewModel核心职责无关
- 验证逻辑与ValidatableModelBase重复

#### Layer 2: UnifiedViewModelBase (237行)

**职责**：
- `INavigationAware` 导航感知
- `IRegionMemberLifetime` 生命周期管理
- Region导航 (`NavigateTo`, `NavigateBack`)
- 会话管理 (`SessionManager`)
- 对话框服务 (`CommonDialogService`)
- 用户通知 (`UserNotificationService`)
- 属性验证 (`ValidateProperty`, `ValidateAllProperties`)

**新增依赖**：
- `IRegionManager` (Prism区域管理器)
- `ISessionManager` (会话管理)
- `IUserNotificationService` (用户通知)
- `ICommonDialogService` (对话框服务)

**问题**：
- 构造函数参数达到6个
- 导航、会话、对话框三种不同关注点耦合

#### Layer 3: UnifiedListViewModelBase<T> (183行)

**职责**：
- 列表数据管理 (`ObservableCollection<T> Items`)
- 分页 (`CurrentPage`, `PageSize`, `TotalPages`)
- 搜索 (`SearchText`, `SearchWithDebounceAsync`)
- 批量选择 (`SelectedItems`, `HasSelection`)
- 批量删除 (`BatchDeleteCommand`)
- 分页导航命令 (`FirstPage`, `LastPage`, `PreviousPage`, `NextPage`)

**问题**：
- 分页、搜索、选择三种关注点耦合
- 抽象方法强制所有子类实现

#### Layer 4: MasterDetailViewModelBase<TListItem, TDetail> (541行)

**职责**：
- 详情加载 (`LoadDetailAsync`, `CurrentDetail`)
- 编辑模式管理 (`IsEditMode`, `HasUnsavedChanges`)
- CRUD操作 (`SaveCommand`, `DeleteCommand`, `CancelCommand`)
- 详情取消加载 (`CancellationTokenSource`)
- 资源清理 (`OnDisposing`)

**抽象方法（子类必须实现）**：
```csharp
protected abstract Task<TDetail?> LoadDetailAsync(TListItem item);
protected abstract Task<bool> SaveDetailAsync(TDetail detail);
protected abstract Task<bool> DeleteDetailAsync(TDetail detail);
protected abstract TDetail CreateNewDetail();
protected abstract TDetail CloneDetail(TDetail detail);
protected abstract object? GetDetailId(TDetail detail);
```

**问题**：
- 541行代码量过大
- 6个抽象方法强制所有子类实现
- 简单列表页也被迫使用此基类

### 2.2 功能点提取与分类

经过详细分析，当前基类的**47个功能点**可分为**8个关注领域**：

| 领域 | 功能点数量 | 示例 |
|------|-----------|------|
| **状态管理** | 6 | IsBusy, IsLoading, IsLoadingDetail, BusyMessage, StatusMessage, ErrorMessage |
| **验证** | 5 | HasErrors, GetErrors, AddValidationError, ClearValidationErrors, ValidateProperty |
| **导航** | 7 | NavigateTo, NavigateBack, NavigateForward, OnNavigatedTo, OnNavigatedFrom, IsNavigationTarget, KeepAlive |
| **对话框** | 4 | ShowSuccessMessageAsync, ShowErrorMessageAsync, ShowWarningMessageAsync, ShowConfirmationAsync |
| **分页** | 8 | CurrentPage, PageSize, TotalPages, TotalCount, FirstPage, LastPage, PreviousPage, NextPage |
| **搜索** | 4 | SearchText, SearchCommand, SearchWithDebounceAsync, ClearSearchCommand |
| **选择** | 4 | SelectedItem, SelectedItems, HasSelection, RefreshCanExecuteChanged |
| **详情编辑** | 9 | CurrentDetail, IsEditMode, HasUnsavedChanges, EditCommand, SaveCommand, CancelCommand, DeleteCurrentCommand, LoadDetailAsync, CloneDetail |

### 2.3 当前ViewModel统计

| 模块 | ViewModel数量 | 使用的基类 |
|------|--------------|-----------|
| Auth | 3 | UnifiedViewModelBase |
| Users | 2 | MasterDetailViewModelBase |
| Patients | 3 | MasterDetailViewModelBase |
| MedicalCase | 3 | MasterDetailViewModelBase |
| Consultation | 3 | MasterDetailViewModelBase |
| Prescriptions | 2 | MasterDetailViewModelBase |
| Herbs | 2 | MasterDetailViewModelBase |
| Formula | 2 | MasterDetailViewModelBase |
| **总计** | **38** | |

---

## 3. Proposed Architecture（提案架构）

### 3.1 架构概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            ViewModel Layer                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │HerbsViewModel│  │UsersViewModel│  │FormulaViewModel│ │LoginViewModel│    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         │                 │                 │                 │             │
│         └─────────────────┼─────────────────┼─────────────────┘             │
│                           │ Composition via DI                              │
│                           ▼                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                         Service Components                                   │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌──────────────┐ │
│  │ILoadingManager │ │IPaginationSvc  │ │IDetailEditorSvc│ │IDialogManager│ │
│  └────────────────┘ └────────────────┘ └────────────────┘ └──────────────┘ │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌──────────────┐ │
│  │ISearchManager  │ │ISelectionMgr   │ │INavigationSvc  │ │IErrorHandler │ │
│  └────────────────┘ └────────────────┘ └────────────────┘ └──────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│                         Core Infrastructure                                  │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐ ┌──────────────┐ │
│  │BindableBase   │ │IEventAggregator│ │ILoggerFactory  │ │IServiceProvider│
│  └────────────────┘ └────────────────┘ └────────────────┘ └──────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 核心设计原则

1. **扁平继承**：所有ViewModel只继承`BindableBase`（或`ObservableObject`）
2. **服务注入**：通过构造函数注入所需服务组件
3. **接口隔离**：每个服务只暴露单一职责的接口
4. **生命周期管理**：服务可以是Singleton、Scoped或Transient
5. **可测试性**：所有服务接口化，支持Mock

### 3.3 服务接口设计

#### 3.3.1 状态管理服务

```csharp
/// <summary>
/// 加载状态管理器 - 管理ViewModel的加载状态
/// </summary>
public interface ILoadingStateManager : INotifyPropertyChanged
{
    /// <summary>是否正在加载</summary>
    bool IsLoading { get; set; }
    
    /// <summary>是否繁忙（更通用的状态）</summary>
    bool IsBusy { get; set; }
    
    /// <summary>加载提示消息</summary>
    string BusyMessage { get; set; }
    
    /// <summary>状态消息</summary>
    string StatusMessage { get; set; }
    
    /// <summary>执行带加载状态的异步操作</summary>
    Task ExecuteWithLoadingAsync(Func<Task> action, string? message = null);
    
    /// <summary>执行带加载状态的异步操作（带返回值）</summary>
    Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? message = null);
}
```

#### 3.3.2 分页服务

```csharp
/// <summary>
/// 分页服务 - 管理列表分页逻辑
/// </summary>
public interface IPaginationService : INotifyPropertyChanged
{
    /// <summary>当前页码（从1开始）</summary>
    int CurrentPage { get; set; }
    
    /// <summary>每页大小</summary>
    int PageSize { get; set; }
    
    /// <summary>总记录数</summary>
    int TotalCount { get; set; }
    
    /// <summary>总页数</summary>
    int TotalPages { get; }
    
    /// <summary>是否可以前往上一页</summary>
    bool CanGoPrevious { get; }
    
    /// <summary>是否可以前往下一页</summary>
    bool CanGoNext { get; }
    
    /// <summary>可选的分页大小列表</summary>
    int[] PageSizeOptions { get; }
    
    /// <summary>前往首页</summary>
    void GoToFirst();
    
    /// <summary>前往末页</summary>
    void GoToLast();
    
    /// <summary>前往上一页</summary>
    void GoToPrevious();
    
    /// <summary>前往下一页</summary>
    void GoToNext();
    
    /// <summary>页码变更事件</summary>
    event EventHandler<int>? PageChanged;
    
    /// <summary>重置分页状态</summary>
    void Reset();
}
```

#### 3.3.3 搜索服务

```csharp
/// <summary>
/// 搜索服务 - 管理搜索逻辑（支持防抖）
/// </summary>
public interface ISearchService : INotifyPropertyChanged
{
    /// <summary>搜索文本</summary>
    string SearchText { get; set; }
    
    /// <summary>是否正在搜索</summary>
    bool IsSearching { get; }
    
    /// <summary>防抖延迟（毫秒）</summary>
    int DebounceDelay { get; set; }
    
    /// <summary>搜索触发事件（防抖后触发）</summary>
    event EventHandler<string>? SearchTriggered;
    
    /// <summary>立即触发搜索（跳过防抖）</summary>
    void TriggerSearchImmediately();
    
    /// <summary>清除搜索</summary>
    void Clear();
    
    /// <summary>取消正在进行的搜索</summary>
    void Cancel();
}
```

#### 3.3.4 选择服务

```csharp
/// <summary>
/// 选择服务 - 管理列表选择状态
/// </summary>
public interface ISelectionService<T> : INotifyPropertyChanged where T : class
{
    /// <summary>当前选中项</summary>
    T? SelectedItem { get; set; }
    
    /// <summary>选中项集合（用于批量操作）</summary>
    ObservableCollection<T> SelectedItems { get; }
    
    /// <summary>是否有选中项</summary>
    bool HasSelection { get; }
    
    /// <summary>选中项数量</summary>
    int SelectionCount { get; }
    
    /// <summary>选中项变更事件</summary>
    event EventHandler<T?>? SelectionChanged;
    
    /// <summary>清除所有选择</summary>
    void ClearSelection();
    
    /// <summary>选择全部（需要提供所有项）</summary>
    void SelectAll(IEnumerable<T> allItems);
    
    /// <summary>反选</summary>
    void InvertSelection(IEnumerable<T> allItems);
}
```

#### 3.3.5 详情编辑服务

```csharp
/// <summary>
/// 详情编辑服务 - 管理Master-Detail模式的详情编辑状态
/// </summary>
public interface IDetailEditorService<TDetail> : INotifyPropertyChanged where TDetail : class
{
    /// <summary>当前详情数据</summary>
    TDetail? CurrentDetail { get; set; }
    
    /// <summary>原始详情数据（用于取消编辑时恢复）</summary>
    TDetail? OriginalDetail { get; }
    
    /// <summary>是否处于编辑模式</summary>
    bool IsEditMode { get; set; }
    
    /// <summary>是否正在加载详情</summary>
    bool IsLoadingDetail { get; set; }
    
    /// <summary>是否有未保存的更改</summary>
    bool HasUnsavedChanges { get; set; }
    
    /// <summary>是否有详情数据</summary>
    bool HasDetail { get; }
    
    /// <summary>进入编辑模式</summary>
    void EnterEditMode();
    
    /// <summary>取消编辑（恢复原始数据）</summary>
    void CancelEdit();
    
    /// <summary>确认保存成功（清除编辑状态）</summary>
    void ConfirmSaved();
    
    /// <summary>设置详情（非编辑模式）</summary>
    void SetDetail(TDetail? detail);
    
    /// <summary>设置详情并进入编辑模式（用于新建）</summary>
    void SetDetailForCreate(TDetail detail);
    
    /// <summary>标记数据已修改</summary>
    void MarkAsModified();
    
    /// <summary>克隆详情对象</summary>
    Func<TDetail, TDetail>? CloneFunc { get; set; }
}
```

#### 3.3.6 对话框服务

```csharp
/// <summary>
/// 对话框管理器 - 统一的对话框交互接口
/// </summary>
public interface IDialogManager
{
    /// <summary>显示成功消息</summary>
    Task ShowSuccessAsync(string message, string? title = null);
    
    /// <summary>显示错误消息</summary>
    Task ShowErrorAsync(string message, string? title = null);
    
    /// <summary>显示警告消息</summary>
    Task ShowWarningAsync(string message, string? title = null);
    
    /// <summary>显示信息消息</summary>
    Task ShowInfoAsync(string message, string? title = null);
    
    /// <summary>显示确认对话框</summary>
    Task<bool> ShowConfirmAsync(string message, string? title = null);
    
    /// <summary>显示带选项的确认对话框</summary>
    Task<DialogResult> ShowConfirmWithCancelAsync(string message, string? title = null);
}

public enum DialogResult
{
    Yes,
    No,
    Cancel
}
```

#### 3.3.7 导航服务

```csharp
/// <summary>
/// 导航服务 - 封装Prism导航逻辑
/// </summary>
public interface IViewNavigationService
{
    /// <summary>导航到指定视图</summary>
    void NavigateTo(string viewName, NavigationParameters? parameters = null);
    
    /// <summary>导航到指定区域的视图</summary>
    void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null);
    
    /// <summary>返回上一页</summary>
    bool GoBack(string? regionName = null);
    
    /// <summary>前进到下一页</summary>
    bool GoForward(string? regionName = null);
    
    /// <summary>是否可以返回</summary>
    bool CanGoBack(string? regionName = null);
    
    /// <summary>是否可以前进</summary>
    bool CanGoForward(string? regionName = null);
    
    /// <summary>返回首页</summary>
    void NavigateToHome();
}
```

#### 3.3.8 错误处理服务

```csharp
/// <summary>
/// 错误处理服务 - 统一的异常处理和错误消息管理
/// </summary>
public interface IErrorHandler : INotifyPropertyChanged
{
    /// <summary>当前错误消息</summary>
    string? ErrorMessage { get; }
    
    /// <summary>是否有错误</summary>
    bool HasError { get; }
    
    /// <summary>处理异常</summary>
    void HandleException(Exception ex, string? context = null);
    
    /// <summary>设置错误消息</summary>
    void SetError(string message);
    
    /// <summary>清除错误</summary>
    void ClearError();
    
    /// <summary>获取用户友好的错误消息</summary>
    string GetUserFriendlyMessage(Exception ex);
}
```

#### 3.3.9 异步执行器

```csharp
/// <summary>
/// 安全异步执行器 - 提供带异常处理的异步执行
/// </summary>
public interface IAsyncExecutor
{
    /// <summary>安全执行异步操作</summary>
    Task ExecuteSafelyAsync(Func<Task> action, string? context = null);
    
    /// <summary>安全执行异步操作（带返回值）</summary>
    Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, string? context = null);
    
    /// <summary>带HTTP状态码处理的安全执行</summary>
    Task<T?> ExecuteWithHttpHandlingAsync<T>(Func<Task<T>> action, string? context = null);
    
    /// <summary>在UI线程执行</summary>
    void RunOnUIThread(Action action);
    
    /// <summary>异常发生事件</summary>
    event EventHandler<Exception>? ExceptionOccurred;
}
```

### 3.4 复合服务（Facade）

为简化常见场景的使用，提供复合服务：

```csharp
/// <summary>
/// 列表视图服务 - 组合分页、搜索、选择功能
/// </summary>
public interface IListViewServices<T> where T : class
{
    ILoadingStateManager Loading { get; }
    IPaginationService Pagination { get; }
    ISearchService Search { get; }
    ISelectionService<T> Selection { get; }
    IErrorHandler ErrorHandler { get; }
    IDialogManager Dialogs { get; }
}

/// <summary>
/// Master-Detail视图服务 - 组合列表+详情编辑功能
/// </summary>
public interface IMasterDetailServices<TListItem, TDetail> : IListViewServices<TListItem>
    where TListItem : class
    where TDetail : class
{
    IDetailEditorService<TDetail> DetailEditor { get; }
}
```

### 3.5 新ViewModel示例（使用CommunityToolkit.Mvvm源生成器）

#### 3.5.1 简单列表ViewModel

```csharp
/// <summary>
/// 审计日志ViewModel - 只需要列表功能，无详情编辑
/// 使用CommunityToolkit.Mvvm源生成器减少样板代码
/// </summary>
public partial class AuditLogViewModel : ObservableObject, INavigationAware
{
    private readonly IListViewServices<AuditLogDto> _services;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogViewModel> _logger;

    // [ObservableProperty] 自动生成 Items 属性和 OnItemsChanged 方法
    [ObservableProperty]
    private ObservableCollection<AuditLogDto> _items = new();

    public AuditLogViewModel(
        IListViewServices<AuditLogDto> services,
        IAuditLogService auditLogService,
        ILogger<AuditLogViewModel> logger)
    {
        _services = services;
        _auditLogService = auditLogService;
        _logger = logger;

        // 订阅分页变更
        _services.Pagination.PageChanged += async (_, _) => await LoadDataAsync();

        // 订阅搜索触发
        _services.Search.SearchTriggered += async (_, _) => await LoadDataAsync();
    }

    // 暴露服务属性供XAML绑定
    public ILoadingStateManager Loading => _services.Loading;
    public IPaginationService Pagination => _services.Pagination;
    public ISearchService Search => _services.Search;

    // [RelayCommand] 自动生成 RefreshCommand 属性
    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        await _services.Loading.ExecuteWithLoadingAsync(async () =>
        {
            var result = await _auditLogService.GetPagedAsync(
                Pagination.CurrentPage,
                Pagination.PageSize,
                Search.SearchText);

            Items = new ObservableCollection<AuditLogDto>(result.Items);
            Pagination.TotalCount = result.TotalCount;
        }, "加载审计日志...");
    }

    public void OnNavigatedTo(NavigationContext context) => _ = LoadDataAsync();
    public bool IsNavigationTarget(NavigationContext context) => true;
    public void OnNavigatedFrom(NavigationContext context) { }
}
```

#### 3.5.2 Master-Detail ViewModel

```csharp
/// <summary>
/// 药材管理ViewModel - Master-Detail模式
/// 使用CommunityToolkit.Mvvm源生成器 + 服务组合
/// </summary>
public partial class HerbsViewModel : ObservableObject, INavigationAware, IDisposable
{
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _services;
    private readonly IHerbService _herbService;
    private readonly ILogger<HerbsViewModel> _logger;

    // [ObservableProperty] 自动生成属性和PropertyChanged通知
    [ObservableProperty]
    private ObservableCollection<HerbListDto> _items = new();

    public HerbsViewModel(
        IMasterDetailServices<HerbListDto, HerbDetailModel> services,
        IHerbService herbService,
        ILogger<HerbsViewModel> logger)
    {
        _services = services;
        _herbService = herbService;
        _logger = logger;

        // 订阅事件
        _services.Pagination.PageChanged += async (_, _) => await LoadDataAsync();
        _services.Search.SearchTriggered += async (_, _) => await LoadDataAsync();
        _services.Selection.SelectionChanged += async (_, item) => await LoadDetailAsync(item);

        // 设置克隆函数
        _services.DetailEditor.CloneFunc = detail => detail.Clone();
    }

    // 暴露服务属性供XAML绑定
    public ILoadingStateManager Loading => _services.Loading;
    public IPaginationService Pagination => _services.Pagination;
    public ISearchService Search => _services.Search;
    public ISelectionService<HerbListDto> Selection => _services.Selection;
    public IDetailEditorService<HerbDetailModel> DetailEditor => _services.DetailEditor;

    // [RelayCommand] 自动生成命令属性
    // async方法自动支持CanExecute绑定
    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private void Add()
    {
        var newDetail = HerbDetailModel.CreateNew();
        DetailEditor.SetDetailForCreate(newDetail);
    }

    // CanExecute通过方法名约定自动关联: CanEdit()
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit() => DetailEditor.EnterEditMode();
    private bool CanEdit() => DetailEditor.HasDetail && !DetailEditor.IsEditMode;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (DetailEditor.CurrentDetail == null) return;

        // 验证
        if (!DetailEditor.CurrentDetail.ValidateAll())
        {
            await _services.Dialogs.ShowWarningAsync("请检查输入项");
            return;
        }

        await Loading.ExecuteWithLoadingAsync(async () =>
        {
            var dto = DetailEditor.CurrentDetail.ToDto();
            var success = DetailEditor.CurrentDetail.IsNew
                ? await _herbService.CreateAsync(dto)
                : await _herbService.UpdateAsync(dto);

            if (success)
            {
                DetailEditor.ConfirmSaved();
                await LoadDataAsync();
                await _services.Dialogs.ShowSuccessAsync("保存成功");
            }
            else
            {
                await _services.Dialogs.ShowErrorAsync("保存失败，请重试");
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

        var confirmed = await _services.Dialogs.ShowConfirmAsync("确认删除此药材吗？");
        if (!confirmed) return;

        await Loading.ExecuteWithLoadingAsync(async () =>
        {
            var success = await _herbService.DeleteAsync(DetailEditor.CurrentDetail.Id);
            if (success)
            {
                DetailEditor.SetDetail(null);
                Selection.ClearSelection();
                await LoadDataAsync();
                await _services.Dialogs.ShowSuccessAsync("删除成功");
            }
            else
            {
                await _services.Dialogs.ShowErrorAsync("删除失败，请重试");
            }
        });
    }
    private bool CanDelete() => DetailEditor.HasDetail;

    private async Task LoadDataAsync()
    {
        await Loading.ExecuteWithLoadingAsync(async () =>
        {
            var result = await _herbService.GetPagedAsync(
                Pagination.CurrentPage,
                Pagination.PageSize,
                Search.SearchText);

            Items = new ObservableCollection<HerbListDto>(result.Items);
            Pagination.TotalCount = result.TotalCount;
        });
    }

    private async Task LoadDetailAsync(HerbListDto? item)
    {
        if (item == null)
        {
            DetailEditor.SetDetail(null);
            return;
        }

        DetailEditor.IsLoadingDetail = true;
        try
        {
            var detail = await _herbService.GetByIdAsync(item.Id);
            DetailEditor.SetDetail(HerbDetailModel.FromDto(detail));
        }
        finally
        {
            DetailEditor.IsLoadingDetail = false;
        }
    }

    public void OnNavigatedTo(NavigationContext context) => _ = LoadDataAsync();
    public bool IsNavigationTarget(NavigationContext context) => true;
    public void OnNavigatedFrom(NavigationContext context) { }

    public void Dispose()
    {
        // 取消订阅等清理
    }
}
```

#### 3.5.3 代码量对比

| 项目 | 传统方式 | CommunityToolkit.Mvvm |
|------|----------|----------------------|
| 属性定义 | 6行 | 2行 (`[ObservableProperty]` + 字段) |
| 命令定义 | 5行 | 1-2行 (`[RelayCommand]`) |
| CanExecute绑定 | 手动实现 | 编译时自动生成 |
| 类声明 | `class` | `partial class` |
| 基类 | `BindableBase` | `ObservableObject` |

**实际对比**:

```csharp
// 传统方式: 6行定义一个属性
private string _name = string.Empty;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// CommunityToolkit.Mvvm: 2行
[ObservableProperty]
private string _name = string.Empty;
// 编译时自动生成 public string Name { get; set; } 和 OnNameChanged()
```

```csharp
// 传统方式: 5行定义一个命令
private DelegateCommand? _saveCommand;
public DelegateCommand SaveCommand => _saveCommand ??=
    new DelegateCommand(async () => await ExecuteSaveAsync(), () => IsEditMode);

// CommunityToolkit.Mvvm: 1行标注
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync() { ... }
// 编译时自动生成 public IRelayCommand SaveCommand { get; }
```

### 3.6 DI注册配置

```csharp
public static class ViewModelServicesExtensions
{
    public static IServiceCollection AddViewModelServices(this IServiceCollection services)
    {
        // 核心服务 - Singleton
        services.AddSingleton<IDialogManager, DialogManager>();
        services.AddSingleton<IViewNavigationService, ViewNavigationService>();
        
        // 共享服务 - Scoped (每个View一个实例)
        services.AddScoped<ILoadingStateManager, LoadingStateManager>();
        services.AddScoped<IErrorHandler, ErrorHandler>();
        services.AddScoped<IAsyncExecutor, AsyncExecutor>();
        
        // 状态服务 - Transient (每次注入新实例)
        services.AddTransient<IPaginationService, PaginationService>();
        services.AddTransient<ISearchService, SearchService>();
        services.AddTransient(typeof(ISelectionService<>), typeof(SelectionService<>));
        services.AddTransient(typeof(IDetailEditorService<>), typeof(DetailEditorService<>));
        
        // 复合服务
        services.AddTransient(typeof(IListViewServices<>), typeof(ListViewServices<>));
        services.AddTransient(typeof(IMasterDetailServices<,>), typeof(MasterDetailServices<,>));
        
        return services;
    }
}
```

---

## 4. Migration Strategy（迁移策略）

### 4.1 迁移原则

1. **渐进式迁移**：不一次性替换所有ViewModel
2. **双轨运行**：新旧架构可以共存
3. **先简后繁**：先迁移简单ViewModel，再迁移复杂的
4. **充分测试**：每个迁移阶段都要验证功能正常

### 4.2 迁移阶段

#### Phase 1: 基础设施建设（2-3天）

**目标**：创建所有服务接口和实现

**任务清单**：
- [ ] 创建`LYBT.Desktop.Services`项目
- [ ] 实现`ILoadingStateManager`及其实现类
- [ ] 实现`IPaginationService`及其实现类
- [ ] 实现`ISearchService`及其实现类
- [ ] 实现`ISelectionService<T>`及其实现类
- [ ] 实现`IDetailEditorService<TDetail>`及其实现类
- [ ] 实现`IDialogManager`及其实现类
- [ ] 实现`IViewNavigationService`及其实现类
- [ ] 实现`IErrorHandler`及其实现类
- [ ] 实现`IAsyncExecutor`及其实现类
- [ ] 实现复合服务`IListViewServices<T>`和`IMasterDetailServices<TListItem, TDetail>`
- [ ] 配置DI注册
- [ ] 编写单元测试

#### Phase 2: 试点迁移 - Auth模块（1天）

**目标**：迁移最简单的模块验证架构

**任务清单**：
- [ ] 迁移`LoginViewModel`
- [ ] 迁移`ChangePasswordViewModel`
- [ ] 验证登录流程正常
- [ ] 验证导航正常

#### Phase 3: 试点迁移 - Herbs模块（2天）

**目标**：验证Master-Detail模式的迁移

**任务清单**：
- [ ] 迁移`HerbsMasterDetailViewModel`
- [ ] 验证列表加载、分页、搜索
- [ ] 验证详情加载、编辑、保存、删除
- [ ] 验证批量删除
- [ ] 对比新旧架构的代码量

#### Phase 4: 批量迁移 - 其他模块（5-7天）

**任务清单**：
- [ ] Users模块迁移
- [ ] Patients模块迁移
- [ ] MedicalCase模块迁移
- [ ] Consultation模块迁移
- [ ] Prescriptions模块迁移
- [ ] Formula模块迁移

#### Phase 5: 清理旧代码（1天）

**任务清单**：
- [ ] 删除`ViewModelBase`
- [ ] 删除`UnifiedViewModelBase`
- [ ] 删除`UnifiedListViewModelBase`
- [ ] 删除`MasterDetailViewModelBase`
- [ ] 更新文档

### 4.3 迁移兼容策略

在迁移期间，新旧ViewModel可以共存：

```csharp
// 旧ViewModel继续使用旧基类
public class OldHerbsViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>
{
    // 继续工作
}

// 新ViewModel使用组合模式
public class HerbsViewModel : BindableBase, INavigationAware
{
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _services;
    // 新架构
}

// DI配置可以按需切换
// services.AddTransient<HerbsViewModel>(); // 使用新版
// services.AddTransient<OldHerbsViewModel>(); // 使用旧版
```

---

## 5. Risk Assessment（风险评估）

### 5.1 风险矩阵

| 风险 | 可能性 | 影响 | 风险等级 | 缓解措施 |
|------|--------|------|----------|----------|
| 功能回归 | 中 | 高 | **高** | 渐进式迁移，每个阶段充分测试 |
| 迁移时间超预期 | 中 | 中 | **中** | 先完成试点，评估实际工作量 |
| 服务设计不合理 | 低 | 中 | **中** | 试点阶段验证，必要时调整接口 |
| 性能下降 | 低 | 中 | **低** | 服务实例复用，避免过多对象创建 |
| 团队学习成本 | 中 | 低 | **低** | 完善文档，提供示例代码 |

### 5.2 回滚策略

如果迁移出现严重问题：

1. **Phase级回滚**：Git回退到Phase开始前的commit
2. **模块级回滚**：恢复该模块的旧ViewModel，DI配置切换回旧版
3. **全局回滚**：如果架构根本不可行，恢复整个旧架构

### 5.3 成功标准

| 标准 | 指标 |
|------|------|
| 功能完整 | 所有现有功能正常工作 |
| 代码量减少 | 单个ViewModel代码量减少30%+ |
| 测试覆盖 | 服务层单元测试覆盖率80%+ |
| 无性能回退 | 页面加载时间无明显增加 |

---

## 6. Alternatives Considered（备选方案）

### 6.1 方案B：接口提取

**方案描述**：保留继承结构，但为每层提取接口

**优点**：
- 风险低
- 改动小
- 向后兼容

**缺点**：
- 未解决继承深度问题
- 未解决职责耦合问题

**结论**：不选择，因为未从根本上解决架构问题

### 6.2 方案C：仅使用CommunityToolkit.Mvvm（不做组合重构）

**方案描述**：仅引入CommunityToolkit.Mvvm替换基类，保留现有继承结构

**优点**：
- 代码量大幅减少（90%样板代码）
- 风险较低，改动较小

**缺点**：
- 未解决4层继承深度问题
- 未解决职责耦合问题
- 可测试性改善有限

**结论**：不选择，因为组合模式能带来更大的架构改善。**已将CommunityToolkit.Mvvm整合到方案A中**，获得双重收益

### 6.3 方案D：渐进式优化

**方案描述**：只拆分ViewModelBase的职责，保留继承

**优点**：
- 风险最低
- 工作量最小

**缺点**：
- 未解决继承深度问题
- 收益有限

**结论**：如果方案A风险过高，可降级为方案D

---

## 7. Timeline（时间线）

**注意**：此重构应在v1.0 Release后进行，不影响当前Pre-Release进度。

| 阶段 | 工作内容 | 预估工时 | 里程碑 |
|------|----------|----------|--------|
| Phase 1 | 基础设施建设 | 2-3天 | 服务层完成 |
| Phase 2 | Auth模块试点 | 1天 | 试点验证 |
| Phase 3 | Herbs模块试点 | 2天 | Master-Detail验证 |
| Phase 4 | 批量迁移 | 5-7天 | 全部迁移完成 |
| Phase 5 | 清理旧代码 | 1天 | 重构完成 |
| **总计** | | **11-14天** | |

---

## 8. Approval（审批）

**提案状态**: 待审批

**审批要求**：
- [ ] 架构设计评审通过
- [ ] 工作量评估确认
- [ ] 迁移计划确认
- [ ] 风险评估确认

**下一步**：用户确认后，创建详细的`tasks.md`和`design.md`

---

## Appendix A: 现有基类功能点完整清单

### ViewModelBase (351行)

| 功能 | 类型 | 说明 |
|------|------|------|
| IsBusy | Property | 繁忙状态 |
| IsLoading | Property | 加载状态 |
| ErrorMessage | Property | 错误消息 |
| StatusMessage | Property | 状态消息 |
| HasErrors | Property | 是否有验证错误 |
| GetErrors | Method | 获取验证错误 |
| AddValidationError | Method | 添加验证错误 |
| ClearValidationErrors | Method | 清除验证错误 |
| ExecuteSafelyAsync | Method | 安全异步执行 |
| SafeExecuteAsync | Method | 带HTTP处理的安全执行 |
| HandleError | Method | 错误处理 |
| GetUserFriendlyMessage | Method | 获取用户友好消息 |
| RunOnUIThread | Method | UI线程执行 |
| Dispose | Method | 资源清理 |

### UnifiedViewModelBase (237行)

| 功能 | 类型 | 说明 |
|------|------|------|
| PageTitle | Property | 页面标题 |
| NavigateToHomeCommand | Command | 返回首页 |
| NavigateTo | Method | 导航到视图 |
| NavigateBack | Method | 返回上一页 |
| NavigateForward | Method | 前进到下一页 |
| OnNavigatedTo | Method | 导航进入处理 |
| OnNavigatedFrom | Method | 导航离开处理 |
| IsNavigationTarget | Method | 是否为导航目标 |
| ProcessNavigationParameters | Method | 处理导航参数 |
| InitializeAsync | Method | 异步初始化 |
| ValidateProperty | Method | 验证属性 |
| ValidateAllProperties | Method | 验证所有属性 |
| ShowSuccessMessageAsync | Method | 显示成功消息 |
| ShowErrorMessageAsync | Method | 显示错误消息 |
| ShowWarningMessageAsync | Method | 显示警告消息 |
| ShowConfirmationAsync | Method | 显示确认对话框 |
| KeepAlive | Property | 是否保持存活 |

### UnifiedListViewModelBase<T> (183行)

| 功能 | 类型 | 说明 |
|------|------|------|
| Items | Property | 列表项集合 |
| SelectedItem | Property | 选中项 |
| SelectedItems | Property | 选中项集合 |
| HasSelection | Property | 是否有选中项 |
| SearchText | Property | 搜索文本 |
| TotalCount | Property | 总记录数 |
| CurrentPage | Property | 当前页码 |
| PageSize | Property | 每页大小 |
| TotalPages | Property | 总页数 |
| PageSizes | Property | 分页大小选项 |
| BusyMessage | Property | 繁忙消息 |
| SearchCommand | Command | 搜索命令 |
| RefreshCommand | Command | 刷新命令 |
| AddCommand | Command | 新增命令 |
| DeleteCommand | Command | 删除命令 |
| BatchDeleteCommand | Command | 批量删除命令 |
| FirstPageCommand | Command | 首页命令 |
| LastPageCommand | Command | 末页命令 |
| PreviousPageCommand | Command | 上一页命令 |
| NextPageCommand | Command | 下一页命令 |
| ClearSearchCommand | Command | 清除搜索命令 |
| LoadPageAsync | Method | 加载分页数据 |
| SearchAsync | Method | 执行搜索 |
| RefreshAsync | Method | 刷新数据 |
| GetItemsAsync | Abstract | 获取分页数据（抽象） |
| OnExecuteAddAsync | Virtual | 执行新增（虚方法） |
| OnExecuteDeleteAsync | Virtual | 执行删除（虚方法） |
| OnExecuteBatchDeleteAsync | Abstract | 执行批量删除（抽象） |

### MasterDetailViewModelBase<TListItem, TDetail> (541行)

| 功能 | 类型 | 说明 |
|------|------|------|
| CurrentDetail | Property | 当前详情 |
| IsEditMode | Property | 是否编辑模式 |
| IsLoadingDetail | Property | 是否加载详情中 |
| HasUnsavedChanges | Property | 是否有未保存更改 |
| EditCommand | Command | 编辑命令 |
| SaveCommand | Command | 保存命令 |
| CancelCommand | Command | 取消命令 |
| DeleteCurrentCommand | Command | 删除当前项命令 |
| LoadDetailAsync | Abstract | 加载详情（抽象） |
| SaveDetailAsync | Abstract | 保存详情（抽象） |
| DeleteDetailAsync | Abstract | 删除详情（抽象） |
| CreateNewDetail | Abstract | 创建新详情（抽象） |
| CloneDetail | Abstract | 克隆详情（抽象） |
| GetDetailId | Abstract | 获取详情ID（抽象） |
| ExecuteEdit | Method | 执行编辑 |
| ExecuteSaveAsync | Method | 执行保存 |
| ExecuteCancel | Method | 执行取消 |
| ExecuteDeleteCurrentAsync | Method | 执行删除当前项 |
| OnExecuteAddAsync | Override | 执行新增 |
| MarkAsModified | Method | 标记为已修改 |

---

## Appendix B: 参考资料

1. **Microsoft MVVM Documentation**: https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm
2. **CommunityToolkit.Mvvm**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm
3. **Prism Library**: https://prismlibrary.com/docs/
4. **Composition over Inheritance**: https://en.wikipedia.org/wiki/Composition_over_inheritance
5. **SOLID Principles**: https://en.wikipedia.org/wiki/SOLID

---

**文档版本**: v1.1 (CommunityToolkit.Mvvm整合)
**最后更新**: 2025-12-25

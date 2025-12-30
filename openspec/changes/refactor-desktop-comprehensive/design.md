# Desktop层综合重构优化 - 架构设计文档

**Change ID**: refactor-desktop-comprehensive
**Version**: 1.0
**Created**: 2025-12-30

---

## 1. 架构设计原则

### 1.1 SOLID原则应用

| 原则 | 应用场景 | 实现方式 |
|------|----------|----------|
| **S**ingle Responsibility | ViewModel拆分 | 每个ViewModel只负责一个视图的UI状态 |
| **O**pen/Closed | 服务扩展 | 通过接口扩展，不修改现有实现 |
| **L**iskov Substitution | 基类设计 | 子类可完全替代基类使用 |
| **I**nterface Segregation | 服务接口 | 8个小接口而非1个大接口 |
| **D**ependency Inversion | DI容器 | 依赖接口而非具体实现 |

### 1.2 设计模式应用

| 模式 | 应用场景 | 说明 |
|------|----------|------|
| **组合模式** | ViewModel | 组合多个小服务而非继承大基类 |
| **策略模式** | 验证逻辑 | 不同实体不同验证策略 |
| **状态模式** | MedicalCase | 病历状态机管理 |
| **中介者模式** | EventAggregator | 模块间解耦通信 |
| **仓储模式** | 数据访问 | Repository封装API调用 |

---

## 2. 分层架构设计

### 2.1 层次依赖规则

```
┌─────────────────────────────────────────┐
│              Presentation               │
│  (Views, ViewModels, Controls)          │
├─────────────────────────────────────────┤
│              Application                │
│  (Services, Handlers, Coordinators)     │
├─────────────────────────────────────────┤
│               Domain                    │
│  (Models, Interfaces, Events)           │
├─────────────────────────────────────────┤
│            Infrastructure               │
│  (Repositories, API Clients, Caching)   │
└─────────────────────────────────────────┘

依赖方向: 上层 → 下层 (单向)
禁止: 下层 → 上层
```

### 2.2 模块间通信规则

```
┌──────────┐     EventAggregator     ┌──────────┐
│ Module A │ ◄─────────────────────► │ Module B │
└──────────┘                         └──────────┘
     │                                     │
     └────── Shared.Models (DTOs) ─────────┘

规则:
1. 模块间不直接引用
2. 通过EventAggregator发布/订阅事件
3. 共享数据通过Shared.Models
```

### 2.3 聚合根设计 (MedicalCase)

```
MedicalCase (聚合根)
    │
    ├── Patient (引用，非包含)
    │   └── 通过PatientId关联
    │
    ├── Consultation (值对象，包含)
    │   ├── ChiefComplaint
    │   ├── Diagnosis
    │   └── TreatmentPlan
    │
    └── Prescription (实体，包含)
        └── PrescriptionItems[]
            ├── HerbId
            ├── Dosage
            └── Unit

边界规则:
- MedicalCase负责自身及Consultation、Prescription的一致性
- Patient通过ID引用，不负责Patient的生命周期
- 所有对子实体的操作必须通过聚合根
```

---

## 3. 服务接口设计

### 3.1 IMasterDetailServices聚合接口

```csharp
/// <summary>
/// Master-Detail视图标准服务聚合接口
/// </summary>
public interface IMasterDetailServices<TListDto, TDetailDto>
{
    ILoadingStateManager Loading { get; }
    IPaginationService<TListDto> Pagination { get; }
    ISearchService Search { get; }
    ISelectionService<TListDto> Selection { get; }
    IDetailEditorService<TDetailDto> Editor { get; }
    IDialogManager Dialogs { get; }
    IViewNavigationService Navigation { get; }
    IErrorHandler Errors { get; }
}
```

### 3.2 各服务接口定义

#### ILoadingStateManager
```csharp
public interface ILoadingStateManager
{
    bool IsBusy { get; }
    string? BusyMessage { get; }
    
    IDisposable BeginLoading(string? message = null);
    void SetBusy(bool isBusy, string? message = null);
}
```

#### IPaginationService
```csharp
public interface IPaginationService<TItem>
{
    int CurrentPage { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
    
    ObservableCollection<TItem> Items { get; }
    
    Task LoadPageAsync(int page);
    Task RefreshAsync();
    Task NextPageAsync();
    Task PreviousPageAsync();
}
```

#### ISearchService
```csharp
public interface ISearchService
{
    string? SearchKeyword { get; set; }
    bool IsSearching { get; }
    
    Task SearchAsync(string? keyword);
    void ClearSearch();
}
```

#### ISelectionService
```csharp
public interface ISelectionService<TItem>
{
    TItem? SelectedItem { get; set; }
    IReadOnlyList<TItem> SelectedItems { get; }
    bool HasSelection { get; }
    
    event EventHandler<TItem?>? SelectionChanged;
    
    void Select(TItem? item);
    void SelectMultiple(IEnumerable<TItem> items);
    void ClearSelection();
}
```

#### IDetailEditorService
```csharp
public interface IDetailEditorService<TDetail>
{
    TDetail? CurrentDetail { get; }
    bool IsEditing { get; }
    bool IsDirty { get; }
    bool HasErrors { get; }
    
    Task<bool> LoadDetailAsync(Guid id);
    Task<bool> CreateNewAsync();
    Task<CommandResult<TDetail>> SaveAsync();
    Task<bool> DeleteAsync(Guid id);
    void CancelEdit();
}
```

#### IDialogManager
```csharp
public interface IDialogManager
{
    Task<bool> ConfirmAsync(string message, string title = "确认");
    Task ShowErrorAsync(string message, string title = "错误");
    Task ShowWarningAsync(string message, string title = "警告");
    Task ShowInfoAsync(string message, string title = "提示");
    Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDialogParameters? parameters = null);
}
```

#### IViewNavigationService
```csharp
public interface IViewNavigationService
{
    void NavigateToDetail(Guid id);
    void NavigateToCreate();
    void NavigateToList();
    void NavigateBack();
    bool CanNavigateBack { get; }
}
```

#### IErrorHandler
```csharp
public interface IErrorHandler
{
    void HandleException(Exception ex, string? context = null);
    void ShowValidationErrors(IEnumerable<ValidationError> errors);
    void ClearErrors();
}
```

---

## 4. ViewModel组合模式

### 4.1 组合模式结构

```csharp
public class HerbMasterDetailViewModel : ViewModelBase, INavigationAware
{
    // 组合服务而非继承
    private readonly IMasterDetailServices<HerbListDto, HerbDetailDto> _services;
    private readonly IHerbService _herbService;
    
    // 代理属性
    public bool IsBusy => _services.Loading.IsBusy;
    public string? BusyMessage => _services.Loading.BusyMessage;
    public ObservableCollection<HerbListDto> Items => _services.Pagination.Items;
    public HerbListDto? SelectedItem
    {
        get => _services.Selection.SelectedItem;
        set => _services.Selection.Select(value);
    }
    
    // 命令直接委托
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    
    public HerbMasterDetailViewModel(
        IMasterDetailServices<HerbListDto, HerbDetailDto> services,
        IHerbService herbService)
    {
        _services = services;
        _herbService = herbService;
        
        // 命令初始化
        RefreshCommand = new AsyncDelegateCommand(_services.Pagination.RefreshAsync);
        // ...
    }
}
```

### 4.2 服务注册扩展方法

```csharp
public static class ServiceCollectionExtensions
{
    public static IContainerRegistry AddMasterDetailServices<TListDto, TDetailDto>(
        this IContainerRegistry registry)
    {
        // 注册泛型服务
        registry.RegisterScoped<IMasterDetailServices<TListDto, TDetailDto>, 
            MasterDetailServices<TListDto, TDetailDto>>();
        return registry;
    }
}
```

---

## 5. 数据流设计

### 5.1 标准CRUD数据流

```
[View] ──Command──► [ViewModel] ──Call──► [Service] ──Call──► [Repository] ──HTTP──► [API]
                                                                    │
                    ◄──Notify──    ◄──Result──    ◄──Result──      │
                                                                    ▼
                                                              [Server]
```

### 5.2 MedicalCase特殊数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                    MedicalCaseWorkspaceView                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │ PatientCard │  │ Consultation│  │ Prescription│             │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘             │
│         │                │                │                     │
│  ┌──────┴────────────────┴────────────────┴──────┐             │
│  │           MedicalCaseWorkspaceViewModel        │             │
│  │  ┌──────────────────────────────────────────┐ │             │
│  │  │     MedicalCaseWorkspaceCoordinator      │ │             │
│  │  │  ┌─────────┐ ┌─────────┐ ┌────────────┐ │ │             │
│  │  │  │StateMch │ │DataLoader│ │SaveHandler │ │ │             │
│  │  │  └─────────┘ └─────────┘ └────────────┘ │ │             │
│  │  └──────────────────────────────────────────┘ │             │
│  └───────────────────────┬───────────────────────┘             │
└──────────────────────────│──────────────────────────────────────┘
                           │
              ┌────────────┴────────────┐
              │   IMedicalCaseService   │
              └────────────┬────────────┘
                           │
              ┌────────────┴────────────┐
              │ IMedicalCaseRepository  │
              └─────────────────────────┘
```

---

## 6. 状态管理设计

### 6.1 MedicalCase状态机

```
                    ┌─────────┐
                    │  None   │
                    └────┬────┘
                         │ StartConsultation
                         ▼
┌─────────┐         ┌─────────┐
│ Paused  │◄───────►│ Editing │
└─────────┘  Pause/ └────┬────┘
             Resume      │
                         │ Save/SaveDraft
                         ▼
                    ┌─────────┐
                    │ Saving  │
                    └────┬────┘
                         │ Success/Failure
                         ▼
┌─────────┐         ┌─────────┐
│Completed│◄────────│ Saved   │
└─────────┘ Complete└─────────┘
```

### 6.2 EditState统一管理

```csharp
public enum EditState
{
    None,           // 无编辑状态
    Creating,       // 新建模式
    Editing,        // 编辑模式
    Viewing,        // 只读查看
    Saving          // 保存中
}

public interface IEditStateManager
{
    EditState CurrentState { get; }
    bool IsEditing { get; }
    bool IsDirty { get; }
    
    void TransitionTo(EditState newState);
    bool CanTransitionTo(EditState targetState);
}
```

---

## 7. 错误处理设计

### 7.1 错误处理层次

```
┌─────────────────────────────────────────┐
│           View Layer                     │
│  显示用户友好的错误消息                   │
├─────────────────────────────────────────┤
│           ViewModel Layer                │
│  捕获Service异常，转换为UI错误            │
├─────────────────────────────────────────┤
│           Service Layer                  │
│  业务逻辑验证，返回CommandResult          │
├─────────────────────────────────────────┤
│           Repository Layer               │
│  API调用异常，转换为领域异常              │
└─────────────────────────────────────────┘
```

### 7.2 CommandResult标准返回

```csharp
public record CommandResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
    
    public static CommandResult<T> Ok(T data) => 
        new() { Success = true, Data = data };
    
    public static CommandResult<T> Fail(string error) => 
        new() { Success = false, Error = error };
    
    public static CommandResult<T> ValidationFail(IEnumerable<ValidationError> errors) =>
        new() { Success = false, ValidationErrors = errors.ToList() };
}
```

---

## 8. 性能优化设计

### 8.1 延迟加载策略

```csharp
// 列表页只加载ListDto
Task<PagedResult<HerbListDto>> GetPagedAsync(int page, int pageSize);

// 详情页才加载DetailDto
Task<CommandResult<HerbDetailDto>> GetByIdAsync(Guid id);
```

### 8.2 缓存策略

```csharp
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
    void Clear();
}

// Repository层应用缓存
public class HerbRepository : IHerbRepository
{
    private readonly ICacheService _cache;
    
    public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"herb:{id}";
        var cached = _cache.Get<HerbDetailDto>(cacheKey);
        if (cached != null) return cached;
        
        var result = await _apiClient.GetAsync<HerbDetailDto>($"/api/herbs/{id}");
        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        }
        return result;
    }
}
```

---

## 9. 测试策略

### 9.1 单元测试覆盖

| 层次 | 测试重点 | 覆盖率目标 |
|------|----------|------------|
| ViewModel | 命令执行、状态变化 | 80% |
| Service | 业务逻辑、验证规则 | 90% |
| Repository | Mock API响应 | 70% |

### 9.2 集成测试场景

| 场景 | 测试内容 |
|------|----------|
| CRUD流程 | 创建→查询→更新→删除完整流程 |
| 状态转换 | MedicalCase状态机所有路径 |
| 错误处理 | 网络异常、验证失败等场景 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-30 | 1.0 | 初始设计文档 |

# Desktop层架构统一重构 - 详细设计文档

**Change ID**: unify-desktop-architecture
**Version**: 1.0
**Created**: 2025-12-30
**Last Updated**: 2025-12-30

---

## 1. 架构总览

### 1.1 目标架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                 Views (XAML)                                 │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐│
│  │PatientMaster│ │ UserMaster  │ │ HerbMaster  │ │MedicalCaseWorkspaceView ││
│  │ DetailView  │ │ DetailView  │ │ DetailView  │ │  (特殊布局)             ││
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └───────────┬─────────────┘│
└─────────┼───────────────┼───────────────┼───────────────────┼───────────────┘
          │               │               │                   │
          ▼               ▼               ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ViewModels                                      │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │              MasterDetailViewModelBase<TListDto, TDetailModel>          ││
│  │  + IMasterDetailServices (8个服务接口)                                   ││
│  │  + [ObservableProperty] 自动属性                                         ││
│  │  + [RelayCommand] 自动命令                                               ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│         △                 △                 △                   △           │
│         │                 │                 │                   │           │
│  ┌──────┴──────┐   ┌──────┴──────┐   ┌──────┴──────┐   ┌───────┴─────────┐ │
│  │PatientMaster│   │ UserMaster  │   │ HerbMaster  │   │MedicalCaseWork- │ │
│  │DetailVM     │   │ DetailVM    │   │ DetailVM    │   │spaceViewModel   │ │
│  │ (<400行)    │   │ (<400行)    │   │ (<400行)    │   │ + Coordinator   │ │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘   └───────┬─────────┘ │
└─────────┼───────────────┼───────────────────┼───────────────────┼───────────┘
          │               │                   │                   │
          ▼               ▼                   ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CommandHandlers                                    │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐│
│  │IPatient     │ │IUser        │ │IHerb        │ │IMedicalCase             ││
│  │CommandHdlr  │ │CommandHdlr  │ │CommandHdlr  │ │CommandHandler           ││
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └───────────┬─────────────┘│
└─────────┼───────────────┼───────────────┼───────────────────┼───────────────┘
          │               │               │                   │
          ▼               ▼               ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Repositories                                      │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────────────┐│
│  │IPatient     │ │IUser        │ │IHerb        │ │IMedicalCase             ││
│  │Repository   │ │Repository   │ │Repository   │ │Repository               ││
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 模块划分

| 模块 | 项目 | 职责 |
|------|------|------|
| Infrastructure | LYBT.Desktop.Infrastructure | 基类、控件、服务接口 |
| Contracts | LYBT.Desktop.Contracts | 接口定义、DTO |
| Patients | LYBT.Desktop.Patients | 患者管理 |
| Users | LYBT.Desktop.Users | 用户管理 |
| Herbs | LYBT.Desktop.Herbs | 药材管理 |
| Formula | LYBT.Desktop.Formula | 方剂管理 |
| MedicalCase | LYBT.Desktop.MedicalCase | 医案管理 (聚合根) |

---

## 2. 基础设施层设计

### 2.1 NuGet依赖

```xml
<!-- 新增依赖 -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

### 2.2 IMasterDetailServices接口

**文件**: `LYBT.Desktop.Contracts/Services/IMasterDetailServices.cs`

```csharp
namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// MasterDetail模式的服务聚合接口
/// OpenSpec: unify-desktop-architecture
/// </summary>
public interface IMasterDetailServices
{
    /// <summary>加载状态管理</summary>
    ILoadingStateManager LoadingStateManager { get; }
    
    /// <summary>分页服务</summary>
    IPaginationService PaginationService { get; }
    
    /// <summary>搜索服务</summary>
    ISearchService SearchService { get; }
    
    /// <summary>选择服务</summary>
    ISelectionService SelectionService { get; }
    
    /// <summary>详情编辑服务</summary>
    IDetailEditorService DetailEditorService { get; }
    
    /// <summary>对话框管理</summary>
    IDialogManager DialogManager { get; }
    
    /// <summary>视图导航服务</summary>
    IViewNavigationService ViewNavigationService { get; }
    
    /// <summary>错误处理</summary>
    IErrorHandler ErrorHandler { get; }
}

/// <summary>加载状态管理接口</summary>
public interface ILoadingStateManager
{
    bool IsBusy { get; set; }
    string? BusyMessage { get; set; }
    void SetBusy(bool isBusy, string? message = null);
}

/// <summary>分页服务接口</summary>
public interface IPaginationService
{
    int PageSize { get; set; }
    int CurrentPage { get; set; }
    int TotalCount { get; set; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
    Task GoToPageAsync(int page);
}

/// <summary>搜索服务接口</summary>
public interface ISearchService
{
    string? SearchText { get; set; }
    bool IsSearching { get; }
    Task SearchAsync(string? searchText);
    void ClearSearch();
}

/// <summary>选择服务接口</summary>
public interface ISelectionService
{
    object? SelectedItem { get; set; }
    IList SelectedItems { get; }
    bool HasSelection { get; }
    event EventHandler? SelectionChanged;
}

/// <summary>详情编辑服务接口</summary>
public interface IDetailEditorService
{
    bool IsEditing { get; }
    bool HasChanges { get; }
    void BeginEdit();
    void EndEdit();
    void CancelEdit();
}

/// <summary>对话框管理接口</summary>
public interface IDialogManager
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task ShowWarningAsync(string message);
}

/// <summary>视图导航服务接口</summary>
public interface IViewNavigationService
{
    void NavigateTo(string viewName, NavigationParameters? parameters = null);
    void GoBack();
    bool CanGoBack { get; }
}

/// <summary>错误处理接口</summary>
public interface IErrorHandler
{
    void HandleError(Exception ex, string? context = null);
    string GetSafeErrorMessage(Exception ex);
}
```

### 2.3 MasterDetailViewModelBase重构

**文件**: `LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LYBT.Desktop.Infrastructure.ViewModels;

/// <summary>
/// MasterDetail模式基类 (使用CommunityToolkit.Mvvm)
/// OpenSpec: unify-desktop-architecture
/// </summary>
public abstract partial class MasterDetailViewModelBase<TListItem, TDetail> : ObservableObject, 
    INavigationAware, IRegionMemberLifetime, IDisposable
    where TListItem : class
    where TDetail : class
{
    #region 依赖

    protected readonly IMasterDetailServices Services;
    protected readonly ILogger Logger;

    #endregion

    #region 可观察属性 (源生成)

    [ObservableProperty]
    private ObservableCollection<TListItem> _items = new();

    [ObservableProperty]
    private TListItem? _selectedItem;

    [ObservableProperty]
    private TDetail? _selectedDetail;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string? _searchText;

    #endregion

    #region 构造函数

    protected MasterDetailViewModelBase(
        IMasterDetailServices services,
        ILoggerFactory loggerFactory)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Logger = loggerFactory.CreateLogger(GetType());
    }

    #endregion

    #region 抽象方法 (子类必须实现)

    /// <summary>加载列表数据</summary>
    protected abstract Task LoadListAsync();

    /// <summary>加载详情数据</summary>
    protected abstract Task LoadDetailAsync(TListItem item);

    /// <summary>创建新详情对象</summary>
    protected abstract TDetail CreateNewDetail();

    /// <summary>保存详情</summary>
    protected abstract Task<bool> SaveDetailAsync(TDetail detail);

    /// <summary>删除项目</summary>
    protected abstract Task<bool> DeleteItemAsync(TListItem item);

    #endregion

    #region 命令 (源生成)

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            SetBusy(true, "正在加载...");
            await LoadListAsync();
        }
        catch (Exception ex)
        {
            Services.ErrorHandler.HandleError(ex, "刷新列表");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedDetail == null) return;

        try
        {
            SetBusy(true, "正在保存...");
            var success = await SaveDetailAsync(SelectedDetail);
            if (success)
            {
                await Services.DialogManager.ShowSuccessAsync("保存成功");
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            Services.ErrorHandler.HandleError(ex, "保存");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var confirmed = await Services.DialogManager.ShowConfirmAsync(
            "确认删除", "确定要删除选中的项目吗？此操作不可撤销。");
        
        if (!confirmed) return;

        try
        {
            SetBusy(true, "正在删除...");
            var success = await DeleteItemAsync(SelectedItem);
            if (success)
            {
                await Services.DialogManager.ShowSuccessAsync("删除成功");
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            Services.ErrorHandler.HandleError(ex, "删除");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private void CreateNew()
    {
        SelectedDetail = CreateNewDetail();
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SearchAsync(string? text)
    {
        SearchText = text;
        await RefreshAsync();
    }

    #endregion

    #region 属性变更处理

    partial void OnSelectedItemChanged(TListItem? value)
    {
        if (value != null)
        {
            _ = LoadDetailAsync(value);
        }
    }

    #endregion

    #region 辅助方法

    protected void SetBusy(bool isBusy, string? message = null)
    {
        IsBusy = isBusy;
        BusyMessage = message;
    }

    #endregion

    #region INavigationAware

    public virtual void OnNavigatedTo(NavigationContext navigationContext)
    {
        _ = RefreshAsync();
    }

    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

    #endregion

    #region IRegionMemberLifetime

    public virtual bool KeepAlive => true;

    #endregion

    #region IDisposable

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}
```

---

## 3. CommandHandler层设计

### 3.1 统一接口模板

**文件**: `LYBT.Desktop.Contracts/CommandHandlers/ICommandHandlerBase.cs`

```csharp
namespace LYBT.Desktop.Contracts.CommandHandlers;

/// <summary>
/// CommandHandler统一返回类型
/// OpenSpec: unify-desktop-architecture
/// </summary>
public record CommandResult<T>(bool Success, T? Data, string? Error);

/// <summary>
/// CommandHandler基础接口模板
/// </summary>
public interface ICommandHandlerBase<TListDto, TDetailDto, TInputDto>
    where TListDto : class
    where TDetailDto : class
    where TInputDto : class
{
    /// <summary>获取列表</summary>
    Task<CommandResult<List<TListDto>>> GetListAsync(QueryParams? query = null);
    
    /// <summary>获取详情</summary>
    Task<CommandResult<TDetailDto>> GetDetailAsync(Guid id);
    
    /// <summary>保存 (创建/更新)</summary>
    Task<CommandResult<TDetailDto>> SaveAsync(TInputDto input);
    
    /// <summary>删除</summary>
    Task<CommandResult<bool>> DeleteAsync(Guid id);
}

/// <summary>
/// 查询参数
/// </summary>
public record QueryParams
{
    public string? SearchText { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public Dictionary<string, object>? Filters { get; init; }
}
```

### 3.2 具体CommandHandler示例

**文件**: `LYBT.Desktop.Patients/CommandHandlers/PatientCommandHandler.cs`

```csharp
namespace LYBT.Desktop.Patients.CommandHandlers;

/// <summary>
/// 患者CommandHandler实现
/// OpenSpec: unify-desktop-architecture
/// </summary>
public class PatientCommandHandler : IPatientCommandHandler
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientCommandHandler> _logger;

    public PatientCommandHandler(
        IPatientRepository repository,
        ILoggerFactory loggerFactory)
    {
        _repository = repository;
        _logger = loggerFactory.CreateLogger<PatientCommandHandler>();
    }

    public async Task<CommandResult<List<PatientListDto>>> GetListAsync(QueryParams? query = null)
    {
        try
        {
            var result = await _repository.GetListAsync(
                query?.SearchText,
                query?.Page ?? 1,
                query?.PageSize ?? 20);
            
            return new CommandResult<List<PatientListDto>>(true, result.ToList(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return new CommandResult<List<PatientListDto>>(false, null, 
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取列表", ex));
        }
    }

    public async Task<CommandResult<PatientDetailDto>> GetDetailAsync(Guid id)
    {
        try
        {
            var result = await _repository.GetDetailAsync(id);
            if (result == null)
            {
                return new CommandResult<PatientDetailDto>(false, null, "未找到患者信息");
            }
            return new CommandResult<PatientDetailDto>(true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败, Id: {Id}", id);
            return new CommandResult<PatientDetailDto>(false, null,
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取详情", ex));
        }
    }

    public async Task<CommandResult<PatientDetailDto>> SaveAsync(PatientInputDto input)
    {
        try
        {
            var result = await _repository.SaveAsync(input);
            return new CommandResult<PatientDetailDto>(true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存患者失败");
            return new CommandResult<PatientDetailDto>(false, null,
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    public async Task<CommandResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return new CommandResult<bool>(true, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败, Id: {Id}", id);
            return new CommandResult<bool>(false, false,
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex));
        }
    }
}
```

---

## 4. DTO层设计

### 4.1 命名规范

| 类型 | 命名规则 | 用途 | 示例 |
|------|----------|------|------|
| ListDto | `[Entity]ListDto` | 列表展示 | `PatientListDto` |
| DetailDto | `[Entity]DetailDto` | 详情展示 | `PatientDetailDto` |
| InputDto | `[Entity]InputDto` | 创建/更新输入 | `PatientInputDto` |
| QueryDto | `[Entity]QueryDto` | 查询参数 | `PatientQueryDto` |

### 4.2 DTO结构示例

```csharp
// 列表DTO - 精简字段，用于列表展示
public record PatientListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public int? Age { get; init; }
    public string? Phone { get; init; }
    public DateTime CreatedAt { get; init; }
}

// 详情DTO - 完整字段，用于详情展示
public record PatientDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public DateTime? BirthDate { get; init; }
    public int? Age { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalHistory { get; init; }
    public string? Remark { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

// 输入DTO - 可写字段，用于创建/更新
public record PatientInputDto
{
    public Guid? Id { get; init; }  // null表示创建，有值表示更新
    public required string Name { get; init; }
    public string? Gender { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalHistory { get; init; }
    public string? Remark { get; init; }
}
```

---

## 5. MedicalCase特殊设计

### 5.1 聚合根架构

```
MedicalCaseWorkspaceViewModel
    │
    ├── Properties
    │   ├── CurrentMedicalCaseId: Guid
    │   ├── CurrentPatient: PatientDisplayModel
    │   ├── NeedsPrescription: bool
    │   └── EditState: MedicalCaseEditState
    │
    ├── Child ViewModels
    │   ├── ConsultationPanelViewModel (IDataProvider)
    │   └── PrescriptionPanelViewModel (IDataProvider, IValidatable)
    │
    ├── Components
    │   ├── MedicalCaseWorkspaceCoordinator (协调器)
    │   ├── MedicalCaseEditModeStateMachine (状态机)
    │   ├── MedicalCaseDataLoader (数据加载)
    │   └── MedicalCaseNavigationHandler (导航处理)
    │
    └── Commands
        ├── SaveDraftCommand
        ├── CompleteCommand
        ├── CancelCommand
        └── SuspendCommand
```

### 5.2 IDataProvider接口

```csharp
/// <summary>
/// 面板数据提供者接口
/// OpenSpec: unify-desktop-architecture
/// </summary>
public interface IDataProvider
{
    /// <summary>获取诊断数据</summary>
    ConsultationInputDto? GetConsultationData();
    
    /// <summary>获取处方数据</summary>
    PrescriptionInputDto? GetPrescriptionData();
}
```

### 5.3 聚合保存流程

```
用户点击"保存"
    │
    ▼
MedicalCaseWorkspaceViewModel.SaveCommand
    │
    ▼
Coordinator.SaveAsync()
    ├── 1. 从ConsultationPanelVM获取诊断数据
    ├── 2. 从PrescriptionPanelVM获取处方数据 (if NeedsPrescription)
    ├── 3. 构建MedicalCaseInputDto (聚合DTO)
    ├── 4. 调用Repository.SaveAsync() (单次API调用)
    └── 5. 返回结果
    │
    ▼
显示结果提示
```

---

## 6. 可复用控件设计

### 6.1 PatientInfoCardControl

**文件**: `LYBT.Desktop.Infrastructure/Controls/PatientInfoCardControl.xaml`

**属性**:
| 属性 | 类型 | 说明 |
|------|------|------|
| Patient | PatientDisplayModel | 患者数据 |
| DisplayMode | PatientCardDisplayMode | 显示模式 (Compact/Full) |
| ShowActions | bool | 是否显示操作按钮 |

**用法**:
```xaml
<controls:PatientInfoCardControl 
    Patient="{Binding CurrentPatient}"
    DisplayMode="Full"
    ShowActions="True" />
```

### 6.2 PatientSearchControl

**文件**: `LYBT.Desktop.Infrastructure/Controls/PatientSearchControl.xaml`

**属性**:
| 属性 | 类型 | 说明 |
|------|------|------|
| SearchText | string | 搜索文本 |
| SearchCommand | ICommand | 搜索命令 |
| SelectedPatient | PatientListDto | 选中的患者 |
| Patients | IEnumerable | 患者列表 |

### 6.3 PendingQueueControl

**文件**: `LYBT.Desktop.Infrastructure/Controls/PendingQueueControl.xaml`

**属性**:
| 属性 | 类型 | 说明 |
|------|------|------|
| PendingItems | ObservableCollection | 候诊列表 |
| SelectedItem | PendingMedicalCaseDto | 选中项 |
| SelectCommand | ICommand | 选择命令 |
| StatusFilter | PendingCaseStatus? | 状态筛选 |

---

## 7. 错误处理设计

### 7.1 统一错误处理流程

```
异常发生
    │
    ▼
CommandHandler捕获
    ├── 记录日志 (Logger.LogError)
    ├── 转换为友好消息 (ClientErrorMessageMapper)
    └── 返回 CommandResult<T>(false, null, errorMessage)
    │
    ▼
ViewModel接收
    ├── 检查 result.Success
    ├── 如果失败: Services.DialogManager.ShowErrorAsync(result.Error)
    └── 如果成功: 继续业务逻辑
```

### 7.2 ClientErrorMessageMapper扩展

```csharp
public static class ClientErrorMessageMapper
{
    public static string GetSafeOperationFailureMessage(string operation, Exception ex)
    {
        // 根据异常类型返回用户友好消息
        return ex switch
        {
            HttpRequestException => $"{operation}失败：网络连接异常，请检查网络后重试",
            TaskCanceledException => $"{operation}失败：请求超时，请稍后重试",
            ValidationException ve => $"{operation}失败：{ve.Message}",
            _ => $"{operation}失败：{GetGenericErrorMessage(ex)}"
        };
    }

    private static string GetGenericErrorMessage(Exception ex)
    {
        // 生产环境隐藏技术细节
        #if DEBUG
        return ex.Message;
        #else
        return "操作失败，请稍后重试或联系管理员";
        #endif
    }
}
```

---

## 8. 文件变更清单

### 8.1 新增文件

| 文件路径 | 说明 |
|----------|------|
| `Contracts/Services/IMasterDetailServices.cs` | 服务聚合接口 |
| `Contracts/CommandHandlers/ICommandHandlerBase.cs` | CommandHandler基础接口 |
| `Infrastructure/Services/MasterDetailServices.cs` | 服务聚合实现 |
| `Infrastructure/Controls/PatientInfoCardControl.xaml(.cs)` | 患者信息卡片 |
| `Infrastructure/Controls/PatientSearchControl.xaml(.cs)` | 患者搜索控件 |
| `Infrastructure/Controls/PendingQueueControl.xaml(.cs)` | 候诊队列控件 |

### 8.2 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `Infrastructure/ViewModels/MasterDetailViewModelBase.cs` | 重构为CommunityToolkit.Mvvm模式 |
| `Patients/ViewModels/PatientMasterDetailViewModel.cs` | 应用新基类 |
| `Users/ViewModels/UserMasterDetailViewModel.cs` | 应用新基类 |
| `Herbs/ViewModels/HerbMasterDetailViewModel.cs` | 应用新基类 |
| `Formula/ViewModels/FormulaMasterDetailViewModel.cs` | 应用新基类 |
| `MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs` | 瘦身 + Coordinator模式 |
| 各模块的CommandHandler | 统一返回类型 |

### 8.3 删除文件

| 文件路径 | 说明 |
|----------|------|
| `Infrastructure/ViewModels/UnifiedListViewModelBase.cs` | 合并到MasterDetailViewModelBase |
| 重复的控件实现 | 统一到Infrastructure/Controls |

---

## 9. 测试策略

### 9.1 单元测试

| 测试类 | 覆盖范围 |
|--------|----------|
| `CommandHandlerTests` | 所有CommandHandler的CRUD操作 |
| `ViewModelTests` | ViewModel命令和状态变化 |
| `CoordinatorTests` | MedicalCase聚合保存逻辑 |

### 9.2 集成测试

| 测试场景 | 验证内容 |
|----------|----------|
| 患者CRUD流程 | 创建、读取、更新、删除完整流程 |
| 医案完整生命周期 | 创建→诊断→处方→完成 |
| 挂起恢复流程 | 挂起→恢复→继续编辑 |

---

**文档版本**: 1.0
**最后更新**: 2025-12-30

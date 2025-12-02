# Design: refactor-viewmodel-layer

## 架构概述

本设计文档描述ViewModel层重构的架构决策和实现策略。

## 当前架构分析

### 基类继承体系

```
BindableBase (Prism)
    │
    ▼
ViewModelBase (537行)
    ├─ 错误处理: ExecuteSafelyAsync<T>
    ├─ 状态属性: IsLoading, IsBusy, HasError
    ├─ 验证支持: INotifyDataErrorInfo
    └─ 日志和资源管理
    │
    ▼
UnifiedViewModelBase (576行)
    ├─ 导航支持: INavigationAware
    ├─ 异步初始化: InitializeAsync
    ├─ 消息显示: ShowSuccessMessageAsync等
    └─ 会话管理: GetCurrentUserInfo
    │
    ▼
UnifiedListViewModelBase<T> (605行)
    ├─ 列表操作: Items, SelectedItem(s)
    ├─ 分页支持: PageSize, TotalCount
    ├─ 命令集: Search, Refresh, Add, Delete
    └─ 防抖搜索: SearchWithDebounceAsync
```

### Components模式现状

```
已采用Components的模块:
├─ Formula/
│   └─ ViewModels/
│       ├─ Components/
│       │   ├─ FormulaCommandHandler.cs
│       │   ├─ FormulaDataManager.cs
│       │   ├─ FormulaCalculator.cs
│       │   └─ FormulaValidator.cs
│       └─ FormulaDetailViewModel.cs (主VM)
│
├─ Patients/
│   └─ ViewModels/
│       └─ Components/
│           ├─ PatientCommandHandler.cs
│           ├─ PatientDataManager.cs
│           └─ PatientValidator.cs
│
├─ Prescriptions/
│   └─ ViewModels/
│       └─ Components/
│           ├─ PrescriptionCommandHandler.cs
│           ├─ PrescriptionDataManager.cs
│           ├─ PrescriptionCalculator.cs
│           ├─ PrescriptionValidator.cs
│           └─ PrescriptionEventCoordinator.cs

未采用Components的模块:
├─ MedicalCase/ (需要添加)
├─ Herbs/
├─ Users/
└─ Auth/
```

## 重构设计

### Phase 1: viewmodel-conventions规范

创建统一的ViewModel设计规范，作为后续重构的指导。

**规范内容**:
- VM-001: ViewModel大小限制（<500行）
- VM-002: Components分层模式
- VM-003: 命令初始化模式
- VM-004: 错误处理模式
- VM-005: 异步模式一致性
- VM-006: 导航模式
- VM-007: 基类继承规范

### Phase 2: MedicalCase模块Components分层

#### 2.1 拆分MedicalCaseWorkspaceViewModel

**当前结构** (1544行):
```csharp
public class MedicalCaseWorkspaceViewModel : UnifiedViewModelBase
{
    // 患者信息管理
    // 医案数据CRUD
    // 诊断面板协调
    // 处方面板协调
    // 状态管理
    // 事件处理
    // 10+个DelegateCommand
}
```

**重构后结构**:
```
MedicalCase/
└─ ViewModels/
    ├─ Components/
    │   ├─ MedicalCaseCommandHandler.cs    # CRUD操作
    │   ├─ MedicalCaseDataManager.cs       # 数据加载和缓存
    │   ├─ MedicalCaseValidator.cs         # 业务规则验证
    │   └─ MedicalCaseStateManager.cs      # 状态机管理
    ├─ MedicalCaseWorkspaceViewModel.cs    # 协调器 (<400行)
    ├─ ConsultationPanelViewModel.cs       # 保持不变
    └─ PrescriptionPanelViewModel.cs       # 评估是否需要拆分
```

#### 2.2 Components职责划分

**MedicalCaseCommandHandler**:
```csharp
public class MedicalCaseCommandHandler
{
    // 依赖注入
    private readonly IMedicalCaseRepository _repository;
    private readonly IMapper _mapper;

    // CRUD方法
    Task<Result<MedicalCaseDto>> CreateAsync(CreateMedicalCaseRequest request);
    Task<Result<MedicalCaseDto>> UpdateAsync(Guid id, UpdateMedicalCaseRequest request);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
}
```

**MedicalCaseDataManager**:
```csharp
public class MedicalCaseDataManager
{
    // 数据加载
    Task<Result<MedicalCaseDto>> LoadByIdAsync(Guid id);
    Task<Result<IEnumerable<MedicalCaseDto>>> LoadByPatientAsync(Guid patientId);

    // 缓存管理
    void InvalidateCache(Guid id);
    MedicalCaseDto GetFromCache(Guid id);
}
```

**MedicalCaseValidator**:
```csharp
public class MedicalCaseValidator
{
    // 业务规则验证
    ValidationResult ValidateForSave(MedicalCaseDto dto);
    ValidationResult ValidateForClose(MedicalCaseDto dto);
    ValidationResult ValidateStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to);
}
```

**MedicalCaseStateManager**:
```csharp
public class MedicalCaseStateManager
{
    // 状态机管理
    bool CanTransitionTo(MedicalCaseStatus targetStatus);
    void TransitionTo(MedicalCaseStatus targetStatus);
    MedicalCaseStatus CurrentStatus { get; }
}
```

#### 2.3 重构后的MedicalCaseWorkspaceViewModel

```csharp
public class MedicalCaseWorkspaceViewModel : UnifiedViewModelBase
{
    // 组件注入
    private readonly MedicalCaseCommandHandler _commandHandler;
    private readonly MedicalCaseDataManager _dataManager;
    private readonly MedicalCaseValidator _validator;
    private readonly MedicalCaseStateManager _stateManager;

    // 子ViewModel（已分离）
    public ConsultationPanelViewModel ConsultationPanel { get; }
    public PrescriptionPanelViewModel PrescriptionPanel { get; }

    // UI状态属性
    public MedicalCaseDto CurrentCase { get; set; }
    public PatientDto CurrentPatient { get; set; }

    // 协调命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CloseCommand { get; }

    // 协调方法 - 委托给Components
    private async Task OnExecuteSaveAsync()
    {
        var validationResult = _validator.ValidateForSave(CurrentCase);
        if (!validationResult.IsValid) { /* 显示错误 */ return; }

        var result = await _commandHandler.UpdateAsync(CurrentCase.Id, MapToRequest());
        if (result.IsSuccess) { /* 刷新UI */ }
    }
}
```

### Phase 3: 代码模式统一

#### 3.1 命令初始化工厂

**新增CommandFactory辅助类**:
```csharp
// 位置: LYBT.Desktop.Foundation/Commands/CommandFactory.cs
public static class CommandFactory
{
    /// <summary>
    /// 创建带加载保护的异步命令
    /// </summary>
    public static DelegateCommand CreateAsyncWithLoadingGuard(
        Func<Task> execute,
        ViewModelBase viewModel)
    {
        return new DelegateCommand(
            async () => await execute(),
            () => !viewModel.IsLoading && !viewModel.IsBusy)
            .ObservesProperty(() => viewModel.IsLoading)
            .ObservesProperty(() => viewModel.IsBusy);
    }

    /// <summary>
    /// 创建带参数的命令
    /// </summary>
    public static DelegateCommand<T> CreateWithParameter<T>(
        Action<T> execute,
        Func<T, bool> canExecute = null) where T : class
    {
        return new DelegateCommand<T>(
            execute,
            canExecute ?? (item => item != null));
    }
}
```

**使用示例**:
```csharp
// 之前 (8行)
AddCommand = new DelegateCommand(
    async () => await OnExecuteAddAsync(),
    () => !IsLoading && !IsBusy)
    .ObservesProperty(() => IsLoading)
    .ObservesProperty(() => IsBusy);

// 之后 (1行)
AddCommand = CommandFactory.CreateAsyncWithLoadingGuard(OnExecuteAddAsync, this);
```

#### 3.2 错误处理扩展

**增强ViewModelBase**:
```csharp
// 位置: ViewModelBase.cs
protected async Task ExecuteWithErrorHandlingAsync(
    Func<Task> operation,
    string operationName,
    string successMessage = null)
{
    try
    {
        IsBusy = true;
        await operation();
        if (!string.IsNullOrEmpty(successMessage))
        {
            await ShowSuccessMessageAsync(successMessage);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "{Operation} 失败", operationName);
        await UserNotificationService?.HandleExceptionAsync(ex, operationName);
    }
    finally
    {
        IsBusy = false;
    }
}
```

**使用示例**:
```csharp
// 之前 (12行)
try {
    IsBusy = true;
    await _repository.DeleteAsync(id);
    await LoadDataAsync();
} catch (Exception ex) {
    Logger.LogError(ex, "删除患者失败");
    await UserNotificationService.HandleExceptionAsync(ex, "删除患者");
} finally {
    IsBusy = false;
}

// 之后 (4行)
await ExecuteWithErrorHandlingAsync(
    async () => { await _repository.DeleteAsync(id); await LoadDataAsync(); },
    "删除患者",
    "删除成功");
```

## 依赖关系

```
viewmodel-conventions spec
    │
    ├── VM-002 Components Pattern
    │       │
    │       └── MedicalCase Components分层
    │               ├── MedicalCaseCommandHandler
    │               ├── MedicalCaseDataManager
    │               ├── MedicalCaseValidator
    │               └── MedicalCaseStateManager
    │
    ├── VM-003 Command Pattern
    │       │
    │       └── CommandFactory辅助类
    │
    └── VM-004 Error Handling
            │
            └── ViewModelBase增强
```

## 风险和缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| MedicalCase功能回归 | 高 | 完整的单元测试覆盖，分步骤重构 |
| Components注入复杂 | 中 | 遵循现有模块的DI注册模式 |
| 命令工厂兼容性 | 低 | 保持现有DelegateCommand用法可选 |

## 测试策略

1. **单元测试**: 每个Component独立测试
2. **集成测试**: MedicalCaseWorkspaceViewModel协调测试
3. **手动测试**: 医案创建、编辑、关闭全流程验证

## 决策记录

### ADR-VM-001: 为什么选择Components模式而非子ViewModel

**决策**: 采用Components分层而非创建更多子ViewModel

**原因**:
1. 其他模块（Formula, Patients, Prescriptions）已采用此模式，保持一致性
2. Components可以复用于不同的ViewModel
3. 避免过深的ViewModel嵌套

**后果**:
- 需要在模块DI注册中添加Components
- 主ViewModel变为协调器角色

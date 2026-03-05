# Composite ViewModel Pattern - Framework Design

> Date: 2026-03-05
> Scope: 全局 ViewModel 组合模式设计，以 MedicalCaseWorkspaceViewModel 为首个实施对象
> Status: Draft

---

## 1. Problem Statement

### 1.1 Current Architecture Issues

`MedicalCaseWorkspaceViewModel` (1099 行) 是项目中最大的 ViewModel，但行数只是表象。根本问题是**设计模式层面的缺陷**:

| 问题 | 具体表现 | 影响 |
|------|----------|------|
| Handler 回调接线 | 每个 Handler 需 5-8 个 `Action/Func` 回调属性，构造函数 80+ 行手动接线 | 脆弱、不可发现、难测试 |
| 编辑状态机内联 | 之前独立提取，又被合并回 VM (~90 行)，缺乏清晰边界 | 职责混乱 |
| VM 承担数据映射 | `InitializeChildViewModels()` 手动逐字段复制 20+ DTO 属性 | 违反 SRP |
| 属性变更链式传播 | `RaiseEditStateProperties()` 手动通知 10+ 计算属性 | 容易遗漏、维护成本高 |
| Service 职责过重 | `MedicalCaseService` (740 行) 混合缓存/变更检测/保存/生命周期 | 难以独立测试 |
| 领域边界模糊 | 打印、导入、清空等操作命名和归属停留在 Prescription 层 | 违反聚合根原则 |

### 1.2 Design Goals

1. **建立全局可复用的 ViewModel 组合模式** - 不只是减少行数
2. **以 DDD 聚合根为中心重组操作** - MedicalCase 是唯一聚合根
3. **消除回调接线** - 用类型安全的接口替代 Action/Func 属性
4. **不可变状态管理** - record + with 表达式消除手动 PropertyChanged 链
5. **子 VM 可独立测试** - 每个子 VM 只依赖接口，不依赖父 VM 具体类型

---

## 2. Architecture Design

### 2.1 Composite ViewModel Structure

```
MedicalCaseWorkspaceViewModel (瘦壳 ~200行)
  |  职责: 导航生命周期 + 子 VM 组合 + WorkspaceState 管理
  |  实现: IWorkspaceContext + IWorkspaceHost
  |
  +-- WorkspaceState (record, 不可变)
  |     EditState, EditType, WorkspaceMode, CanEdit
  |     + 所有计算属性 (IsEditing, ShowButtons, HeaderTitle...)
  |
  +-- ConsultationEditorViewModel (~150行)
  |     职责: 诊断数据录入 (四诊字段绑定 + 验证)
  |     属性: PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
  |     接口: IDataProvider (GetConsultationData)
  |     依赖: IWorkspaceContext
  |
  +-- PrescriptionEditorViewModel (~200行)
  |     职责: 处方数据录入 (药材列表 + 剂量 + 用法 + 价格计算)
  |     属性: Items, DosageCount, Usage, TotalPrice, AllHerbs
  |     接口: IDataProvider (GetPrescriptionData)
  |     依赖: IWorkspaceContext
  |
  +-- MedicalCaseCommandsViewModel (~300行)
  |     职责: 聚合根级操作 (全部以医案为中心)
  |       - 保存 (AggregateSave)
  |       - 挂起 (Suspend)
  |       - 完成 (Complete + 验证规则)
  |       - 打印 (医案处方笺, 原 PrescriptionPrintHandler)
  |       - 导入验方 (原 PrescriptionImportHandler.OpenFormulaImportDialog)
  |       - 历史复制 (原 PrescriptionImportHandler.OpenHistoryCopyDialog)
  |       - 清空处方 (原 PrescriptionImportHandler.ClearHerbItems)
  |     命令: SaveCommand, SuspendCommand, CompleteCommand, PrintCommand,
  |           ImportFormulaCommand, CopyHistoryCommand, ClearHerbsCommand
  |     依赖: IWorkspaceContext, IWorkspaceHost, IMedicalCaseSaveService,
  |           IMedicalCaseLifecycleService, IPrintService, IDialogService
  |
  +-- PendingQueueViewModel (~200行)
  |     职责: 待诊队列管理 (原 PendingQueueHandler)
  |       - 队列刷新
  |       - 患者切换 (处理 Active/Suspended/新建)
  |       - 挂起冲突处理
  |     属性: PendingQueue, SelectedCase, IsRefreshing, HasNoPendingCases
  |     命令: RefreshCommand, SelectCaseCommand
  |     依赖: IWorkspaceContext, IWorkspaceHost, IPendingQueueManager,
  |           INavigationCoordinator
  |
  +-- CardReaderViewModel (~250行)
        职责: 读卡器集成 (原 CardReaderWorkspaceHandler)
          - 连接管理
          - 自动/手动读卡
          - 患者查找/创建
        属性: IsConnected, IsAutoReadEnabled, IsReading, StatusMessage
        命令: ReadCardCommand, ToggleAutoReadCommand
        依赖: IWorkspaceHost, ICardReaderService, IPatientCardReaderIntegration
```

### 2.2 Communication Contracts

两个接口定义父子通信契约，替代 Handler 的回调属性模式:

```csharp
/// <summary>
/// 子 VM 读取父状态 (只读契约)
/// </summary>
public interface IWorkspaceContext
{
    WorkspaceState State { get; }
    Guid MedicalCaseId { get; }
    PatientDetailDto? CurrentPatient { get; }
    ISessionManager? SessionManager { get; }
}

/// <summary>
/// 子 VM 请求父执行 UI 操作 (操作契约)
/// </summary>
public interface IWorkspaceHost
{
    void SetBusy(bool isBusy, string? message = null);
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<bool> ShowConfirmAsync(string message, string title = "确认");
    Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string title);
    ICommonDialogService? CommonDialogService { get; }
    void NotifyStateChanged();  // 子 VM 通知父: 状态需要重新计算
}
```

父 VM 同时实现两个接口:

```csharp
public class MedicalCaseWorkspaceViewModel : NavigableViewModelBase,
    IWorkspaceContext, IWorkspaceHost
{
    // IWorkspaceContext 实现
    public WorkspaceState State { get; private set; }
    public Guid MedicalCaseId { get; private set; }
    public PatientDetailDto? CurrentPatient { get; private set; }

    // 子 VM 通过构造函数注入
    public ConsultationEditorViewModel ConsultationEditor { get; }
    public PrescriptionEditorViewModel PrescriptionEditor { get; }
    public MedicalCaseCommandsViewModel Commands { get; }
    public PendingQueueViewModel PendingQueue { get; }
    public CardReaderViewModel CardReader { get; }
}
```

### 2.3 State Management - WorkspaceState Record

用不可变 record 替代当前分散的 30+ 属性 + 手动 PropertyChanged 链:

```csharp
public record WorkspaceState(
    EditState EditState = EditState.Editing,
    EditType EditType = EditType.Create,
    WorkspaceMode Mode = WorkspaceMode.Clinical,
    bool CanEdit = false,
    bool IsPrescriptionEnabled = false,
    bool NeedsPrescription = true,
    bool CanComplete = false,
    bool CanPrint = false,
    string EditReason = "",
    string Remark = "")
{
    // 编辑状态计算属性
    public bool IsEditing => EditState == EditState.Editing;
    public bool IsReadOnly => EditState == EditState.ReadOnly;
    public bool IsHistoricalEditMode => EditType == EditType.EditCompleted;

    // 按钮可见性计算属性
    public bool ShowEditButton => IsReadOnly && CanEdit && Mode == WorkspaceMode.Clinical;
    public bool ShowEditButtonTopRight => IsReadOnly && CanEdit && Mode == WorkspaceMode.Management;
    public bool ShowSaveButton => IsEditing && Mode == WorkspaceMode.Management;
    public bool ShowSuspendButton => IsEditing && Mode == WorkspaceMode.Clinical;
    public bool ShowCompleteButton => IsEditing && Mode == WorkspaceMode.Clinical;

    // 显示文本计算属性
    public string HeaderTitle => Mode switch
    {
        WorkspaceMode.Clinical => IsEditing ? "看诊中" : "查看医案",
        WorkspaceMode.Management => IsEditing ? "编辑医案" : "查看医案",
        _ => "看诊中"
    };

    public string BackButtonText => Mode switch
    {
        WorkspaceMode.Clinical => "返回患者选择",
        WorkspaceMode.Management => "返回医案列表",
        _ => "返回"
    };

    // 状态转换方法 (返回新实例)
    public WorkspaceState EnterEditMode()
        => CanEdit ? this with { EditState = EditState.Editing } : this;

    public WorkspaceState EnterReadOnlyMode()
        => this with { EditState = EditState.ReadOnly };

    public WorkspaceState DetermineFromContext(
        WorkspaceMode workspaceMode, bool isCompleted, bool isOwner,
        bool isAdmin, bool preferEditing)
    {
        var canEdit = isAdmin || (isOwner && !isCompleted);
        var editType = isCompleted ? EditType.EditCompleted : EditType.EditSuspended;
        var editState = preferEditing && canEdit ? EditState.Editing : EditState.ReadOnly;
        return this with
        {
            Mode = workspaceMode,
            CanEdit = canEdit,
            EditType = editType,
            EditState = editState
        };
    }
}
```

消除当前 `RaiseEditStateProperties()` 的 10 个 `OnPropertyChanged` + 4 个 `RaiseCanExecuteChanged`，替换为单一 `OnPropertyChanged(nameof(State))`。

### 2.4 Child ViewModel Base Class

基础设施层提供子 VM 基类:

```csharp
/// <summary>
/// 子 ViewModel 基类 - 持有 Context + Host 引用
/// 所有子 VM 继承此类，获得统一的父子通信能力
/// </summary>
public abstract class ChildViewModelBase : ObservableObject, IDisposable
{
    protected IWorkspaceContext Context { get; }
    protected IWorkspaceHost Host { get; }
    protected ILogger Logger { get; }

    protected ChildViewModelBase(
        IWorkspaceContext context,
        IWorkspaceHost host,
        ILoggerFactory loggerFactory)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// 初始化子 VM (数据加载等), 由父 VM 在 OnNavigatedTo 后调用
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose() { }
}
```

### 2.5 DataTemplate View Association

```xml
<!-- MedicalCaseWorkspaceView.xaml -->
<UserControl.Resources>
    <DataTemplate DataType="{x:Type vm:ConsultationEditorViewModel}">
        <controls:ConsultationPanel />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:PrescriptionEditorViewModel}">
        <controls:PrescriptionEditorPanel />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:MedicalCaseCommandsViewModel}">
        <controls:MedicalCaseCommandBar />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:PendingQueueViewModel}">
        <controls:PendingQueuePanel />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:CardReaderViewModel}">
        <controls:CardReaderPanel />
    </DataTemplate>
</UserControl.Resources>

<Grid>
    <ContentControl Content="{Binding ConsultationEditor}" Grid.Column="0" />
    <ContentControl Content="{Binding PrescriptionEditor}" Grid.Column="1" />
    <ContentControl Content="{Binding Commands}" Grid.Row="1" />
    <ContentControl Content="{Binding PendingQueue}" />
</Grid>
```

---

## 3. Service Layer Refactoring

### 3.1 Current State

`MedicalCaseService` (740 行) 混合 4 种职责:
- 数据加载 + 缓存管理
- 变更检测 (深拷贝 + 字段逐一比较)
- 聚合保存 (Save + SaveAndSuspend + SaveAndComplete + SaveAndCancel)
- 生命周期管理 (Create + Resume + Suspend + Cancel + Complete)

### 3.2 SRP Split

```
IMedicalCaseService (当前: 740行 单一实现)
  |
  +-- IMedicalCaseDataStore (~200行)
  |     职责: 数据加载 + 缓存管理
  |     方法: LoadDetailsAsync, ClearCache
  |     属性: CachedMedicalCase, CachedConsultation, CachedPrescription
  |     生命周期: Scoped (per workspace navigation)
  |
  +-- IMedicalCaseSaveService (~200行)
  |     职责: 聚合保存操作
  |     方法: AggregateSaveAsync, SaveAndSuspendAsync, SaveAndCompleteAsync, SaveAndCancelAsync
  |     依赖: IMedicalCaseRepository, IMedicalCaseDataStore
  |
  +-- IMedicalCaseLifecycleService (~150行, 已有接口)
  |     职责: 状态转换
  |     方法: CreateMedicalCaseAsync, ResumeSuspendedAsync, CompleteMedicalCaseAsync
  |     依赖: IMedicalCaseRepository
  |
  +-- MedicalCaseChangeTracker (~100行, 新增)
        职责: 变更检测 (独立关注点)
        方法: SetBaseline(snapshot), HasChanges(current)
        依赖: MedicalCaseCloneMapper
```

### 3.3 Change Tracker

```csharp
public class MedicalCaseChangeTracker
{
    private readonly MedicalCaseCloneMapper _cloneMapper = new();
    private MedicalCaseDetailDto? _baseline;

    public void SetBaseline(MedicalCaseDetailDto snapshot)
        => _baseline = _cloneMapper.Clone(snapshot);

    public bool HasChanges(MedicalCaseDetailDto current)
    {
        if (_baseline == null || current == null) return false;
        return IsMedicalCaseChanged(_baseline, current)
            || IsConsultationChanged(_baseline.Consultation, current.Consultation)
            || IsPrescriptionChanged(_baseline.Prescription, current.Prescription);
    }

    public void ClearBaseline() => _baseline = null;

    // 现有的字段比较逻辑从 MedicalCaseService 迁移到此处
    private static bool IsMedicalCaseChanged(...) { ... }
    private static bool IsConsultationChanged(...) { ... }
    private static bool IsPrescriptionChanged(...) { ... }
}
```

---

## 4. Domain Alignment - Aggregate Root Operations

### 4.1 Operation Reclassification

所有外部操作以 MedicalCase 聚合根为中心重新归属:

| 操作 | 原归属 | 新归属 | 理由 |
|------|--------|--------|------|
| 打印处方笺 | PrescriptionPrintHandler | MedicalCaseCommandsVM | 打印内容包含患者+诊断+处方，是医案级产出 |
| 导入验方 | PrescriptionImportHandler | MedicalCaseCommandsVM | 导入影响聚合根的处方部分 |
| 历史复制 | PrescriptionImportHandler | MedicalCaseCommandsVM | 复制来源信息影响聚合根 |
| 清空药材 | PrescriptionImportHandler | MedicalCaseCommandsVM | 清空是对聚合根处方部分的操作 |
| 保存 | VM.ExecuteSave | MedicalCaseCommandsVM | 聚合保存(诊断+处方) |
| 挂起 | VM.ExecuteSuspend | MedicalCaseCommandsVM | 生命周期操作 |
| 完成 | VM.ExecuteComplete | MedicalCaseCommandsVM | 生命周期操作 + 验证规则 |
| 进入编辑 | VM.ExecuteEnterEditMode | MedicalCaseCommandsVM | 状态转换 |

### 4.2 Naming Alignment

| 原命名 | 新命名 | 理由 |
|--------|--------|------|
| PrescriptionPrintHandler | (删除, 逻辑归入 MedicalCaseCommandsVM) | 打印是医案操作 |
| PrescriptionImportHandler | (删除, 逻辑归入 MedicalCaseCommandsVM) | 导入是医案操作 |
| CanPrintPrescription | State.CanPrint | 打印对象是医案处方笺 |
| HasUnsavedPrescriptionChanges | (由 ChangeTracker 计算) | 变更检测是聚合根级 |

---

## 5. File Changes

### 5.1 New Files

```
LYBT.Desktop.Infrastructure/
  +-- ViewModels/Composition/
      +-- IWorkspaceContext.cs
      +-- IWorkspaceHost.cs
      +-- ChildViewModelBase.cs

LYBT.Desktop.MedicalCase/
  +-- Models/
  |   +-- WorkspaceState.cs            (不可变状态 record, 重写)
  +-- Services/
  |   +-- MedicalCaseDataStore.cs      (从 MedicalCaseService 拆出)
  |   +-- MedicalCaseSaveService.cs    (从 MedicalCaseService 拆出)
  |   +-- MedicalCaseChangeTracker.cs  (从 MedicalCaseService 拆出)
  +-- ViewModels/Workspace/
      +-- ConsultationEditorViewModel.cs
      +-- PrescriptionEditorViewModel.cs
      +-- MedicalCaseCommandsViewModel.cs

LYBT.Desktop.Clinical/
  +-- ViewModels/Workspace/
      +-- PendingQueueViewModel.cs     (从 Handler 升级)
      +-- CardReaderViewModel.cs       (从 Handler 升级)
```

### 5.2 Deleted Files

```
LYBT.Desktop.Clinical/Handlers/
  - PendingQueueHandler.cs             (升级为 PendingQueueViewModel)
  - PrescriptionImportHandler.cs       (逻辑归入 MedicalCaseCommandsVM)
  - CardReaderWorkspaceHandler.cs      (升级为 CardReaderViewModel)

LYBT.Desktop.MedicalCase/ViewModels/Components/
  - PrescriptionPrintHandler.cs        (逻辑归入 MedicalCaseCommandsVM)
```

### 5.3 Modified Files

```
MedicalCaseWorkspaceViewModel.cs       (重写为 ~200行 瘦壳)
MedicalCaseModule.cs                   (DI 注册变更)
ClinicalModule.cs                      (DI 注册变更)
MedicalCaseWorkspaceView.xaml          (DataTemplate + ContentControl)
```

### 5.4 DI Registration

```csharp
// MedicalCaseModule.cs
containerRegistry.RegisterScoped<IMedicalCaseDataStore, MedicalCaseDataStore>();
containerRegistry.Register<IMedicalCaseSaveService, MedicalCaseSaveService>();
containerRegistry.Register<IMedicalCaseLifecycleService, MedicalCaseLifecycleService>();
containerRegistry.Register<MedicalCaseChangeTracker>();
containerRegistry.Register<ConsultationEditorViewModel>();
containerRegistry.Register<PrescriptionEditorViewModel>();
containerRegistry.Register<MedicalCaseCommandsViewModel>();

// ClinicalModule.cs
containerRegistry.Register<PendingQueueViewModel>();
containerRegistry.Register<CardReaderViewModel>();

// IWorkspaceContext/IWorkspaceHost 不注册到容器
// 由父 VM 在构造时传入 this
```

---

## 6. Data Flow

### 6.1 Initialization (OnNavigatedTo)

```
Parent.OnNavigatedTo(context)
  +-- 1. 解析导航参数 -> MedicalCaseId, CurrentPatient, WorkspaceMode
  +-- 2. State = new WorkspaceState().DetermineFromContext(...)
  +-- 3. DataStore.LoadDetailsAsync(MedicalCaseId)
  +-- 4. ConsultationEditor.InitializeAsync()   // Mapper.ToItem(cachedDto)
  +-- 5. PrescriptionEditor.InitializeAsync()   // Mapper.ToItem(cachedDto)
  +-- 6. Commands.InitializeAsync()             // 计算 CanComplete, CanPrint
  +-- 7. PendingQueue.InitializeAsync()         // fire-and-forget
  +-- 8. CardReader.InitializeAsync()           // fire-and-forget
```

### 6.2 Save (Aggregate Root Operation)

```
Commands.SaveCommand.Execute()
  +-- 1. Host.SetBusy(true, "正在保存...")
  +-- 2. consultation = ConsultationEditor.GetConsultationData()
  +-- 3. prescription = PrescriptionEditor.GetPrescriptionData()
  +-- 4. SaveService.AggregateSaveAsync(id, consultation, prescription, remark)
  +-- 5. Success -> Host.ShowSuccessAsync("保存成功")
  +-- 6. Host.SetBusy(false)
```

### 6.3 Child-to-Parent State Notification

```
PrescriptionEditor: Items.CollectionChanged
  +-- Host.NotifyStateChanged()
       +-- Parent.UpdateState()
            State = State with {
                CanComplete = CalculateCanComplete(),
                CanPrint = PrescriptionEditor.HasItems
            };
            OnPropertyChanged(nameof(State));
```

---

## 7. Global Pattern Applicability

### 7.1 Reusable Elements

| Element | Location | Scope |
|---------|----------|-------|
| IWorkspaceContext | Infrastructure | All composite VMs |
| IWorkspaceHost | Infrastructure | All composite VMs |
| ChildViewModelBase | Infrastructure | All child VMs |
| record State | Per module | Each complex VM defines own State |
| ChangeTracker | Per module (genericizable later) | All edit-capable modules |
| DataTemplate association | XAML convention | All ContentControl bindings |

### 7.2 Future Candidates

| ViewModel | Lines | Potential Child VMs |
|-----------|-------|---------------------|
| MainWindowViewModel | 745 | MenuVM, NavigationVM, HealthCheckVM |
| PatientSelectionViewModel | 429 | PatientListVM, PatientDetailVM |
| SyncViewModel | 569 | SyncProgressVM, SyncConfigVM |
| LoginViewModel | 448 | CredentialsVM, ModeSelectionVM |

---

## 8. Migration Strategy

Strangler Pattern - 分步替换，每步可验证:

| Phase | Content | Verification |
|-------|---------|-------------|
| 1 | Infrastructure: IWorkspaceContext + IWorkspaceHost + ChildViewModelBase | 编译通过 |
| 2 | WorkspaceState record (替换内联状态机) | 编译 + UI 状态切换正常 |
| 3 | Service 拆分: DataStore + SaveService + Lifecycle + ChangeTracker | 编译 + 全量测试 |
| 4 | ConsultationEditorVM + PrescriptionEditorVM | 编译 + 数据绑定正常 |
| 5 | MedicalCaseCommandsVM (合并 Print + Import + 保存/挂起/完成) | 编译 + 全量测试 |
| 6 | PendingQueueVM + CardReaderVM (Handler -> VM) | 编译 + 功能验证 |
| 7 | 父 VM 瘦身 (删除旧代码，重写为组合壳) | 编译 + 全量测试 |
| 8 | XAML 适配 (DataTemplate + ContentControl) | UI 完整验证 |
| 9 | 测试 + 文档更新 | 全量测试 + CLAUDE.md 更新 |

---

## 9. Design Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| VM 组合模式 | Child ViewModel Composition | 类型安全、IDE 可追踪、可独立测试 |
| 状态管理 | C# record + with 表达式 | 零外部依赖、不可变、消除 PropertyChanged 链 |
| 通信契约 | IWorkspaceContext + IWorkspaceHost | 替代 Action/Func 回调、编译时检查 |
| View 关联 | DataTemplate 自动匹配 | 当前 XAML 已有独立 Panel 控件，改动最小 |
| Service 拆分 | DataStore + SaveService + Lifecycle + ChangeTracker | SRP，每个可独立测试 |
| 领域对齐 | 所有操作提升到聚合根 | DDD 原则，MedicalCase 是唯一聚合根 |
| 迁移策略 | Strangler Pattern (9 Phases) | 渐进替换，每步可验证 |
| 不引入新依赖 | 不用 Stateless / ReactiveUI | YAGNI，当前状态逻辑足够简单 |

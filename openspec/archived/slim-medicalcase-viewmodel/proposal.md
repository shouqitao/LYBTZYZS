# OpenSpec Proposal: slim-medicalcase-viewmodel

**Change ID**: slim-medicalcase-viewmodel
**Status**: archived
**Created**: 2025-12-30
**Completed**: 2025-12-30
**Archived**: 2026-01-05
**Author**: Claude Code

---

## 1. 问题陈述

### 1.1 核心问题

MedicalCase模块存在以下架构问题：

1. **ViewModel过大**: `MedicalCaseWorkspaceViewModel` 达到1229行，超出推荐的400行上限
2. **多模式并存**: Services/ 和 ViewModels/Components/ 两个目录存在职责重叠
3. **死代码残留**: `MedicalCaseUICoordinator.cs` 完全未被使用
4. **命名混乱**: 存在两个Coordinator、两个DataLoader，边界不清

### 1.2 影响范围

| 影响项 | 具体表现 |
|--------|----------|
| 可读性 | 1229行代码难以理解和维护 |
| 可维护性 | 多模式并存导致新代码放置位置不明确 |
| 新人上手 | 需要理解两套不同的模式 |
| 测试覆盖 | 大型ViewModel难以编写单元测试 |

---

## 2. 当前架构分析

### 2.1 目录结构

```
Services/ (7个类)
├── MedicalCaseUICoordinator.cs   ← [死代码] 未被任何ViewModel使用
├── MedicalCaseDataLoader.cs      ← 加载医案业务数据
├── MedicalCaseLifecycleHandler.cs ← 医案生命周期状态管理
├── MedicalCaseNavigationHandler.cs ← 导航逻辑
├── MedicalCaseService.cs          ← API调用封装
├── MedicalCaseValidator.cs        ← 业务验证
└── AuditRequirementChecker.cs     ← 审计检查

ViewModels/Components/ (8个类)
├── MedicalCaseWorkspaceCoordinator.cs ← 工作区协调器(聚合保存)
├── MedicalCaseEditModeStateMachine.cs ← 编辑状态机
├── PrescriptionDataLoader.cs      ← 加载处方参考数据(药材/验方)
├── PrescriptionItemHandler.cs     ← 处方项CRUD操作
├── PrescriptionImportHandler.cs   ← 验方/历史导入
├── PrescriptionSaveHandler.cs     ← 处方保存
├── PrescriptionCalculator.cs      ← 金额计算
└── PrescriptionValidator.cs       ← 处方验证
```

### 2.2 依赖注入分析

**MedicalCaseWorkspaceViewModel构造函数** (14+依赖):
```csharp
public MedicalCaseWorkspaceViewModel(
    MedicalCaseService dataManager,
    MedicalCaseLifecycleHandler lifecycleHandler,
    MedicalCaseDataLoader dataLoader,
    MedicalCaseWorkspaceCoordinator coordinator,
    MedicalCaseNavigationHandler navigationHandler,
    MedicalCaseEditModeStateMachine editModeStateMachine,
    IPendingQueueManager pendingQueueManager,
    IPrescriptionPrintService prescriptionPrintService,
    ConsultationPanelViewModel consultationPanelViewModel,
    PrescriptionPanelViewModel prescriptionPanelViewModel,
    IActiveConsultationService activeConsultationService,
    IDialogService dialogService,
    IAuditRequirementChecker? auditRequirementChecker,
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager,
    IUserNotificationService? userNotificationService
)
```

### 2.3 ViewModel符号统计

| 类别 | 数量 | 备注 |
|------|------|------|
| Fields | 36+ | 包含依赖注入字段和状态字段 |
| Properties | 50+ | MVVM绑定属性 |
| Methods | 40+ | 命令执行、事件处理、辅助方法 |
| **Total Lines** | **1229** | 目标<400行 |

---

## 3. 解决方案

### 3.1 Phase 1: 删除死代码

**目标**: 删除未使用的 `MedicalCaseUICoordinator.cs`

**文件清单**:
- [ ] 删除 `Services/MedicalCaseUICoordinator.cs`
- [ ] 更新 `MedicalCaseModule.cs` DI注册 (如有)

**验证**: 编译通过即可

### 3.2 Phase 2: 提取打印逻辑

**目标**: 将打印相关逻辑提取到独立Handler

**提取内容**:
- `ExecutePrintPrescription()` 方法 (~40行)
- `BuildPrescriptionDetailDto()` 方法 (~40行)

**新建文件**: `ViewModels/Components/PrescriptionPrintHandler.cs`

```csharp
public class PrescriptionPrintHandler
{
    private readonly IPrescriptionPrintService _printService;
    private readonly ILogger<PrescriptionPrintHandler> _logger;

    public async Task PrintAsync(
        PrescriptionDetailDto prescriptionData,
        PatientDetailDto patientInfo,
        string patientName);

    public PrescriptionDetailDto BuildPrescriptionDetailDto(
        PrescriptionPanelViewModel prescriptionPanel,
        MedicalCaseDetailDto medicalCase,
        string patientName);
}
```

**预期减少**: ~80行

### 3.3 Phase 3: 提取待诊选择逻辑

**目标**: 将待诊队列选择逻辑提取到独立Handler

**提取内容**:
- `ExecuteSelectPendingCaseAsync()` 方法 (~105行)
- `HandleSuspendedCaseAsync()` 方法 (~68行)
- `NavigateToNewMedicalCaseAsync()` 方法 (~38行)
- `NavigateToExistingMedicalCaseAsync()` 方法 (~20行)

**新建文件**: `ViewModels/Components/PendingCaseSelectionHandler.cs`

```csharp
public class PendingCaseSelectionHandler
{
    public async Task<SelectionResult> HandleSelectionAsync(
        PendingMedicalCaseDto selectedCase,
        Guid currentMedicalCaseId,
        bool hasUnsavedChanges);

    public async Task<bool> HandleSuspendedCaseAsync(
        Guid patientId,
        PendingMedicalCaseDto suspendedCase);

    public async Task NavigateToNewMedicalCaseAsync(Guid patientId);
    public async Task NavigateToExistingMedicalCaseAsync(Guid medicalCaseId);
}
```

**预期减少**: ~230行

### 3.4 Phase 4: 提取状态计算逻辑

**目标**: 将状态更新逻辑提取到独立Calculator

**提取内容**:
- `UpdateConsultationStatus()` 方法 (~15行)
- `UpdatePrescriptionStatus()` 方法 (~20行)
- `UpdateCanComplete()` 方法 (~15行)

**新建文件**: `ViewModels/Components/WorkspaceStatusCalculator.cs`

```csharp
public class WorkspaceStatusCalculator
{
    public (string Text, Brush Color) CalculateConsultationStatus(
        ConsultationPanelViewModel panel);

    public (string Text, string Summary, Brush Color, Brush Background) 
        CalculatePrescriptionStatus(
            PrescriptionPanelViewModel panel,
            bool needsPrescription);

    public bool CalculateCanComplete(
        ConsultationPanelViewModel consultation,
        PrescriptionPanelViewModel prescription,
        bool needsPrescription);
}
```

**预期减少**: ~50行

---

## 4. 预期结果

### 4.1 代码行数变化

| ViewModel | Before | After | 减少 |
|-----------|--------|-------|------|
| MedicalCaseWorkspaceViewModel | 1229 | ~870 | ~360 |

### 4.2 新增文件

| 文件 | 行数 | 职责 |
|------|------|------|
| PrescriptionPrintHandler.cs | ~100 | 打印逻辑封装 |
| PendingCaseSelectionHandler.cs | ~280 | 待诊选择逻辑 |
| WorkspaceStatusCalculator.cs | ~80 | 状态计算逻辑 |

### 4.3 架构改进

1. **职责分离**: 每个Handler专注单一职责
2. **可测试性**: 独立Handler更易于单元测试
3. **可复用性**: Handler可被其他ViewModel复用
4. **可读性**: ViewModel只保留协调逻辑

---

## 5. 实施计划

| Phase | 内容 | 风险 | 优先级 |
|-------|------|------|--------|
| 1 | 删除死代码 | 低 | P0 | ✅ 已完成 |
| 2 | 提取打印逻辑 | 低 | P1 | ✅ 已完成 |
| 3 | 提取待诊选择逻辑 | 中 | P1 | ❌ 取消 |
| 4 | 提取状态计算逻辑 | 低 | P2 | 待定 |

### 5.1 Phase 3 取消原因

待诊选择逻辑与ViewModel状态深度耦合（SetIsBusy/MedicalCaseId/IsReadOnly/_regionManager/CommonDialogService），提取需传入大量回调和状态参数，得不偿失。

---

## 6. 验收标准

- [x] `MedicalCaseUICoordinator.cs` 已删除
- [x] `IMedicalCaseUICoordinator.cs` 已删除
- [x] 编译通过 (0 warnings, 0 errors)
- [x] `PrescriptionPrintHandler.cs` 已创建
- [x] DI注册已更新
- [ ] 手动测试: 医案工作区功能正常 (待验证)

---

## 7. 风险与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 提取后功能异常 | 中 | 高 | 每个Phase后进行手动测试 |
| DI注册遗漏 | 低 | 中 | 编译时检查 |
| 循环依赖 | 低 | 中 | 使用接口解耦 |

---

## 8. 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-30 | 1.0 | 初始提案 |
| 2025-12-30 | 1.1 | Phase 1完成: 删除MedicalCaseUICoordinator死代码 |
| 2025-12-30 | 1.2 | Phase 2完成: 创建PrescriptionPrintHandler |
| 2025-12-30 | 1.3 | Phase 3取消: 待诊逻辑与ViewModel深度耦合 |

---

## 9. 实际成果

### 9.1 删除的文件
- `Services/MedicalCaseUICoordinator.cs` (死代码)
- `Interfaces/IMedicalCaseUICoordinator.cs` (死代码接口)

### 9.2 新增的文件
- `ViewModels/Components/PrescriptionPrintHandler.cs` (~150行)

### 9.3 代码行数变化

| 项目 | Before | After | 变化 |
|------|--------|-------|------|
| MedicalCaseWorkspaceViewModel | 1229 | ~1185 | -44行 |
| BuildPrescriptionDetailDto | 39行 | 删除 | -39行 |
| MedicalCaseUICoordinator | 存在 | 删除 | 死代码清理 |
| PrescriptionPrintHandler | 0 | ~150 | +150行(新Handler) |

**净收益**:
- 消除死代码，减少理解成本
- 打印逻辑独立，提高可测试性
- ViewModel职责更清晰

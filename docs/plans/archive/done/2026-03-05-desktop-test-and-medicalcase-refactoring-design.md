# Desktop 测试重构 + 医案模块代码优化设计

**日期**: 2026-03-05
**状态**: 设计阶段
**范围**: Desktop 测试架构优化 + MedicalCase 模块代码简化

---

## 1. 背景与目标

### 问题
Desktop 层 503 个测试一直全绿，但存在两类问题：
1. **40 个假测试**: ViewModels/ 目录下的测试重度 mock，代码改坏测试不会红
2. **医案模块过度设计**: 5 层调用栈、重复变更追踪、1275 行 ViewModel、361 行状态机管 3 个按钮

### 目标
- **测试真实反映代码**: 测试失败 = 代码有问题 或 测试设计不合理
- **化繁为简**: 医案模块从 5 层简化为 3 层，消除人造复杂度
- **保留真实复杂度**: 处方三种录入方式（单味药材/经验方导入/历史导入）是业务需求，不简化

---

## 2. 设计原则

| 原则 | 来源 | 在本项目的应用 |
|------|------|--------------|
| Subcutaneous Testing | Martin Fowler | ViewModel 是 UI 的皮下层，测 ViewModel = 测真实行为 |
| 测试行为不测实现 | Ian Cooper | 断言数据库状态和属性值，不断言 mock 方法调用 |
| Testing Trophy | Kent C. Dodds | Integration 测试 ROI 最高，优先投入 |
| Sociable Tests | Martin Fowler | 经典派：用真实协作者，不隔离内部类 |
| KISS / YAGNI | 通用 | 删除不增加价值的抽象层 |

---

## 3. 测试架构设计

### 3.1 现状诊断

| 分类 | 测试数 | 实际价值 | 决策 |
|------|--------|---------|------|
| PureLogic/ | 241 | 高 (纯逻辑零依赖) | 保留 |
| LocalData/ | 70 | 高 (真实 SQLite) | 保留 |
| EndToEnd/ | 95 | 高 (真实业务流) | 保留 + 补漏 |
| ViewModels/ 状态测试 | ~25 | 中 (测真实状态转换) | 保留，删 Received() |
| ViewModels/ mock 测试 | ~40 | 低 (pass-through mock) | **删除** |

### 3.2 删除清单 (40 个低价值测试)

| 文件 | 测试数 | 删除理由 |
|------|--------|---------|
| `ViewModels/Patients/PatientRepositoryTests.cs` | 14 | mock DataSource 返回 X，断言 Repository 返回 X |
| `ViewModels/Users/UserRepositoryTests.cs` | 17 | 同上 |
| `ViewModels/Admin/AdminHomeViewModelTests.cs` 导航测试 | 4 | 验证 mock NavigateTo 被调用 |
| `ViewModels/Clinical/ClinicalHomeViewModelTests.cs` 导航测试 | 4 | 同上 |
| `ViewModels/Shell/Login/LoginCoordinatorTests.cs` Received() 部分 | ~8 | mock 交互验证 |

**保留**: 构造函数 null guard 测试 (防御性编程)、LoginCoordinator 状态转换测试 (测真实 CurrentState)。

### 3.3 测试覆盖差距与补漏计划

**高优先级** (核心业务流无覆盖):

| 差距 | 补测位置 | 验证方式 |
|------|---------|---------|
| 历史处方导入 + 单味修改 | EndToEnd/Prescription/ | 数据库断言 |
| 医案完整状态流转 (编辑→挂起→继续→完成) | EndToEnd/MedicalCase/ | 状态字段断言 |

**中优先级** (DataSource 层已覆盖，ViewModel 层未覆盖):

| 差距 | 说明 |
|------|------|
| 患者恢复软删除 | DataSource 有覆盖，ViewModel 层 RestoreCommand 未测 |
| 患者批量删除 | DataSource 有覆盖，ViewModel 层未测 |
| 药材状态切换 | DataSource 有覆盖，ViewModel 层未测 |
| 用户密码重置 | 管理员操作未测 |

**低优先级/待定**:

| 差距 | 条件 |
|------|------|
| Sync 模块 | 如已上线，需补完整测试 |
| Excel 导入/导出 | Remote-only 功能，Desktop 测试难覆盖 |
| 身份证读卡 | 硬件依赖，mock 是合理的 |

### 3.4 目标测试结构

```
tests/LYBT.Tests.Desktop/
├── _Infrastructure/           [不动] DesktopFixture + LocalDbContextFixture
├── PureLogic/                 [保留] 241 tests, 纯逻辑零依赖
├── LocalData/                 [保留] 70 tests, 真实 SQLite DataSource
├── EndToEnd/                  [保留+补漏] ~100 tests, 真实业务流
│   ├── BusinessFlow/          全流程 (4 tests)
│   ├── Formula/               验方 CRUD (5 tests)
│   ├── Foundation/            认证/Token/重试 (17 tests)
│   ├── Herbs/                 药材 CRUD (4 tests)
│   ├── LocalMode/             本地模式 (18 tests)
│   ├── MedicalCase/           医案 CRUD + 聚合 (23 tests + 补状态流转)
│   ├── Navigation/            导航流 (4 tests)
│   ├── Patients/              患者 CRUD (6 tests)
│   ├── Prescription/          处方 + 三种录入 (5 tests + 补历史导入)
│   └── Users/                 用户管理 (3 tests)
└── ViewModels/                [清理后] ~15 tests
    └── Shell/Login/           LoginCoordinator 状态转换测试
```

---

## 4. 医案模块代码重构设计

### 4.1 过度设计诊断

| 问题 | 具体表现 | 严重度 |
|------|---------|--------|
| 层级过多 | VM → Coordinator → Service → Repository → DataSource (5层) | 高 |
| 职责重复 | 变更追踪在 Service 和 ViewModel 各一份 | 高 |
| 巨型 ViewModel | 1275 行, 14 个依赖注入 (目标 500 行, 5-7 依赖) | 高 |
| 状态机膨胀 | 361 行, 13 个计算属性控制 3 个按钮显隐 | 中 |
| 死代码 | MasterDetailVM 注入 Coordinator 但未调用 | 中 |
| 接口浪费 | IDataProvider 30 行 2 个方法, 固定 2 个实现 | 低 |

### 4.2 真实复杂度 vs 人造复杂度

**保留 (真实业务驱动)**:
- 医案 = 诊断(Consultation) + 处方(Prescription) 聚合根
- 处方三种录入: 单味药材 / 经验方导入+单味修改 / 历史导入+单味修改
- 医案状态流转: 编辑中 → 挂起 / 完成 / 取消
- ConsultationItem / PrescriptionItem 编辑模型
- DataSource 抽象层 (双模式基石)
- MedicalCaseRepository (数据访问统一入口)

**砍掉 (人造复杂)**:
- MedicalCaseWorkspaceCoordinator → 合并到 ViewModel
- MedicalCaseService → 变更追踪归 ViewModel，数据操作直接调 Repository
- MedicalCaseEditModeStateMachine (361行) → 替换为 ~30 行计算属性
- IDataProvider 接口 → 直接调用 `ConsultationItem.ToInputDto()`
- UnfinishedCaseHandler → 合并到 MedicalCaseStartCoordinator

### 4.3 目标架构

```
之前 (5层):
  ViewModel(1275行) → Coordinator(354行) → Service(612行) → Repository(677行) → DataSource

之后 (3层):
  ViewModel(~400行) → Repository(保留) → DataSource(保留)
```

**MedicalCaseWorkspaceViewModel 重构后** (~400行, ≤7依赖):

```csharp
public class MedicalCaseWorkspaceViewModel : NavigableViewModelBase
{
    // 核心依赖 (≤7)
    private readonly IMedicalCaseRepository _repository;
    private readonly IHerbSearchProvider _herbSearch;
    private readonly IDialogManager _dialogManager;
    private readonly IPrescriptionPrintHandler _printHandler;
    private readonly INavigationCoordinator _navigation;

    // 业务模型 (真实复杂度，保留)
    public ConsultationItem Consultation { get; }
    public PrescriptionItem Prescription { get; }

    // 状态 (替代361行状态机)
    public MedicalCaseStatus Status { get; private set; }
    public bool IsEditing => Status == MedicalCaseStatus.Editing;
    public bool CanEdit => Status != MedicalCaseStatus.Completed;
    public bool ShowSaveButton => IsEditing;
    public bool ShowCompleteButton => IsEditing && Consultation.IsValid;
    public bool ShowSuspendButton => IsEditing;

    // 数据操作 (直接调 Repository，不经过 Service/Coordinator)
    public async Task LoadAsync(Guid caseId) { ... }
    public async Task SaveAsync() { ... }
    public async Task CompleteAsync() { ... }
    public async Task SuspendAsync() { ... }
}
```

**PrescriptionItem 保留三种录入入口**:

```csharp
public class PrescriptionItem : ObservableObject, IValidatable
{
    public ObservableCollection<PrescriptionHerbItem> HerbItems { get; }

    // 方式1: 单味药材添加
    public void AddHerb(HerbDetailDto herb, decimal dosage) { ... }

    // 方式2: 经验方导入 + 可单味修改
    public void ImportFromFormula(FormulaDetailDto formula) { ... }

    // 方式3: 历史处方导入 + 可单味修改
    public void ImportFromHistory(PrescriptionDetailDto history) { ... }

    // 公共: 修改单味药材
    public void UpdateHerbDosage(Guid herbItemId, decimal newDosage) { ... }
    public void RemoveHerb(Guid herbItemId) { ... }

    // 导出为 DTO
    public PrescriptionInputDto ToInputDto() { ... }
}
```

### 4.4 1275 行 ViewModel 拆分方案

| 职责 | 当前位置 | 拆分到 | 预估行数 |
|------|---------|--------|---------|
| 数据加载/保存/状态流转 | MedicalCaseWorkspaceVM | 保留在 ViewModel | ~150 |
| 处方录入(三种方式) | 散落在 VM + Handler | PrescriptionItem (已有) | 保持 |
| 打印预览 | PrescriptionPrintHandler | 保留独立 Handler | 保持 |
| 待诊队列操作 | PendingQueueHandler | 保留独立 Handler (如在用) | 保持 |
| 读卡器集成 | CardReaderWorkspaceHandler | 保留独立 Handler | 保持 |
| 按钮显隐 | EditModeStateMachine(361行) | ViewModel 内计算属性 | ~30 |
| 验方导入对话框 | PrescriptionImportHandler | PrescriptionItem.ImportFromFormula() | 合并 |

---

## 5. 重构后的测试策略

### 5.1 测试与代码的对应关系

代码简化为 3 层后，测试也简化：

```
之前 (mock 每一层):
  ViewModel 测试 mock Service
  Service 测试 mock Repository
  Repository 测试 mock DataSource
  → 每层绿 ≠ 整体能跑

之后 (走真实路径):
  ViewModel → Repository → DataSource(SQLite InMemory)
  → 测试绿 = 代码真的能跑
```

### 5.2 医案测试覆盖

| 业务场景 | 测试方式 | 位置 |
|---------|---------|------|
| 创建医案 + 诊断保存 | DesktopFixture + 真实 SQLite | EndToEnd/MedicalCase/ (已有) |
| 处方 - 单味药材添加 | DesktopFixture + 真实 SQLite | EndToEnd/Prescription/ (已有) |
| 处方 - 经验方导入 | DesktopFixture + 真实 SQLite | EndToEnd/Prescription/ (已有) |
| 处方 - 历史导入 | DesktopFixture + 真实 SQLite | **需补** |
| 医案挂起 → 继续 → 完成 | DesktopFixture + 真实 SQLite | **需补** |
| 按钮显隐逻辑 | 纯属性计算 | PureLogic/ (重构后新增) |
| 聚合根 (共享主键/导航属性) | 真实 SQLite | EndToEnd/MedicalCase/ (已有) |

---

## 6. 实施分期

### Phase A: 砍假测试 (低风险, 独立交付)
**范围**: 删除 40 个低价值 mock 测试，清理 LoginCoordinator
**风险**: 极低，不改任何业务代码
**验证**: 测试数减少，全绿，代码零变更
**预估**: 1-2 小时

### Phase B: 医案代码简化 (核心重构)
**范围**:
1. 删除 MedicalCaseService (变更追踪移到 ViewModel)
2. 删除 MedicalCaseWorkspaceCoordinator (加载/保存直接调 Repository)
3. 替换 EditModeStateMachine → ViewModel 内计算属性
4. 删除 IDataProvider 接口
5. 拆分 1275 行 ViewModel
6. 简化 MedicalCaseStartCoordinator

**风险**: 中等，需要仔细保持现有行为不变
**验证**: 编译通过 + 现有 23 个 MedicalCase E2E 测试全绿
**预估**: 需要单独出详细计划

### Phase C: 补测试 (查漏补缺)
**范围**: 历史处方导入 E2E、医案状态流转 E2E、按钮逻辑 PureLogic
**风险**: 低，只新增测试
**验证**: 新测试覆盖主线业务
**预估**: 2-3 小时

---

## 7. 风险与缓解

| 风险 | 缓解措施 |
|------|---------|
| Phase B 重构破坏现有功能 | 每步 git commit，E2E 测试作为安全网 |
| 删除 Service 层后 CommandResult 统一返回缺失 | Repository 异常由 ViewModel 统一 catch |
| 状态机简化后遗漏边界条件 | PureLogic 测试覆盖所有状态组合 |
| Phase A 删除测试后覆盖率数字下降 | 覆盖率数字不重要，测试信号质量才重要 |

---

## 8. 成功标准

- [ ] 零假测试: 每个测试失败都指向真实问题
- [ ] 医案 ViewModel ≤ 500 行, ≤ 7 个依赖
- [ ] 调用栈 3 层: ViewModel → Repository → DataSource
- [ ] 医案主线业务 100% E2E 覆盖 (含三种处方录入)
- [ ] 全量测试绿灯 (数量可以减少，信号必须可靠)

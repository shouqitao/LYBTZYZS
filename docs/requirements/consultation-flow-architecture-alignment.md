# 医生接诊 + 前台挂号 架构对齐分析

> **版本**: v1.0
> **创建日期**: 2026-04-12
> **状态**: 待评审
> **范围**: 医生角色 (Clinical) + 前台角色 (Receptionist) 的完整业务流程对照

---

## 一、你的设计描述

1. **医生主页**：有很多功能，"开始接诊"是主要业务
2. **开始接诊后**：分两部分 -- ①选择患者，②编写医案
3. **选择患者**：分两大部分 -- ①待诊列表，②医生直接选择患者
4. **前台挂号**：挂号信息更新到医生的待诊列表
5. **前台**：有自己的主页和工作台，主要业务是"挂号"

---

## 二、当前代码现状 vs 设计对照

### 2.1 医生侧

| 设计要求 | 当前代码实现 | 状态 | 说明 |
|----------|-------------|------|------|
| 医生主页有"开始接诊"主入口 | `ClinicalHomeView.xaml` -- 主卡片"开始接诊" | ✅ 已实现 | 按钮绑定 `StartMedicalCaseCommand`，导航到 PatientSelectionView |
| 开始接诊 → 选择患者 | `PatientSelectionView.xaml` -- 三栏布局 | ✅ 已实现 | 左侧:读卡器+待诊队列, 中间:患者搜索, 右侧:患者信息 |
| 选择患者 → 编写医案 | `MedicalCaseWorkspaceViewModel` + `MedicalCaseWorkspaceView` | ✅ 已实现 | Composite VM 架构: ConsultationEditor + PrescriptionEditor + Commands |
| 选择患者来源1: 待诊列表 | `PendingQueueViewModel` (左侧面板) | ✅ 已实现 | 调用 `IPendingQueueManager` 加载当前医生名下待诊医案 |
| 选择患者来源2: 直接选择患者 | `PatientSelectionControl` (中间搜索) | ✅ 已实现 | 支持关键词搜索患者, 选中后点击"开始看诊"按钮 |
| 待诊列表数据来源: 前台挂号 | `PendingQueueManager` 调用 `GetPendingCasesAsync()` | ⚠️ 部分实现 | 查询的是 MedicalCase 的 Active/Suspended 状态医案, 而非 Registration 的 Waiting 状态记录 |

### 2.2 前台侧

| 设计要求 | 当前代码实现 | 状态 | 说明 |
|----------|-------------|------|------|
| 前台有独立主页 | `ReceptionistHomeView.xaml` + `ReceptionistHomeViewModel.cs` | ✅ 已实现 | 包含搜索栏、新建挂号、新建患者、统计卡片 |
| 前台主要业务是"挂号" | `CreateNewRegistrationCommand` 导航到 `RegistrationList` | ✅ 已实现 | 但入口是导航到挂号队列视图, 不是独立的挂号创建页 |
| 挂号后更新到医生待诊列表 | `RegistrationService.StartVisitAsync` | ⚠️ 部分实现 | 医生端待诊列表(PendingQueue)查询的是医案状态, 不是挂号状态, 两条数据流未打通 |

---

## 三、发现的偏差和短缺

### 偏差 1: 待诊列表数据源不一致 (P0)

**问题**: 医生的"待诊列表"当前有两个独立的数据源，但它们之间没有关联：

| 数据源 | 位置 | 数据来源 | 状态字段 |
|--------|------|----------|----------|
| 挂号队列 | `RegistrationListViewModel` | `IRegistrationService.GetQueueAsync()` | `RegistrationStatus.Waiting` |
| 待诊队列 | `PendingQueueManager` | `IMedicalCaseApi.GetPendingCasesAsync()` | `MedicalCaseStatus.Active/Suspended` |

**影响**:
- 前台创建挂号后，Registration 状态 = Waiting
- 但医生的 `PendingQueueManager` 查询的是医案的 Active/Suspended 状态
- **前台新建挂号后，医生在"待诊队列"中看不到新患者**
- 医生必须去"挂号队列"页面才能看到新挂号的患者

**理想流程应该是**:
```
前台新建挂号 → Registration (Waiting) → 医生待诊列表显示 → 医生点击接诊 → Registration → InProgress + MedicalCase (Active)
```

**当前实际流程**:
```
前台新建挂号 → Registration (Waiting) → 医生待诊列表不显示 (查的是医案状态)
医生直接选患者 → 创建 MedicalCase (Active) → 待诊列表才显示
```

### 偏差 2: 前台"新建挂号"缺少独立的挂号创建页 (P1)

**问题**: `ReceptionistHomeViewModel.CreateNewRegistration()` 导航到 `ViewNames.RegistrationList` 并传参 `{ Action: "Create" }`。但 `RegistrationListView` 是一个队列列表视图，不是一个独立的挂号创建表单。

**影响**:
- 前台需要在列表中手动操作才能创建挂号
- 缺少一个一站式的"挂号创建"流程（搜索/创建患者 → 选择医生 → 确认挂号）
- 虽然 `RegistrationCreateDialog` 已存在，但前台主页没有直接打开它

### 偏差 3: "开始接诊"的两条路径 Registration 记录不统一 (P0)

**问题**:
- 路径 A (医生直接选患者): `PatientSelectionViewModel.StartMedicalCaseAsync()` → 仅调用 `CreateMedicalCaseAsync(patientId)`，**不创建 Registration 记录**
- 路径 B (挂号队列接诊): `RegistrationListViewModel.StartVisitAsync()` → 调用 `StartVisitAsync(registrationId)`，**同时创建 Registration + MedicalCase**

**影响**:
- 路径 A 的患者没有 Registration 记录，运营数据缺失
- 与 PRD `registration.md` Section 4.1 "医生直接看诊时系统静默创建 Registration + MedicalCase" 不符
- 前台无法知道医生已经直接接诊了某位患者

### 偏差 4: 前台主页统计卡片数据不准确 (P1)

**问题**: `ReceptionistHomeViewModel.LoadStatisticsAsync()` 调用 `GetPagedAsync(pageSize: 100)` 获取所有挂号记录，然后在前端按状态计数。

**影响**:
- 没有按日期过滤，统计的是最近 100 条记录，不是"今日"数据
- 当记录超过 100 条时统计完全不准确
- 应改为调用按日期范围查询的接口

### 偏差 5: 前台主页缺少"刷新数据"命令实现 (P2)

**问题**: `ReceptionistHomeView.xaml:265` 绑定了 `RefreshDataCommand`，但 `ReceptionistHomeViewModel.cs` 中**没有定义该命令**。

**影响**: 编译时会报绑定错误（CommunityToolkit source generator 不会生成不存在的命令）

### 短缺 1: 前台缺少独立工作台 (P1)

**你的设计**: "前台有自己的对应的主页和工作台"

**当前现状**: 前台只有 `ReceptionistHomeView`（主页），没有独立的工作台视图。挂号操作需要跳转到 `RegistrationList`（挂号队列）页面完成。

**建议**: 前台应有两个视图:
- `ReceptionistHomeView`: 主页（统计 + 快捷入口）
- `ReceptionistWorkspaceView`: 工作台（挂号创建 + 队列管理 + 患者登记一站式操作）

### 短缺 2: 缺少挂号创建后自动刷新医生待诊列表的机制 (P1)

**当前现状**: 前台创建挂号后，医生端的待诊列表不会自动更新。

**建议**:
- 使用 `IEventAggregator` 发布 `RegistrationCreatedEvent`
- 医生端的 `PendingQueueViewModel` 订阅该事件并触发刷新
- 或者通过定期轮刷新实现（REQ-007 已提到）

---

## 四、修正后的完整流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                        前台 (Receptionist)                       │
│                                                                 │
│  ReceptionistHomeView (主页)                                     │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐              │
│  │  新建患者    │ │  新建挂号    │ │  今日统计     │              │
│  │  PatientMgmt│ │  RegCreate  │ │  (真实数据)   │              │
│  └─────────────┘ └─────────────┘ └──────────────┘              │
│                                                                 │
│  新建挂号流程:                                                   │
│  搜索/创建患者 → 选择医生 → 创建 Registration (Waiting)         │
│                      │                                          │
│                      ▼                                          │
│              发布 RegistrationCreatedEvent                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │ EventAggregator
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        医生 (Clinical)                           │
│                                                                 │
│  ClinicalHomeView (主页)                                         │
│  ┌──────────────────────────────────────────┐                   │
│  │          "开始接诊" 主卡片                 │                   │
│  └──────────────────────────────────────────┘                   │
│              │                                                  │
│              ▼                                                  │
│  PatientSelectionView (患者选择)                                 │
│  ┌───────────────┬──────────────────┬──────────────┐           │
│  │ 待诊队列       │  患者搜索/选择    │  患者信息    │           │
│  │ (两个来源)    │                  │              │           │
│  │               │                  │              │           │
│  │ 来源A:        │  来源B:          │              │           │
│  │ Registration  │  MedicalCase     │              │           │
│  │ Waiting记录   │  Active医案      │              │           │
│  │ (前台挂号)    │  (医生直接创建)   │              │           │
│  └───────────────┴──────────────────┴──────────────┘           │
│              │                                                  │
│              ▼                                                  │
│  MedicalCaseWorkspaceView (医案工作台)                           │
│  ┌─────────────────┬──────────────────────────────┐            │
│  │ Consultation    │ Prescription                  │            │
│  │ 诊断填写         │ 处方开具                       │            │
│  └─────────────────┴──────────────────────────────┘            │
│  [挂起] [打印] [完成看诊]                                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 五、修正需求清单

| 编号 | 需求 | 优先级 | 关联文档 |
|------|------|--------|----------|
| **ARCH-001** | 统一待诊列表数据源：合并 Registration Waiting + MedicalCase Active/Suspended | P0 | 本文件 偏差1 |
| **ARCH-002** | 医生直接选患者时静默创建 Registration 记录 | P0 | `start-consultation-optimization.md` REQ-001 |
| **ARCH-003** | 前台主页"新建挂号"直接打开 RegistrationCreateDialog | P1 | 本文件 偏差2 |
| **ARCH-004** | 修复前台主页统计数据（按日期过滤 + 分页） | P1 | 本文件 偏差4 |
| **ARCH-005** | 实现前台主页缺失的 RefreshDataCommand | P2 | 本文件 偏差5 |
| **ARCH-006** | 前台创建独立工作台视图 ReceptionistWorkspaceView | P1 | 本文件 短缺1 |
| **ARCH-007** | 挂号创建后通过 EventAggregator 通知医生端刷新待诊列表 | P1 | 本文件 短缺2 |

---

## 六、总结

### 已实现 ✅

| 组件 | 状态 |
|------|------|
| 医生主页 (ClinicalHomeView) + "开始接诊"入口 | ✅ |
| 患者选择页 (PatientSelectionView) -- 三栏布局 | ✅ |
| 医案工作台 (MedicalCaseWorkspaceView) -- Composite VM | ✅ |
| 医生待诊队列 (PendingQueueViewModel) | ✅ (数据源不完整) |
| 前台主页 (ReceptionistHomeView) | ✅ |
| 挂号模块 (RegistrationModule) -- 队列 + 接诊 | ✅ |
| 角色模块分离 (Clinical / Receptionist / Admin) | ✅ |

### 需修正 ⚠️

| 组件 | 问题 |
|------|------|
| 待诊列表数据源 | Registration 和 MedicalCase 两条数据流未打通 |
| 医生直接选患者 | 缺少 Registration 静默创建 |
| 前台新建挂号 | 缺少独立创建页，入口跳转不直观 |
| 前台统计 | 未按日期过滤，数据不准确 |
| 前台工作台 | 只有主页，缺少独立工作台视图 |
| 跨角色通知 | 挂号创建后不会自动通知医生端刷新 |

---

*文档版本: v1.0 | 创建日期: 2026-04-12 | 状态: 待评审*

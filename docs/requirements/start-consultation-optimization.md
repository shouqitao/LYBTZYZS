# 医生"开始接诊"流程优化需求文档

> **版本**: v1.0
> **创建日期**: 2026-04-12
> **状态**: 待评审
> **范围**: 前端 Clinical 模块 + Registration 模块 + MedicalCase 模块的"开始接诊"入口链路
> **关联文档**:
> - `docs/01-product/06-clinical-workflow.md` (端到端临床工作流)
> - `docs/02-requirements/07-medical-cases.md` (医案管理 PRD)
> - `docs/02-requirements/08-registration.md` (挂号管理 PRD)
> - `docs/03-architecture/medicalcase-workspace-current-state.md` (医案工作台当前状态)
> - `docs/plans/medicalcase-workspace-refactoring-analysis.md` (重构分析)

---

## 一、现状概述

### 1.1 当前"开始接诊"完整链路

"开始接诊"功能涉及 **两条独立入口路径**，最终都汇入同一个医案工作台：

```
路径 A (医生直接模式):
  ClinicalHomeView → "开始接诊" → PatientSelectionView
    → 搜索/选择患者 → "诊断"按钮 → StartMedicalCaseAsync
    → 检查进行中医案 → 创建新医案/打开已有医案
    → MedicalCaseWorkspaceViewModel (Clinical 模式)

路径 B (挂号队列模式):
  RegistrationListView → 选中 Waiting 项 → "接诊"按钮 → StartVisitAsync
    → 调用 StartVisit API (创建 Registration + MedicalCase)
    → 获取患者详情 → 导航
    → MedicalCaseWorkspaceViewModel (Clinical 模式)
```

### 1.2 涉及的核心文件

| 文件 | 路径 | 职责 |
|------|------|------|
| `ClinicalHomeViewModel.cs` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/` | 首页"开始接诊"按钮入口 |
| `ClinicalHomeView.xaml` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/` | 首页 UI |
| `PatientSelectionViewModel.cs` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/` | 患者选择 + 医案创建/打开 |
| `PatientSelectionView.xaml` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/` | 三栏布局：读卡器/队列 | 患者搜索 | 患者信息 |
| `RegistrationListViewModel.cs` | `src/Client/Desktop/Modules/LYBT.Desktop.Registration/ViewModels/` | 挂号队列接诊 |
| `PendingQueueViewModel.cs` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/` | 患者选择页内的待诊队列子 VM |
| `MedicalCaseWorkspaceViewModel.cs` | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/` | 医案工作台 Composite VM |
| `PendingQueueManager.cs` | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/` | 待诊队列数据管理器 |
| `UnfinishedCaseHandler.cs` | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/` | 未完成医案检查处理器 |

### 1.3 数据流架构

```
┌─────────────────────────────────────────────────────────────────┐
│                     ClinicalHomeView                            │
│  "开始接诊" ─── StartMedicalCaseCommand ───► NavigateTo         │
│                    (PatientSelection)                            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                  PatientSelectionView                           │
│  ┌────────────┬─────────────────────┬────────────────────┐     │
│  │ 读卡器     │  PatientSelection   │  患者信息卡         │     │
│  │ 待诊队列   │  Control(搜索)      │                    │     │
│  │ PendingQVM │                     │                    │     │
│  └─────┬──────┴─────────┬───────────┴─────────┬──────────┘     │
│        │                │                      │                │
│  SelectCommand     StartMedicalCaseCmd         │                │
│        │                │                      │                │
│        ▼                ▼                      ▼                │
│  PendingQueueVM    Check unfinished case       │                │
│  .SelectPending    Create/Open MedicalCase     │                │
│        │                │                      │                │
│        └────────────────┼──────────────────────┘                │
│                         │                                       │
│              NavigateTo(MedicalCaseWorkspace)                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              MedicalCaseWorkspaceViewModel                      │
│  ConsultationEditor │ PrescriptionEditor │ Commands VM          │
│  诊断填写            │ 处方开具            │ 挂起/完成/打印       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 二、问题诊断

### 2.1 P0 - 严重问题（影响功能正确性）

| 编号 | 问题 | 严重性 | 位置 | 详细说明 |
|------|------|--------|------|----------|
| **P0-1** | 统计数据显示永远为 0 | 高 | `ClinicalHomeViewModel.cs:279-292` | `LoadTodayStatistics()` 方法硬编码 `TodayConsultationCount = 0; PendingCaseCount = 0;`，注释标记 "FUTURE"。用户每次打开首页看到的今日接诊和待完成医案都是 0，造成误导 |
| **P0-2** | 两条入口路径医案创建 API 不一致 | 高 | `PatientSelectionVM:383` vs `RegistrationListVM:188` | 路径 A 使用 `_medicalCaseService.CreateMedicalCaseAsync(patientId)` 直接创建医案，**不创建 Registration 记录**；路径 B 使用 `_registrationService.StartVisitAsync(registrationId)` 同时创建 Registration + MedicalCase。这导致路径 A 的患者没有 Registration 记录，运营数据缺失，且与 PRD 设计的"医生直接看诊时系统静默创建 Registration"不符 |
| **P0-3** | PendingQueue 切换患者时 PatientDetail 为 null | 高 | `PendingQueueViewModel.cs:322-340` | `GetPatientDetail(patientId)` 仅在 `CurrentPatient.Id == patientId` 时返回详情，否则返回 null。从待诊队列切换非当前选中患者时，`NavigateToNewMedicalCaseAsync` 传给 `MedicalCaseWorkspaceViewModel` 的 `CurrentPatient` 为 null，导致工作台患者信息为空 |

### 2.2 P1 - 中等问题（影响用户体验和代码质量）

| 编号 | 问题 | 严重性 | 位置 | 详细说明 |
|------|------|--------|------|----------|
| **P1-1** | 底部"诊断"按钮文案不一致 | 中 | `PatientSelectionView.xaml:181` | 底部按钮文案为"诊断"，与首页"开始接诊"和 PRD 术语"开始看诊"不一致，用户认知混乱 |
| **P1-2** | BR-001 碰撞处理只有二选一 | 中 | `PatientSelectionVM:326-369`, `PendingQueueVM:181-223` | PRD 要求 BR-001 碰撞处理为三选一（继续看诊/新建医案/取消），当前实现仅为 `ShowConfirmAsync`(Yes/No) 二选一。Yes=继续, No=新建，缺少"取消离开"选项。且 Yes/No 语义不明确，用户不清楚"取消"是取消操作还是取消医案 |
| **P1-3** | 多余的导航命令未清理 | 中 | `ClinicalHomeViewModel.cs:145-208` | `NavigateToHerbLibraryCommand`, `NavigateToFormulaLibraryCommand`, `NavigateToSyncCommand` 等命令按重构计划（`medicalcase-workspace-refactoring-analysis.md` 附录 A.3）应已移除，但代码中仍然存在 |
| **P1-4** | MedicalCaseId 为空时部分路径继续执行 | 中 | `PatientSelectionVM:223-231`, `PendingQueueVM:282-286` | 多处对 `MedicalCaseId.HasValue` 的检查为 false 时只记录日志和弹错，但 `finally` 块中 `SetBusy(false)` 后代码继续执行，没有 `return` 守卫（部分路径已有 return，但不一致） |
| **P1-5** | 患者选择页缺少待诊队列自动刷新 | 中 | `PatientSelectionViewModel.cs:453-461` | `OnNavigatedTo` 只调用一次 `PendingQueue.RefreshQueueAsync()`，没有定时刷新机制。`RegistrationListViewModel` 有 30 秒自动刷新，但 PatientSelection 页面没有。前台新建挂号后，医生在患者选择页看不到新患者 |

### 2.3 P2 - 体验优化（不影响功能但提升体验）

| 编号 | 问题 | 严重性 | 位置 | 详细说明 |
|------|------|--------|------|----------|
| **P2-1** | 患者搜索无分页 | 低 | `PatientSelectionVM:266-268` | `GetPatientsAsync(page: 1, pageSize: 100)` 固定加载前 100 条，患者超过 100 人时无法搜索到后面的患者 |
| **P2-2** | 读卡器区域固定占位 | 低 | `PatientSelectionView.xaml:47` | 左侧 `Width="280"` 固定分配给读卡器+待诊队列，即使没有读卡器硬件也占据空间 |
| **P2-3** | 患者选择页空状态简陋 | 低 | `PatientSelectionView.xaml:118` | 空队列仅显示"暂无待诊患者"文字，无引导操作 |
| **P2-4** | 加载状态无视觉反馈 | 低 | `PatientSelectionView.xaml` 全局 | 患者列表加载期间无 skeleton/loading 指示，仅依赖底部状态文字 |

---

## 三、需求定义

### 3.1 需求总览

| 需求编号 | 标题 | 优先级 | 类型 | 预计工作量 |
|----------|------|--------|------|------------|
| REQ-001 | 统一两条入口路径的医案创建逻辑 | P0 | 功能修复 | 2-3 小时 |
| REQ-002 | 修复待诊队列切换患者时 PatientDetail 为 null | P0 | Bug 修复 | 1 小时 |
| REQ-003 | 统计数据接入真实接口或暂时隐藏 | P0 | 功能修复 | 1-2 小时 |
| REQ-004 | BR-001 碰撞处理改为三选一弹窗 | P1 | 功能补齐 | 2 小时 |
| REQ-005 | 统一按钮文案为"开始看诊" | P1 | UI 优化 | 0.5 小时 |
| REQ-006 | 清理 ClinicalHomeViewModel 多余命令 | P1 | 代码清理 | 1 小时 |
| REQ-007 | 患者选择页增加待诊队列自动刷新 | P1 | 功能补齐 | 1 小时 |
| REQ-008 | MedicalCaseId 为空时统一错误守卫 | P1 | 代码质量 | 1 小时 |
| REQ-009 | 患者搜索支持分页加载 | P2 | 体验优化 | 2 小时 |
| REQ-010 | 读卡器区域条件显示 | P2 | 体验优化 | 1 小时 |
| REQ-011 | 空状态和加载态 UI 优化 | P2 | 体验优化 | 2 小时 |

### 3.2 详细需求

---

#### REQ-001: 统一两条入口路径的医案创建逻辑

**优先级**: P0
**类型**: 功能修复
**关联 PRD**: `registration.md` Section 4.1 "医生直接看诊 -- 系统后台静默创建 Registration + MedicalCase"

**当前行为**:
- 路径 A (PatientSelection): 仅调用 `CreateMedicalCaseAsync(patientId)`，不创建 Registration
- 路径 B (RegistrationList): 调用 `StartVisitAsync(registrationId)`，同时创建 Registration + MedicalCase

**目标行为**:
- 路径 A 在选择患者后，应先检查是否已有 Waiting 状态的 Registration（医生直接看诊时前台可能已挂号）
  - 如果有 Waiting Registration → 调用 `StartVisitAsync(registrationId)` 将其转为 InProgress
  - 如果没有 Registration → **静默创建一条 Registration** (Source=Doctor, Status=InProgress)，再创建 MedicalCase
- 确保所有接诊路径都产生 Registration 记录

**实现方案**:

方案一（推荐）：在 `PatientSelectionViewModel.StartMedicalCaseAsync` 中增加 Registration 创建/关联逻辑：
1. 调用新接口 `GetPatientWaitingRegistrationAsync(patientId, doctorId)` 检查是否有 Waiting 状态的挂号
2. 如果有 → 调用现有 `StartVisitAsync(registrationId)`
3. 如果没有 → 调用新接口 `CreateDoctorDirectRegistrationAsync(patientId, doctorId)` 静默创建 Registration (Source=Doctor, Status=InProgress)，返回 registrationId
4. 后续逻辑不变

方案二（简化）：仅在前端 `CreateMedicalCaseAsync` 成功后，补充调用一个 `CreateRegistrationAsync` 来补录 Registration 记录。

**验收标准**:
- [ ] 从"开始接诊"入口创建医案后，数据库中存在对应的 Registration 记录 (Source=Doctor, Status=InProgress)
- [ ] 从挂号队列接诊后，Registration 状态正确更新为 InProgress
- [ ] 两种路径最终都导航到同一个 MedicalCaseWorkspaceView (Clinical 模式)
- [ ] 相关单元测试覆盖两条路径

---

#### REQ-002: 修复待诊队列切换患者时 PatientDetail 为 null

**优先级**: P0
**类型**: Bug 修复

**当前行为**:
- `PendingQueueViewModel.GetPatientDetail(patientId)` 仅在当前患者匹配时返回详情
- 不匹配时返回 null → 传给 `MedicalCaseWorkspaceViewModel` 的 `CurrentPatient` 为 null

**目标行为**:
- 从待诊队列选择任意患者时，都能正确加载该患者的 `PatientDetailDto` 并传递给工作台

**实现方案**:

修改 `PendingQueueViewModel.GetPatientDetail` 方法：
1. 如果当前患者匹配 → 直接返回（现有逻辑）
2. 如果不匹配 → 通过 `IPatientService.GetByIdAsync(patientId)` 或 `IPatientApi.GetPatientByIdAsync(patientId)` 加载患者详情
3. 加载成功后返回详情，加载失败则记录日志并返回 null（由调用方处理错误）

**验收标准**:
- [ ] 从待诊队列选择非当前选中患者时，医案工作台能正确显示患者信息
- [ ] 患者详情加载失败时显示错误提示而非空白

---

#### REQ-003: 统计数据接入真实接口或暂时隐藏

**优先级**: P0
**类型**: 功能修复
**关联 Issue**: 重构计划中已标记"统计数据无接口"

**当前行为**:
- `TodayConsultationCount = 0`, `PendingCaseCount = 0` (硬编码)

**目标行为**（二选一）：

方案 A（推荐 -- 暂时隐藏）：
- 在 `ClinicalHomeView.xaml` 中隐藏统计卡片（或移除）
- 在 `ClinicalHomeViewModel.cs` 中删除 `TodayConsultationCount` 和 `PendingCaseCount` 属性及 `LoadTodayStatistics` 方法
- 理由：后端统计接口尚未实现，显示 0 比不显示更让用户困惑

方案 B（接入真实接口）：
- 后端新增 `GET /api/statistics/today?doctorId={id}` 接口
- 前端调用接口获取真实数据
- 工作量更大，需后端配合

**验收标准**（方案 A）：
- [ ] 首页不再显示"今日统计"卡片
- [ ] ViewModel 中不再有硬编码的统计属性和方法
- [ ] XAML 布局仍然美观（移除统计卡片后主卡片居中正常）

---

#### REQ-004: BR-001 碰撞处理改为三选一弹窗

**优先级**: P1
**类型**: 功能补齐
**关联 PRD**: `medical-cases.md` BR-001 碰撞处理规则

**当前行为**:
- `ShowConfirmAsync(message, "选择操作")` → Yes=继续, No=新建
- 用户无法选择"取消，什么都不做"

**目标行为**:
- 三选一弹窗：
  - **继续看诊** → 打开已有医案 (继续原操作)
  - **新建医案** → 关闭/挂起旧医案，创建新医案
  - **取消** → 关闭弹窗，返回患者选择页

**实现方案**:
1. 将 `ShowConfirmAsync` (二选一) 替换为 `ShowTripleChoiceAsync` (三选一)
2. `PatientSelectionViewModel.HandleSuspendedCaseAsync` 和 `PendingQueueViewModel.HandleSuspendedCaseAsync` 都需要修改
3. 弹窗文案优化：
   ```
   患者 [张三] 有未完成的医案。

   请选择操作：
   • 继续看诊 - 打开原医案继续编辑
   • 新建医案 - 暂存原医案并创建新的
   • 取消 - 返回患者列表
   ```

**验收标准**:
- [ ] 弹窗提供三个选项（继续看诊/新建医案/取消）
- [ ] 选择"取消"时关闭弹窗，不进行任何操作
- [ ] PatientSelection 和 PendingQueue 两处弹窗行为一致

---

#### REQ-005: 统一按钮文案为"开始看诊"

**优先级**: P1
**类型**: UI 优化

**当前行为**:
- `PatientSelectionView.xaml` 底部按钮文案："诊断"

**目标行为**:
- 统一使用 PRD 术语"开始看诊"

**验收标准**:
- [ ] `PatientSelectionView.xaml` 底部按钮文案改为"开始看诊"
- [ ] 与首页"开始接诊"卡片文案保持一致或相近

---

#### REQ-006: 清理 ClinicalHomeViewModel 多余命令

**优先级**: P1
**类型**: 代码清理
**关联文档**: `medicalcase-workspace-refactoring-analysis.md` 附录 A.3

**当前行为**:
- `ClinicalHomeViewModel` 包含以下命令：
  - `NavigateToPatientManagementCommand` (保留)
  - `NavigateToMedicalCaseQueryCommand` (保留)
  - `NavigateToHerbLibraryCommand` (应移除)
  - `NavigateToFormulaLibraryCommand` (保留，但文案应改为"我的验方")
  - `NavigateToRegistrationQueueCommand` (应移除)
  - `NavigateToSyncCommand` (应移除)

**目标行为**:
- 移除 `NavigateToHerbLibraryCommand`, `NavigateToRegistrationQueueCommand`, `NavigateToSyncCommand`
- 移除对应的 `TodayConsultationCount`, `PendingCaseCount` 属性（与 REQ-003 合并处理）
- XAML 中对应的卡片也需移除

**验收标准**:
- [ ] ViewModel 中不再有多余的导航命令
- [ ] XAML 中不再有多余的功能卡片
- [ ] 编译通过，无未绑定命令的绑定错误

---

#### REQ-007: 患者选择页增加待诊队列自动刷新

**优先级**: P1
**类型**: 功能补齐

**当前行为**:
- `OnNavigatedTo` 时调用一次 `RefreshQueueAsync()`
- 前台新建挂号后，医生在患者选择页看不到新患者，需手动刷新

**目标行为**:
- 待诊队列每 30 秒自动刷新一次（与 `RegistrationListViewModel` 保持一致）
- 离开页面时停止自动刷新

**实现方案**:
- 在 `PatientSelectionViewModel` 中添加 `PeriodicTimer`（复用 `RegistrationListViewModel` 的模式）
- 或使用 `PendingQueueManager.PendingQueueLoaded` 事件触发刷新

**验收标准**:
- [ ] 患者选择页的待诊队列每 30 秒自动刷新
- [ ] 离开页面后停止自动刷新
- [ ] 刷新期间不影响用户操作

---

#### REQ-008: MedicalCaseId 为空时统一错误守卫

**优先级**: P1
**类型**: 代码质量

**当前行为**:
- 多处 `if (existingCase.MedicalCaseId.HasValue)` 为 false 时弹错后继续执行

**目标行为**:
- 所有 MedicalCaseId 为空的分支都有明确的 `return` 守卫
- 错误日志包含完整的上下文信息

**验收标准**:
- [ ] 所有 MedicalCaseId 为空的路径都有 `return` 终止执行
- [ ] 错误日志包含 PatientId, CaseStatus 等上下文

---

#### REQ-009: 患者搜索支持分页加载

**优先级**: P2
**类型**: 体验优化

**当前行为**:
- `GetPatientsAsync(page: 1, pageSize: 100)` 固定加载前 100 条

**目标行为**:
- 支持"加载更多"或无限滚动
- 或改为搜索驱动模式：用户输入关键词后才加载（默认不加载或仅加载最近就诊的 20 人）

**验收标准**:
- [ ] 患者超过 100 人时能搜索到所有匹配结果
- [ ] 搜索响应时间 < 1 秒

---

#### REQ-010: 读卡器区域条件显示

**优先级**: P2
**类型**: 体验优化

**当前行为**:
- 左侧 280px 固定分配给读卡器+待诊队列

**目标行为**:
- 检测读卡器硬件连接状态
- 未连接时隐藏读卡器区域，待诊队列扩展至全宽
- 或改为可折叠面板

**验收标准**:
- [ ] 无读卡器时左侧区域不显示或可折叠
- [ ] 有待诊队列时正常显示

---

#### REQ-011: 空状态和加载态 UI 优化

**优先级**: P2
**类型**: 体验优化

**当前行为**:
- 空队列显示"暂无待诊患者"文字
- 加载期间无视觉反馈

**目标行为**:
- 空队列显示引导操作（如"暂无待诊患者，可通过搜索选择患者"）
- 加载期间显示骨架屏或 Spinner

**验收标准**:
- [ ] 空状态有引导文案
- [ ] 加载态有视觉反馈

---

## 四、实施方案

### 4.1 Phase 1: 快速修复（预计 4-6 小时）

| 需求 | 涉及文件 | 变更类型 |
|------|----------|----------|
| REQ-002 | `PendingQueueViewModel.cs` | Bug 修复 |
| REQ-003 | `ClinicalHomeView.xaml`, `ClinicalHomeViewModel.cs` | UI 简化 |
| REQ-005 | `PatientSelectionView.xaml` | 文案修改 |
| REQ-008 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs` | 代码质量 |

**执行顺序**: REQ-002 → REQ-003 → REQ-005 → REQ-008

### 4.2 Phase 2: 功能补齐（预计 5-7 小时）

| 需求 | 涉及文件 | 变更类型 |
|------|----------|----------|
| REQ-001 | `PatientSelectionViewModel.cs`, 后端 API | 功能修复 |
| REQ-004 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs` | 功能补齐 |
| REQ-006 | `ClinicalHomeView.xaml`, `ClinicalHomeViewModel.cs` | 代码清理 |
| REQ-007 | `PatientSelectionViewModel.cs` | 功能补齐 |

**执行顺序**: REQ-006 → REQ-004 → REQ-007 → REQ-001

### 4.3 Phase 3: 体验优化（预计 5-6 小时）

| 需求 | 涉及文件 | 变更类型 |
|------|----------|----------|
| REQ-009 | `PatientSelectionViewModel.cs`, `PatientSelectionView.xaml` | 体验优化 |
| REQ-010 | `PatientSelectionView.xaml` | 体验优化 |
| REQ-011 | `PatientSelectionView.xaml` | 体验优化 |

---

## 五、风险与依赖

### 5.1 后端依赖

| 需求 | 后端变更 | 说明 |
|------|----------|------|
| REQ-001 | 需要 | 新增 `CreateDoctorDirectRegistration` API 或在 `CreateMedicalCase` 中自动补录 Registration |
| REQ-003 | 方案 A 无需后端 | 方案 B 需要新增统计 API |

### 5.2 风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| REQ-001 涉及后端 API 变更 | 前后端需同步发布 | 先实现前端兼容层，后端 API 就绪后切换 |
| REQ-004 弹窗改造 | 现有 `ShowTripleChoiceAsync` 可能不存在 | 检查 `ICommonDialogService` 接口，必要时新增方法 |
| REQ-009 分页改造 | 可能影响现有搜索逻辑 | 改为搜索驱动模式，减少分页复杂度 |

---

## 六、测试策略

### 6.1 单元测试

| 测试场景 | 测试文件 |
|----------|----------|
| REQ-001: 有 Waiting Registration 时正确调用 StartVisit | `PatientSelectionViewModelTests.cs` |
| REQ-001: 无 Registration 时静默创建 | `PatientSelectionViewModelTests.cs` |
| REQ-002: 切换患者时正确加载 PatientDetail | `PendingQueueViewModelTests.cs` |
| REQ-004: 三选一弹窗各分支逻辑 | `PatientSelectionViewModelTests.cs` |
| REQ-007: 自动刷新定时触发 | `PatientSelectionViewModelTests.cs` |

### 6.2 集成测试

| 测试场景 | 说明 |
|----------|------|
| 端到端：开始接诊 → 选择患者 → 创建医案 → 导航到工作台 | 验证完整链路 |
| 端到端：挂号队列 → 接诊 → 导航到工作台 | 验证路径 B |
| 并发：前台新建挂号后 30 秒内医生可见 | 验证 REQ-007 |

### 6.3 手动测试

| 测试项 | 步骤 | 预期 |
|--------|------|------|
| 统计卡片隐藏 | 打开 Clinical 首页 | 不显示统计卡片 |
| 按钮文案统一 | 打开患者选择页 | 底部按钮显示"开始看诊" |
| BR-001 三选一 | 选择有挂起医案的患者 | 弹窗显示三个选项 |
| 待诊队列自动刷新 | 前台新建挂号，观察医生端 | 30 秒内队列出现新患者 |

---

## 七、术语表

| 术语 | 含义 |
|------|------|
| 开始接诊 | 医生首页的主操作入口，点击后进入患者选择页 |
| 开始看诊 | 患者选择页的确认操作，点击后创建/打开医案并进入工作台 |
| 医案 (MedicalCase) | 一次完整的诊疗记录，DDD 聚合根 |
| 挂号 (Registration) | 患者就诊的排队记录 |
| 待诊队列 | 当前医生名下 Waiting/Active/Suspended 状态的医案列表 |
| BR-001 | 医案碰撞处理规则：创建医案前检查患者是否有 Active/Suspended 医案 |
| BR-002 | 离开决策规则：有未保存变更时的弹窗逻辑 |
| BR-003 | 完成校验规则：诊断+处方必填项校验 |
| Clinical 模式 | 临床看诊模式，从患者选择/待诊队列进入，默认 Editing |
| Management 模式 | 医案管理模式，从医案列表进入，默认 ReadOnly |

---

*文档版本: v1.0 | 创建日期: 2026-04-12 | 状态: 待评审*

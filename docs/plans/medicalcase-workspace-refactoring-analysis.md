# 医生看诊工作台重构分析

> **日期**: 2026-04-09
> **目的**: 分析当前医生看诊工作台的实现与 PRD 的差距，提出重构意见
> **范围**: MedicalCase 模块的 UI 交互、数据流、状态管理

---

## 一、PRD 定义的完整看诊流程

根据 `docs/02-requirements/medical-cases.md` 和 `docs/01-product/clinical-workflow.md`：

```
患者到达 → 查询/创建患者 → 挂号(可选) → 创建医案(Active)
  → 填写诊断 → 标记处方需求 → 开具处方(验方导入/历史复制/手工)
  → 聚合保存 → 打印预览 → 确认打印 → 完成医案(Completed)
```

### 两种入口模式 (MC-D19)

| 模式 | 入口 | 编辑模式 | 返回目标 |
|------|------|----------|----------|
| 模式 1 | 前台挂号 → 医生从待诊队列选中 | Clinical (Editing) | 待诊队列 |
| 模式 2 | 医生直接查询患者创建 | Clinical (Editing) | 患者选择 |

### 编辑模式 (US-MC-011)

| 模式 | 入口 | 默认状态 | 底部按钮 |
|------|------|----------|----------|
| **Clinical** | 患者选择/待诊队列 | Editing | [挂起医案] [打印处方笺] [完成看诊] |
| **Management** | 医案列表 | ReadOnly | [编辑医案] [打印处方笺] |
| Management | 点击"编辑医案"后 | Editing | [保存医案] [取消编辑] [打印处方笺] |

### 离开界面决策 (BR-002)

- **Clinical 模式有变更**: 弹窗 [挂起] [关闭(取消)] [完成看诊]
- **Management 模式有变更**: 弹窗 [保存] [放弃] [取消离开]
- **无变更**: 直接离开

---

## 二、当前实现分析

### 2.1 架构现状

当前 MedicalCase 模块使用 **MasterDetail 模式**（列表 + 详情），由以下组件组成：

| 组件 | 文件 | 职责 |
|------|------|------|
| ViewModel | `MedicalCaseMasterDetailViewModel.cs` | 列表加载、详情加载、保存、删除 |
| MasterDetailControl | `MedicalCaseMasterDetailControl.xaml` | 列表 DataGrid + EditControl/ViewControl 容器 |
| EditControl | `MedicalCaseEditControl.xaml` | 诊断 + 处方编辑表单 |
| ViewControl | `MedicalCaseViewControl.xaml` | 医案详情只读展示 |

### 2.2 当前实现与 PRD 的差距

#### 差距 1：缺少 Clinical / Management 编辑模式区分

**PRD 要求** (US-MC-011):
- Clinical 模式：从患者选择进入，默认 Editing，底部按钮 [挂起] [打印] [完成看诊]
- Management 模式：从医案列表进入，默认 ReadOnly，底部按钮 [编辑医案] [打印]

**当前实现**:
- 只有一种模式，统一使用 MasterDetail 模式
- 没有模式切换逻辑
- 没有挂起/完成等状态操作按钮
- 没有离开界面的 BR-002 决策弹窗

**影响**: 医生无法区分"正在看诊"和"查看历史医案"两种场景，操作流程混乱。

#### 差距 2：缺少医案状态操作

**PRD 要求**:
- 挂起医案 (US-MC-006): 状态转为 Suspended
- 完成医案 (US-MC-007): 校验 BR-003 后转为 Completed
- 取消医案 (US-MC-008): 软删除

**当前实现**:
- 只有 CRUD（增删改查）
- 没有挂起/完成/取消操作
- 没有完成校验 (BR-003)

#### 差距 3：缺少处方来源选择

**PRD 要求**:
- 验方导入 (US-MC-016): 从验方库导入药材
- 历史处方复制 (US-MC-018): 从患者历史 Completed 医案复制处方

**当前实现**:
- 只有手工输入处方药材
- 没有验方导入入口
- 没有历史处方复制入口

#### 差距 4：缺少打印流程

**PRD 要求** (US-MC-015):
- 打印预览 (FR-PRINT-002)
- 确认打印 → IsPrinted=true, PrintCount++, PrintLog
- 打印后修改需 EditReason

**当前实现**:
- 没有打印功能集成在看诊工作台中

#### 差距 5：数据流过于复杂

**当前 ViewModel 的问题**:
- `MedicalCaseMasterDetailViewModel` 承担了太多职责：列表、详情、处方编辑、药材加载
- `Consultation` 和 `Prescription` 作为独立属性管理，与 `MedicalCaseDetailModel` 之间存在数据同步问题
- `InitializeEditModels()` 方法将 DTO 数据拆分到多个对象，增加维护成本

#### 差距 6：缺少 BR-001 碰撞处理

**PRD 要求**: 创建医案时检查患者是否有 Active/Suspended 医案，三选一处理

**当前实现**: `CreateNewDetail()` 直接抛 `NotSupportedException`，创建医案不在此模块处理

#### 差距 7：缺少待诊队列

**PRD 要求** (US-MC-017): 查看当前待看诊患者列表

**当前实现**: 医案列表是通用分页查询，没有专门的待诊队列视图

---

## 三、核心问题诊断

### 3.1 根本问题：MasterDetail 模式不适合看诊场景

MasterDetail 模式适用于**管理场景**（浏览列表 → 查看/编辑单条记录），但**不适合看诊场景**：

| 维度 | MasterDetail (管理) | 看诊工作流 (Clinical) |
|------|---------------------|----------------------|
| 主要操作 | 浏览 + 偶尔编辑 | 创建 → 填写 → 保存 → 完成 |
| 状态流转 | 无 | Active → Suspended → Completed |
| 离开行为 | 直接返回 | 必须选择处置方式 (挂起/完成/取消) |
| 数据完整性 | 可部分填写 | 完成时需通过 BR-003 校验 |
| 底部操作 | 工具栏按钮 | 场景化按钮 (挂起/打印/完成) |

### 3.2 当前架构的连锁问题

1. **没有入口区分**: 无论从待诊队列还是医案列表进入，看到的都是同一个 MasterDetail 界面
2. **没有模式切换**: 无法在 ReadOnly 和 Editing 之间切换
3. **没有状态操作**: 挂起/完成/取消操作缺失
4. **没有流程引导**: 医生不知道下一步该做什么（诊断 → 处方 → 打印 → 完成）
5. **没有数据校验反馈**: BR-003 校验只在服务端，客户端无提示

---

## 四、重构建议

### 4.1 方案概述：双模式架构

将医案模块拆分为两个独立的 workspace：

```
MedicalCase 模块
├── ClinicalWorkspace (看诊工作台)
│   ├── PatientSelectionView    → 患者选择/待诊队列
│   ├── MedicalCaseWorkspaceView → 看诊工作区 (Clinical 模式)
│   └── MedicalCaseWorkspaceViewModel
│
└── ManagementWorkspace (医案管理)
    ├── MedicalCaseListView     → 医案列表 + 搜索
    ├── MedicalCaseDetailView   → 医案详情查看 (Management 模式)
    └── MedicalCaseManagementViewModel
```

### 4.2 ClinicalWorkspace 设计

**入口**:
- 从首页待诊队列点击 → 进入 PatientSelectionView
- 从患者选择创建医案 → 直接进入 MedicalCaseWorkspaceView

**MedicalCaseWorkspaceView 布局**:

```
┌──────────────────────────────────────────────────────┐
│ 患者: 张三 | 医案编号: MC20260409001 | 状态: 进行中    │
├──────────────────────┬───────────────────────────────┤
│  诊断区 (左侧)        │  处方区 (右侧)                 │
│                      │                               │
│  [现病史]             │  [处方需求: ○需要 ○不需要]     │
│  [舌诊]               │                               │
│  [脉诊]               │  ┌─ 药材列表 ────────────┐    │
│  [中医辨证*]          │  │ 药名  剂量  煎法  单价 │    │
│                      │  │                        │    │
│                      │  │ [+添加] [验方导入]      │    │
│                      │  │ [历史处方]             │    │
│                      │  └────────────────────────┘    │
│                      │                               │
│                      │  帖数: [7]  折扣: [1.0]       │
│                      │  单剂价: ¥45.00  总价: ¥315.00 │
├──────────────────────┴───────────────────────────────┤
│  [挂起医案]          [打印处方笺]        [完成看诊]    │
└──────────────────────────────────────────────────────┤
```

**关键特性**:
1. **自动创建医案**: 选择患者后自动创建 MedicalCase (Active) + Consultation
2. **实时保存**: 每次修改自动聚合保存（或手动暂存）
3. **完成校验**: 点击"完成看诊"时客户端预校验 BR-003
4. **离开保护**: 有未保存变更时弹窗 (BR-002)
5. **处方来源**: 支持验方导入、历史处方复制、手工输入

### 4.3 ManagementWorkspace 设计

**入口**: 从 Sidebar "医案管理" 进入

**MedicalCaseManagementView 布局**:

```
┌──────────────────────────────────────────────────────┐
│ 医案管理                                              │
│ [搜索框] [状态筛选] [患者筛选] [搜索] [清除]           │
├──────────────────────────────────────────────────────┤
│ 医案列表 (DataGrid)                                   │
│ 编号 | 患者 | 医生 | 状态 | 诊断 | 创建时间 | 操作     │
│ ...                                                   │
├──────────────────────────────────────────────────────┤
│ 选中医案详情 (只读)                                    │
│ [编辑医案] [打印处方笺]                                │
└──────────────────────────────────────────────────────┘
```

**关键特性**:
1. **默认 ReadOnly**: 查看医案详情，不可编辑
2. **点击"编辑医案"**: 切换到 Editing 模式
3. **Editing 底部按钮**: [保存医案] [取消编辑] [打印处方笺]
4. **离开保护**: 有未保存变更时弹窗 [保存] [放弃] [取消离开]

### 4.4 数据流重构

**当前问题**: ViewModel 中 `Consultation`、`Prescription`、`MedicalCaseDetailModel` 三套数据并存，同步复杂。

**建议**: 统一使用 `MedicalCaseDetailModel` 作为唯一数据源：

```
MedicalCaseDetailModel (唯一数据源)
├── MedicalCase 基础信息
├── Consultation (诊断)
├── Prescription (处方)
│   └── Items (处方药材)
└── 计算属性 (SingleDosePrice, TotalPrice, IsLocked 等)
```

EditControl 直接绑定 `MedicalCaseDetailModel` 的各子属性，不再需要独立的 `Consultation`/`Prescription` 属性。

### 4.5 状态管理

**新增**: `MedicalCaseWorkspaceState` 枚举

```
enum WorkspaceMode { Clinical, Management }
enum EditMode { ReadOnly, Editing }

Clinical 模式: 始终 Editing
Management 模式: 默认 ReadOnly → 点击编辑 → Editing
```

---

## 五、重构优先级

| 优先级 | 任务 | 原因 |
|--------|------|------|
| **P0** | 创建 ClinicalWorkspace 基础框架 | 看诊是核心流程，当前完全缺失 |
| **P0** | 实现医案状态操作 (挂起/完成/取消) | PRD 核心功能 |
| **P1** | 实现 Clinical/Management 模式区分 | 用户体验核心 |
| **P1** | 实现 BR-002 离开决策弹窗 | 数据安全 |
| **P1** | 统一数据模型 (消除三套数据并存) | 架构简化 |
| **P2** | 实现验方导入到处方 | 效率提升 |
| **P2** | 实现历史处方复制 | 效率提升 |
| **P2** | 集成打印功能 | 完整流程 |
| **P3** | 待诊队列视图 | 辅助功能 |
| **P3** | 自动保存/暂存 | 体验优化 |

---

## 六、待讨论问题

1. **Clinical 模式是否需要"暂存"功能？** PRD 中无自动保存，但实际看诊中医生可能希望随时保存进度
2. **验方导入和历史处方复制的 UI 形式**：弹窗选择？侧边栏？还是内嵌区域？
3. **打印时机**：PRD 中打印是独立于完成的可选步骤，是否需要在完成前强制打印？
4. **挂起医案的后续处理**：医生回到挂起医案时，是否需要提醒？
5. **Clinical 和 Management 是否共用同一个 EditControl？** 还是各自独立？

---

## 七、相关文件索引

| 文件 | 路径 | 说明 |
|------|------|------|
| 医案 PRD | `docs/02-requirements/medical-cases.md` | 18 个 User Stories |
| 临床工作流 | `docs/01-product/clinical-workflow.md` | 端到端流程 |
| 当前 ViewModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs` | 327 行 |
| 当前 EditControl | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | 诊断+处方编辑 |
| 当前 MasterDetail | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml` | 列表+详情容器 |
| DetailModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs` | 数据模型 |

---

*文档版本: v1.0 | 创建日期: 2026-04-09 | 状态: 待讨论*

---

## 附录 A：临床工作台首页重构（2026-04-09 讨论确认）

### A.1 当前设计问题

| 问题 | 说明 |
|------|------|
| 操作型 vs 管理型混排 | 挂号队列（看诊中操作）与药材库/验方库（后台管理）平铺，无优先级区分 |
| 入口重叠 | "开始接诊"和"医案查询"最终都到医案工作区，只是模式不同，用户无法感知 |
| 职责错位 | 药材库维护是 Admin 职责，不应出现在医生工作台 |
| 挂号队列位置不当 | 挂号队列是"开始接诊"后续流程的子步骤，不应与"开始接诊"平级 |
| 统计数据无接口 | 今日统计/待完成医案无后端接口，显示为 0 |

### A.2 最终设计方案

```
┌──────────────────────────────────────────────────────────────┐
│                        临床工作台                              │
│                      凌隐宝堂中医诊所                           │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │   🩺  开始接诊                                        │    │
│  │   选择患者，开始诊疗                                   │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                    │
│  │ 🔍       │ │ 📖       │ │ 👤       │                    │
│  │ 医案查询  │ │ 我的验方  │ │ 患者管理  │                    │
│  │ 查找历史  │ │ 个人验方  │ │ 档案维护  │                    │
│  └──────────┘ └──────────┘ └──────────┘                    │
└──────────────────────────────────────────────────────────────┘
```

### A.3 变更汇总

| 操作 | 元素 | 理由 |
|------|------|------|
| **保留** | 开始接诊 | 核心入口 |
| **移除** | 今日统计 | 无接口实现 |
| **移除** | 挂号队列 | 属于"开始接诊"后续流程（患者选择页） |
| **移除** | 药材库 | Admin 职责，医生不需要 |
| **移除** | 数据同步 | 低频运维功能，侧边栏已有 |
| **保留+优化** | 验方库 → "我的验方" | 文案更精准，强调个人验方维护 |
| **保留** | 医案查询 | 复诊查历史 |
| **保留** | 患者管理 | 前台不在时医生维护档案 |

### A.4 挂号队列的归属

挂号队列移至**患者选择页面**（PatientSelectionView），作为医生选择患者的来源之一：

```
开始接诊 → 患者选择页面
            ├── 搜索患者
            ├── 新建患者
            └── 挂号队列（待诊患者列表）← 这里
```

### A.5 涉及修改的文件

| 文件 | 变更 |
|------|------|
| `ClinicalHomeView.xaml` | 删除统计卡片、挂号队列、药材库、数据同步；验方库文案改"我的验方" |
| `ClinicalHomeViewModel.cs` | 删除 5 个命令 + 2 个统计属性 + 相关方法 |

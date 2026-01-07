# Proposal: refactor-clinical-workflow

## Summary

**看诊工作流重构** - 统一Clinical模块架构，将医生工作流的三个核心界面（主页、患者选择、看诊）整合到Clinical模块中，采用控件模式实现功能复用。

## 三大部分概述

| 部分 | 界面 | 当前状态 | 变更计划 |
|------|------|----------|----------|
| **1. 主页** | `ClinicalHomeView` | 已完成 | **不动** |
| **2. 患者选择** | `PatientSelectionView` | 在Patients模块 | 重新设计并迁移到Clinical |
| **3. 看诊** | `MedicalCaseWorkspaceView` | 在MedicalCase模块 | 迁移到Clinical |

---

## Architecture Context

### 控件模式架构原则

| 层级 | 位置 | 职责 |
|------|------|------|
| **主界面(View)** | 角色模块 `LYBT.Desktop.Clinical` | 页面布局、导航、角色操作 |
| **控件(Control)** | 功能模块 (Patients/MedicalCase) | 可复用业务组件 |

### 目标架构

```
LYBT.Desktop.Clinical/                    # 角色模块 - 医生
├── Views/
│   ├── ClinicalHomeView.xaml             # 1. 主页 (不动)
│   ├── PatientSelectionView.xaml         # 2. 患者选择 (新建)
│   └── MedicalCaseWorkspaceView.xaml     # 3. 看诊 (迁移)
└── ViewModels/
    ├── ClinicalHomeViewModel.cs          # (不动)
    ├── PatientSelectionViewModel.cs      # (新建)
    └── MedicalCaseWorkspaceViewModel.cs  # (迁移)

LYBT.Desktop.Patients/                    # 功能模块 - 患者
└── Controls/
    ├── PatientSelectionControl.xaml      # 新建 - 患者选择控件
    ├── PatientViewControl.xaml           # 复用 - 只读详情
    └── PatientSearchControl.xaml         # 复用 - 搜索列表

LYBT.Desktop.MedicalCase/                 # 功能模块 - 医案
└── Controls/
    ├── PendingQueueControl.xaml          # 已存在 - 待诊队列
    └── (其他医案控件)
```

### 导航流程

```
ClinicalHomeView → PatientSelectionView → MedicalCaseWorkspaceView
     主页               患者选择              看诊
```

---

## Part 1: 主页 (ClinicalHomeView)

**状态**: 不动

**当前功能**:
- 医生工作台入口
- 显示今日统计
- 提供"开始接诊"入口

**导航出口**: `ClinicalHomeViewModel.ExecuteStartConsultation()` → PatientSelectionView

---

## Part 2: 患者选择 (PatientSelectionView)

### 当前问题

1. **布局复杂**: 三区域布局（患者详情+待诊队列+患者列表）
2. **位置不当**: 定义在Patients模块而非Clinical模块
3. **职责混乱**: 待诊队列应在看诊界面，不应在患者选择

### 设计目标

1. **简洁布局**: Master-Detail模式（患者列表+详情）
2. **位置正确**: 主界面在Clinical，控件在Patients
3. **职责清晰**: 仅负责选择患者、挂号/开始看诊

### 新界面设计

```
┌─────────────────────────────────────────────────────────────┐
│ 患者选择                                    [返回主页]     │
├─────────────────────────────────────────────────────────────┤
│ Master区域                 │ Detail区域                    │
│ ┌─────────────────────────┐│┌───────────────────────────┐  │
│ │ [新建] [刷新]           ││ 患者详情                    │  │
│ │ [搜索框...]             ││ 姓名：张三                  │  │
│ │ ───────────────────────││ 性别/年龄/电话...           │  │
│ │ 患者列表(可分页)        ││                             │  │
│ └─────────────────────────┘│└───────────────────────────┘  │
├─────────────────────────────────────────────────────────────┤
│ [状态消息]                       [挂号] 或 [开始看诊]      │
└─────────────────────────────────────────────────────────────┘
```

### 角色操作区分

| 角色 | 操作按钮 | 点击后行为 |
|------|----------|------------|
| 前台 | **挂号** | 创建待诊记录(Waiting状态)，患者进入待诊队列 |
| 医生 | **开始看诊** | 检查挂起医案 → 创建/继续医案 → 导航到看诊界面 |

### 挂起医案处理 (参考 optimize-medicalcase-navigation)

**四选项弹窗** - 医生选择有挂起医案的患者时显示:

| # | 选项 | 操作 |
|---|------|------|
| 1 | 继续挂起医案 | 导航到挂起医案继续编辑 |
| 2 | 关闭挂起+新建 | 取消原医案，创建新医案 |
| 3 | 仅关闭挂起 | 取消原医案，留在当前界面 |
| 4 | 取消 | 不做任何操作 |

---

## Part 3: 看诊 (MedicalCaseWorkspaceView)

### 当前状态

- 位置: `LYBT.Desktop.MedicalCase/Views/`
- 功能完整，已包含待诊队列处理

### 变更计划

**仅迁移位置**，功能不变:

1. 将View/ViewModel从MedicalCase模块迁移到Clinical模块
2. 保留MedicalCase模块中的Controls (如PendingQueueControl)
3. 更新模块注册

### 待诊队列说明 (参考 redesign-pending-queue)

待诊队列状态定义:

| 状态 | 英文 | 含义 | 操作 |
|------|------|------|------|
| 待诊 | Waiting | 已挂号等待 | 双击新建医案 |
| 挂起 | Suspended | 有Draft医案 | 双击显示四选项弹窗 |
| 正在看诊 | InProgress | 当前患者 | 不可操作 |

---

## Scope

### In Scope

**Part 2 - 患者选择**:
- 新建`PatientSelectionView`在Clinical模块
- 新建`PatientSelectionControl`在Patients模块
- 实现Master-Detail布局
- 实现角色区分(挂号/看诊)
- 实现挂起医案四选项弹窗
- 废弃旧`PatientSelectionView`

**Part 3 - 看诊**:
- 迁移`MedicalCaseWorkspaceView`到Clinical模块
- 迁移`MedicalCaseWorkspaceViewModel`到Clinical模块
- 更新模块注册

### Out of Scope

- Part 1 主页 (不动)
- 患者管理功能 (`PatientMasterDetailView`)
- 待诊队列功能细节 (已在redesign-pending-queue完成)
- MedicalCase模块的Controls (保持原位)

---

## Files to Create

| 文件 | 位置 | 说明 |
|------|------|------|
| `PatientSelectionView.xaml` | Clinical/Views/ | 患者选择主界面 |
| `PatientSelectionView.xaml.cs` | Clinical/Views/ | Code-behind |
| `PatientSelectionViewModel.cs` | Clinical/ViewModels/ | ViewModel |
| `PatientSelectionControl.xaml` | Patients/Controls/ | 可复用控件 |
| `PatientSelectionControl.xaml.cs` | Patients/Controls/ | 控件代码 |

## Files to Move

| 文件 | 原位置 | 新位置 |
|------|--------|--------|
| `MedicalCaseWorkspaceView.xaml` | MedicalCase/Views/ | Clinical/Views/ |
| `MedicalCaseWorkspaceView.xaml.cs` | MedicalCase/Views/ | Clinical/Views/ |
| `MedicalCaseWorkspaceViewModel.cs` | MedicalCase/ViewModels/ | Clinical/ViewModels/ |

## Files to Delete

| 文件 | 理由 |
|------|------|
| `Patients/Views/PatientSelectionView.xaml` | 被新设计替代 |
| `Patients/Views/PatientSelectionView.xaml.cs` | 被新设计替代 |
| `Patients/ViewModels/PatientSelectionViewModel.cs` | 被新设计替代 |
| `Patients/ViewModels/Components/PatientSelectionCommandExecutor.cs` | 不再需要 |
| `Patients/Services/PendingQueueManager.cs` | 功能已在MedicalCase模块 |

## Files to Modify

| 文件 | 修改内容 |
|------|----------|
| `ClinicalModule.cs` | 注册新View/ViewModel |
| `PatientsModule.cs` | 注册新Control，删除旧注册 |
| `MedicalCaseModule.cs` | 删除迁移走的View/ViewModel注册 |
| `ClinicalHomeViewModel.cs` | 更新导航目标 |

---

## Dependencies

- `MasterDetailLayout` 控件 (已存在)
- `PatientViewControl` 控件 (已存在)
- `IRoleNavigationService` 服务 (已存在)
- `WorkspacePendingQueueHandler` (已存在)
- `PendingQueueControl` (已存在)

---

## Risks

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 大量代码迁移 | 可能引入编译错误 | 逐步迁移，每步验证 |
| 命名空间变更 | 需更新所有引用 | 使用IDE重构工具 |
| 模块注册变更 | 可能影响导航 | 保留回退能力 |

---

## Timeline

| Phase | 内容 | 预计 |
|-------|------|------|
| 1 | 创建PatientSelectionControl控件 | 1天 |
| 2 | 创建PatientSelectionView主界面 | 1天 |
| 3 | 实现挂号/看诊/挂起医案逻辑 | 1天 |
| 4 | 迁移MedicalCaseWorkspaceView到Clinical | 1天 |
| 5 | 废弃旧代码，更新模块注册 | 1天 |
| 6 | 测试验证 | 1天 |

**总计**: 6天

---

## Success Criteria

1. 三个主界面都在Clinical模块，结构工整
2. 患者选择使用Master-Detail布局，简洁清晰
3. 前台可挂号，医生可开始看诊
4. 挂起医案四选项弹窗正常工作
5. 看诊界面功能不变，仅位置迁移
6. 所有旧代码删除，编译无错误

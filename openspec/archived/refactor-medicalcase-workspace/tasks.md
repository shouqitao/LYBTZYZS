# Tasks: 看诊工作台布局重构 V2

**Change ID**: refactor-medicalcase-workspace
**总任务数**: 31个
**预计工期**: 3.5-4天
**版本**: V2.1

---

## Phase 1: 控件创建 (1天)

### 1.1 创建MedicalCaseEditControl

- [x] **TASK-V2-001**: 创建MedicalCaseEditControl.xaml
  - 文件: `MedicalCase/Controls/MedicalCaseEditControl.xaml`
  - 布局: 诊断区(3行) + 处方区
  - 诊断区: 现病史 / 舌诊+脉诊 / 中医诊断*
  - 处方区: 工具栏 + HerbListControl + 信息栏
  - **完成**: 支持Full/Compact双模式

- [x] **TASK-V2-002**: 创建MedicalCaseEditControl.xaml.cs
  - 文件: `MedicalCase/Controls/MedicalCaseEditControl.xaml.cs`
  - DependencyProperty: Consultation, HerbItems, AllHerbs
  - Commands: ImportFormulaCommand, ImportHistoryCommand, ClearAllCommand
  - **完成**: 添加IsCompactMode属性支持双模式

- [x] **TASK-V2-003**: 集成HerbListControl
  - 引用: `xmlns:herbList="clr-namespace:LYBT.Desktop.Herbs.Controls.HerbList;assembly=LYBT.Desktop.Herbs"`
  - 配置: IsEditMode=True, Columns=4
  - **完成**: Full和Compact模式均集成HerbListControl

### 1.2 创建MedicalCaseViewControl

- [x] **TASK-V2-004**: 创建MedicalCaseViewControl.xaml
  - 文件: `MedicalCase/Controls/MedicalCaseViewControl.xaml`
  - 布局: 诊断信息 + 处方内容 + 底部信息
  - HerbListControl配置: IsEditMode=False
  - **完成**: 支持Full/Compact双模式

- [x] **TASK-V2-005**: 创建MedicalCaseViewControl.xaml.cs
  - 文件: `MedicalCase/Controls/MedicalCaseViewControl.xaml.cs`
  - DependencyProperty: MedicalCase, ShowPrintButton, PrintCommand
  - **完成**: 添加IsCompactMode属性和Compact模式专用属性

### 1.3 编译验证

- [x] **TASK-V2-006**: 控件编译验证
  - 编译通过，无错误
  - DependencyProperty正确注册
  - 无循环依赖
  - **完成**: MedicalCase和Clinical模块编译通过(0错误)

---

## Phase 2: 界面重构 (1天)

### 2.1 MedicalCaseWorkspaceView重构

- [x] **TASK-V2-007**: 左侧布局调整
  - 上部: PatientInfoCardControl
  - 下部: PendingQueueControl (新增)
  - 宽度: 25% (Min 280px, Max 350px)
  - **完成**: 25:75布局，左侧患者卡片+待诊队列

- [x] **TASK-V2-008**: 右侧使用MedicalCaseEditControl
  - 替换现有诊断区+处方区
  - 绑定: Consultation, HerbItems, AllHerbs
  - 命令绑定: ImportFormula, ImportHistory, ClearAll
  - **完成**: EditContent使用MedicalCaseEditControl(IsCompactMode=True)
  - **完成**: ViewContent使用MedicalCaseViewControl(IsCompactMode=True)

- [x] **TASK-V2-009**: 移除旧布局代码
  - 删除原诊断区Grid (35%)
  - 删除原处方区Grid (65%)
  - 保留PrescriptionPanelViewModel
  - **完成**: 移除旧5:5分栏布局，清理未使用的xmlns声明和样式

### 2.2 MedicalCaseWorkspaceViewModel调整

- [x] **TASK-V2-010**: 添加待诊队列支持
  - 属性: PendingQueue, SelectedPendingCase, IsRefreshingPendingQueue, HasNoPendingCases
  - 命令: SelectPendingCaseCommand, RefreshQueueCommand
  - **完成**: 委托给WorkspacePendingQueueHandler处理

- [x] **TASK-V2-011**: 实现患者切换逻辑
  - 检查未保存更改
  - 自动暂存当前医案
  - 加载新患者医案
  - **完成**: 由WorkspacePendingQueueHandler.SelectPendingCaseAsync实现

### 2.3 MedicalCaseMasterDetailControl更新

- [x] **TASK-V2-012**: 使用新控件
  - ViewContent: MedicalCaseViewControl
  - EditContent: MedicalCaseEditControl
  - **完成**: 已使用Full模式(IsCompactMode=False)

---

## Phase 3: 历史控件清理 (1天)

### 3.1 MedicalCase模块清理

- [x] **TASK-V2-013**: 删除MedicalCaseDetailViewModel.cs
  - 原因: 已被MedicalCaseWorkspaceViewModel替代
  - **完成**: 已删除

- [x] **TASK-V2-014**: 删除DuplicateHerbAlertDialog
  - 文件: Views/DuplicateHerbAlertDialog.xaml(.cs)
  - 文件: ViewModels/DuplicateHerbAlertDialogViewModel.cs
  - 原因: 功能合并到HerbListControl
  - **完成**: 已删除

- [x] **TASK-V2-015**: 删除HistoryPrescriptionSelectionDialog
  - 文件: Views/HistoryPrescriptionSelectionDialog.xaml(.cs)
  - 文件: ViewModels/HistoryPrescriptionSelectionDialogViewModel.cs
  - 原因: 功能重复，保留HistoryCopyDialog
  - **完成**: 已删除

- [x] **TASK-V2-016**: 删除AuditLogDialog和AuditReasonDialog
  - 目录: Dialogs/
  - 原因: 审计功能延迟到v2.0
  - **完成**: 已删除

- [x] **TASK-V2-017**: 删除IAuditRequirementChecker和AuditRequirementChecker
  - 原因: 审计功能延迟到v2.0
  - **完成**: 已删除

### 3.2 Consultation模块清理

- [x] **TASK-V2-018**: 删除ConsultationFormViewModel.cs
  - 原因: 诊断字段精简后不再需要
  - **完成**: 已删除

### 3.3 其他模块清理

- [x] **TASK-V2-019**: 删除HerbDetailViewModel.cs (Herbs模块)
  - 原因: 已被HerbMasterDetailControl替代
  - **完成**: 已删除

- [x] **TASK-V2-020**: 删除PatientDetailViewModel.cs和PatientDetailView.xaml (Patients模块)
  - 原因: 已被PatientMasterDetailControl替代
  - **完成**: 已删除

- [x] **TASK-V2-021**: 删除QuickCreatePatientDialog (Patients模块)
  - 原因: 功能合并到PatientEditControl
  - **完成**: 已删除

- [x] **TASK-V2-022**: 删除UserDetailViewModel.cs (Users模块)
  - 原因: 已被UserMasterDetailControl替代
  - **完成**: 已删除

- [x] **TASK-V2-023**: 删除FormulaDetailViewModel.cs和FormulaValidationViewModel.cs (Formula模块)
  - 原因: 已被FormulaMasterDetailControl替代
  - **完成**: 已删除

- [x] **TASK-V2-024**: 删除EditFormulaDialog (Formula模块)
  - 原因: 已被FormulaEditControl替代
  - **完成**: 已删除

### 3.4 Infrastructure模块清理

- [x] **TASK-V2-025**: 删除已迁移的Herb控件
  - 文件: Controls/HerbCardControl.xaml(.cs)
  - 文件: Controls/HerbListView.xaml(.cs)
  - 文件: Controls/HerbListEditor.xaml(.cs)
  - 目录: Controls/HerbItem/
  - 目录: Controls/HerbList/
  - 文件: Models/HerbItemDto.cs
  - 原因: 已迁移到Herbs模块
  - **完成**: 已删除

---

## Phase 4: 集成测试与验证 (0.5天)

### 4.1 编译验证

- [x] **TASK-V2-026**: 全量编译验证
  - 编译通过 (0错误)
  - 警告数量控制
  - 无悬挂引用
  - **完成**: LYBT.All.sln 编译通过 (0错误 0警告)

### 4.2 功能验证

- [ ] **TASK-V2-027**: 核心功能测试
  - 医案编辑流程正常
  - 经验方导入正常
  - 历史医案复制正常
  - 待诊队列切换正常

- [ ] **TASK-V2-028**: 布局验证
  - 左25%:右75%比例正确
  - 诊断区3行布局正确
  - HerbListControl 4列显示正常

---

## Phase 5: Panel控件清理 (0.25天)

### 5.1 删除死代码Panel控件

- [ ] **TASK-V2-029**: 删除ConsultationPanel控件
  - 文件: `MedicalCase/Controls/ConsultationPanel.xaml`
  - 文件: `MedicalCase/Controls/ConsultationPanel.xaml.cs`
  - 原因: 死代码，从未被作为XAML元素引用

- [ ] **TASK-V2-030**: 删除PrescriptionEditorPanel控件
  - 文件: `MedicalCase/Controls/PrescriptionEditorPanel.xaml`
  - 文件: `MedicalCase/Controls/PrescriptionEditorPanel.xaml.cs`
  - 原因: 死代码，从未被作为XAML元素引用

- [ ] **TASK-V2-031**: 编译验证
  - 确认删除后编译通过
  - 无悬挂引用

---

## 验收检查清单

### 控件验收

- [x] MedicalCaseEditControl创建完成 (支持Full/Compact双模式)
- [x] MedicalCaseViewControl创建完成 (支持Full/Compact双模式)
- [x] DependencyProperty绑定正常
- [x] HerbListControl集成正常

### 布局验收

- [x] 左侧: 患者卡片 + 待诊队列
- [x] 右侧: 统一医案表单 (使用MedicalCaseEditControl/ViewControl)
- [x] 诊断区: 现病史/舌诊+脉诊/中医诊断 三行
- [x] 处方区: 工具栏 + 药材列表 + 信息栏

### 清理验收

- [x] 所有列出的过期文件已删除
- [x] 无悬挂引用
- [x] 编译通过

### 功能验收

- [ ] 新建医案流程正常
- [ ] 经验方导入正常
- [ ] 历史医案复制正常
- [ ] 待诊队列切换正常
- [ ] 保存医案正常

---

## 任务依赖关系

```
Phase 1 (控件创建)
    ├── TASK-V2-001 (EditControl XAML)
    │   └── TASK-V2-002 (EditControl CS)
    │       └── TASK-V2-003 (HerbListControl集成)
    ├── TASK-V2-004 (ViewControl XAML)
    │   └── TASK-V2-005 (ViewControl CS)
    └── TASK-V2-006 (编译验证)
            │
Phase 2 (界面重构) ←────┘
    ├── TASK-V2-007 (左侧布局)
    ├── TASK-V2-008 (右侧控件)
    ├── TASK-V2-009 (移除旧代码)
    ├── TASK-V2-010 (ViewModel调整)
    ├── TASK-V2-011 (切换逻辑)
    └── TASK-V2-012 (MasterDetail更新)
            │
Phase 3 (控件清理) ←────┘
    ├── TASK-V2-013 ~ TASK-V2-017 (MedicalCase清理)
    ├── TASK-V2-018 (Consultation清理)
    ├── TASK-V2-019 ~ TASK-V2-024 (其他模块清理)
    └── TASK-V2-025 (Infrastructure清理)
            │
Phase 4 (集成验证) ←────┘
    ├── TASK-V2-026 (编译验证)
    ├── TASK-V2-027 (功能验证)
    └── TASK-V2-028 (布局验证)
```

---

## 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 控件绑定失效 | 充分测试DependencyProperty |
| 删除文件导致编译失败 | 逐个删除，每次验证编译 |
| 功能回归 | 每阶段完成后测试核心流程 |
| HerbListControl兼容性 | 验证AllHerbs传递正确 |

---

**创建时间**: 2025-12-25
**更新时间**: 2026-01-04 21:30
**版本**: V2
**负责人**: Claude Code

---

## 进度摘要

| 阶段 | 状态 | 完成任务 |
|------|------|----------|
| Phase 1 控件创建 | ✅ 完成 | 6/6 |
| Phase 2 界面重构 | ✅ 完成 | 6/6 |
| Phase 3 历史控件清理 | ✅ 完成 | 13/13 |
| Phase 4 集成测试 | 🔄 进行中 | 1/3 (TASK-V2-026) |
| Phase 5 Panel控件清理 | ⏳ 待开始 | 0/3 |

**总体进度**: 26/31 任务完成 (84%)
**剩余任务**:
- TASK-V2-027, TASK-V2-028 - 需运行时测试
- TASK-V2-029~031 - Panel控件死代码清理

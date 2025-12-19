# Proposal: refactor-medicalcase-management

## Summary

重构医案管理模块，将其统一为Master-Detail布局模式，与其他模块（药材、验方、患者、用户）保持一致的UI交互体验。

## Problem Statement

当前医案模块存在以下问题：

1. **UI模式不统一**: 医案管理使用独立的`MedicalCaseManagementView`列表页 + `MedicalCaseDetailView`详情页跳转模式，而其他模块（Formula、Herbs、Patients、Users）已统一使用Master-Detail布局
2. **诊断字段重构后的适配**: 刚完成的`refactor-diagnosis-fields`移除了5个字段，需要更新相关视图
3. **看诊工作区与管理UI共用组件**: 部分组件（如ConsultationPanel）在看诊工作区和管理视图中共用，需要分离以便后续独立演进

## Goals

1. **统一UI模式**: 医案管理采用Master-Detail布局，参考FormulaManagementView的设计
2. **保持看诊流程不变**: MedicalCaseWorkspaceView（看诊工作区）保持现有逻辑，仅更新诊断字段
3. **分离共用组件**: 看诊工作区和管理视图的共用UI组件先分开，后期独立设计
4. **保持业务逻辑不变**: 管理模块仅提供查看和编辑功能，不提供"新建医案"（新建医案通过看诊流程创建）

## Non-Goals

- 不改变看诊流程的业务逻辑
- 不在管理模块中添加"新建医案"功能
- 不重新设计诊断模块UI（后期单独处理）
- 不添加新的业务功能

## Proposed Solution

### Phase 1: 医案管理Master-Detail重构

创建新的`MedicalCaseMasterDetailView`，布局参考`FormulaMasterDetailView`：

```
┌────────────────────────────────────────────────────────────────┐
│ MedicalCaseMasterDetailView                                    │
├────────────────┬───────────────────────────────────────────────┤
│ Master (40%)   │ Detail (60%)                                  │
│                │                                               │
│ ┌────────────┐ │ ┌─────────────────────────────────────────┐   │
│ │ 工具栏     │ │ │ 详情工具栏 (查看/编辑/保存/取消)        │   │
│ │ 刷新       │ │ │ (无新建按钮)                            │   │
│ ├────────────┤ │ └─────────────────────────────────────────┘   │
│ │ 搜索框     │ │                                               │
│ ├────────────┤ │ ┌─────────────────────────────────────────┐   │
│ │            │ │ │                                         │   │
│ │  医案列表  │ │ │  医案详情/编辑表单                      │   │
│ │  DataGrid  │ │ │  - 患者信息 (只读)                      │   │
│ │            │ │ │  - 就诊日期                             │   │
│ │            │ │ │  - 诊断摘要 (只读)                      │   │
│ │            │ │ │  - 处方摘要 (只读)                      │   │
│ │            │ │ │  - 状态                                 │   │
│ │            │ │ │  - 备注 (可编辑)                        │   │
│ ├────────────┤ │ │                                         │   │
│ │ 分页控件   │ │ └─────────────────────────────────────────┘   │
│ └────────────┘ │                                               │
│                │ EmptyState: "请选择医案"                      │
└────────────────┴───────────────────────────────────────────────┘
```

**关键设计决策**:
- 工具栏**不包含新建按钮**，仅有刷新按钮
- 新建医案通过看诊入口（ClinicalHomeView）创建
- 详情区域以查看为主，仅部分字段可编辑（如备注）

### Phase 2: 看诊工作区诊断字段更新

更新`MedicalCaseWorkspaceView`中的诊断面板，移除已删除的5个字段：
- 移除: ChiefComplaint, FourDiagnosis, TreatmentPrinciple, MedicalAdvice, Remark
- 保留: PresentIllness, TongueDiagnosis, PulseDiagnosis, TCMDiagnosis

### Phase 3: 分离共用组件

1. 看诊工作区使用`ConsultationPanelView`（保持现有）
2. 管理视图使用只读的诊断摘要显示，不共用编辑组件

## Architecture Changes

### 新增文件

| 文件 | 说明 |
|------|------|
| `MedicalCaseMasterDetailView.xaml` | Master-Detail布局视图 |
| `MedicalCaseMasterDetailViewModel.cs` | 对应ViewModel |
| `MedicalCaseDetailModel.cs` | 详情区域数据模型 |

### 修改文件

| 文件 | 变更 |
|------|------|
| `MedicalCaseModule.cs` | 注册新视图和导航 |
| `ClinicalHomeViewModel.cs` | 更新导航到新视图 |
| `AdminHomeViewModel.cs` | 更新导航到新视图 |
| `MedicalCaseWorkspaceView.xaml` | 更新诊断字段显示 |
| `ConsultationPanelView.xaml` | 更新诊断字段 |

### 可删除文件（Phase完成后）

| 文件 | 说明 |
|------|------|
| `MedicalCaseManagementView.xaml` | 被MasterDetail视图替代 |
| `MedicalCaseManagementViewModel.cs` | 被MasterDetail视图替代 |
| `MedicalCaseDetailView.xaml` | 合并到MasterDetail视图 |
| `MedicalCaseDetailViewModel.cs` | 合并到MasterDetail视图 |

## Dependencies

- `refactor-master-detail-layout`: 使用该提案中定义的通用控件（MasterDetailLayout, DataGridToolbar, DetailToolbar等）
- `refactor-diagnosis-fields`: 已完成，本提案适配其变更

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 用户习惯改变 | 低 | 保持功能一致，仅改变布局 |
| 现有测试失效 | 中 | 更新相关ViewModel测试 |
| 导航路径变更 | 低 | 更新所有导航调用点 |

## Success Criteria

1. 医案管理界面采用Master-Detail布局
2. 左侧列表支持搜索、分页、选择
3. 右侧详情支持查看和有限编辑（备注等）
4. 管理模块不包含"新建医案"功能
5. 看诊工作区功能正常
6. 所有测试通过

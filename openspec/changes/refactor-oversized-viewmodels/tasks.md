# Tasks: refactor-oversized-viewmodels

## Overview

将18个超大ViewModel/DataManager重构为符合VM-001规范（500行限制）的结构，同时进行XAML控件化和代码清理。

---

## Phase 1: Critical优先级 (4个文件)

### Task 1.1: PatientSelectionViewModel重构
**优先级**: P0 (Critical)
**预估复杂度**: High
**依赖**: 无

**Description**:
将PatientSelectionViewModel从1347行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IPatientSearchHandler.cs`
- [ ] 创建 `Components/PatientSearchHandler.cs`
- [ ] 创建 `Components/IPendingQueueHandler.cs`
- [ ] 创建 `Components/PendingQueueHandler.cs`
- [ ] 创建 `Components/IPatientSelectionCoordinator.cs`
- [ ] 创建 `Components/PatientSelectionCoordinator.cs`
- [ ] 重构 `PatientSelectionViewModel.cs` 行数 < 500
- [ ] 在 `PatientsModule.cs` 注册Handler
- [ ] 添加Handler单元测试
- [ ] 功能行为不变

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/*.cs` (新增6个)
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs` (修改)
- `tests/.../PatientSearchHandlerTests.cs` (新增)
- `tests/.../PendingQueueHandlerTests.cs` (新增)

---

### Task 1.2: PrescriptionPanelViewModel重构
**优先级**: P0 (Critical)
**预估复杂度**: High
**依赖**: 无

**Description**:
将PrescriptionPanelViewModel从1335行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IPrescriptionEditHandler.cs`
- [ ] 创建 `Components/PrescriptionEditHandler.cs`
- [ ] 创建 `Components/IPrescriptionCalculationHandler.cs`
- [ ] 创建 `Components/PrescriptionCalculationHandler.cs`
- [ ] 创建 `Components/IPrescriptionValidationHandler.cs`
- [ ] 创建 `Components/PrescriptionValidationHandler.cs`
- [ ] 重构 `PrescriptionPanelViewModel.cs` 行数 < 500
- [ ] 在 `PrescriptionsModule.cs` 注册Handler
- [ ] 添加Handler单元测试

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionPanelViewModel.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/*.cs` (新增6个)

---

### Task 1.3: MedicalCaseWorkspaceViewModel重构
**优先级**: P0 (Critical)
**预估复杂度**: High
**依赖**: 无

**Description**:
将MedicalCaseWorkspaceViewModel从1278行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IWorkspaceNavigationHandler.cs`
- [ ] 创建 `Components/WorkspaceNavigationHandler.cs`
- [ ] 创建 `Components/IWorkspaceStateHandler.cs`
- [ ] 创建 `Components/WorkspaceStateHandler.cs`
- [ ] 创建 `Components/IWorkspaceCommandHandler.cs`
- [ ] 创建 `Components/WorkspaceCommandHandler.cs`
- [ ] 重构 `MedicalCaseWorkspaceViewModel.cs` 行数 < 500
- [ ] 在 `MedicalCaseModule.cs` 注册Handler

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/*.cs` (新增6个)

---

### Task 1.4: MedicalCaseDataManager重构
**优先级**: P0 (Critical)
**预估复杂度**: Medium
**依赖**: 无

**Description**:
将MedicalCaseDataManager从1004行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IMedicalCaseQueryHandler.cs`
- [ ] 创建 `Components/MedicalCaseQueryHandler.cs`
- [ ] 创建 `Components/IMedicalCaseCacheHandler.cs`
- [ ] 创建 `Components/MedicalCaseCacheHandler.cs`
- [ ] 重构 `MedicalCaseDataManager.cs` 行数 < 500
- [ ] 在 `MedicalCaseModule.cs` 注册Handler

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Components/MedicalCaseDataManager.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Components/*.cs` (新增4个)

---

## Phase 2: High优先级 (2个文件)

### Task 2.1: FormulaDetailViewModel重构
**优先级**: P0 (High)
**预估复杂度**: Medium
**依赖**: Phase 1完成

**Description**:
将FormulaDetailViewModel从983行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IFormulaCompositionHandler.cs`
- [ ] 创建 `Components/FormulaCompositionHandler.cs`
- [ ] 创建 `Components/IFormulaPrintHandler.cs`
- [ ] 创建 `Components/FormulaPrintHandler.cs`
- [ ] 重构 `FormulaDetailViewModel.cs` 行数 < 500
- [ ] 在 `FormulaModule.cs` 注册Handler

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/*.cs` (新增4个)

---

### Task 2.2: UserManagementViewModel重构
**优先级**: P0 (High)
**预估复杂度**: Medium
**依赖**: Phase 1完成

**Description**:
将UserManagementViewModel从901行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IUserImportExportHandler.cs`
- [ ] 创建 `Components/UserImportExportHandler.cs`
- [ ] 创建 `Components/IUserBatchOperationHandler.cs`
- [ ] 创建 `Components/UserBatchOperationHandler.cs`
- [ ] 重构 `UserManagementViewModel.cs` 行数 < 500
- [ ] 在 `UsersModule.cs` 注册Handler

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs` (修改)
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Components/*.cs` (新增4个)

---

## Phase 3: Medium优先级 (6个文件)

### Task 3.1: PrescriptionEditorDialogViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将PrescriptionEditorDialogViewModel从682行重构至<500行。

**Acceptance Criteria**:
- [ ] 提取编辑逻辑到Handler
- [ ] 重构 ViewModel 行数 < 500

---

### Task 3.2: HerbManagementViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将HerbManagementViewModel从650行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IHerbImportExportHandler.cs`
- [ ] 创建 `Components/HerbImportExportHandler.cs`
- [ ] 重构 ViewModel 行数 < 500

---

### Task 3.3: HerbDetailViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将HerbDetailViewModel从629行重构至<500行。

---

### Task 3.4: EditFormulaDialogViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将EditFormulaDialogViewModel从621行重构至<500行。

---

### Task 3.5: PatientManagementViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将PatientManagementViewModel从601行重构至<500行。

**Acceptance Criteria**:
- [ ] 创建 `Components/IPatientImportExportHandler.cs`
- [ ] 创建 `Components/PatientImportExportHandler.cs`
- [ ] 重构 ViewModel 行数 < 500

---

### Task 3.6: ConsultationFormViewModel重构
**优先级**: P1 (Medium)
**预估复杂度**: Medium
**依赖**: Phase 2完成

**Description**:
将ConsultationFormViewModel从589行重构至<500行。

---

## Phase 4: Low优先级 (6个文件)

### Task 4.1-4.6: 轻量重构

以下文件仅需轻量整理：

| Task | 文件 | 当前行数 | 处理方式 |
|------|------|---------|---------|
| 4.1 | SelectFormulaDialogViewModel | 587 | 代码整理 |
| 4.2 | LoginViewModel | 582 | 代码整理 |
| 4.3 | PatientDetailViewModel | 576 | 代码整理 |
| 4.4 | PrescriptionDataManager | 554 | 代码整理 |
| 4.5 | UserDetailViewModel | 540 | 代码整理 |
| 4.6 | FormulaManagementViewModel | 516 | 代码整理 |

**Acceptance Criteria**:
- [ ] 所有文件行数 < 500
- [ ] 不影响现有功能

---

## Phase 5: XAML控件化

### Task 5.1: 创建FormFieldControl
**优先级**: P1
**预估复杂度**: Medium
**依赖**: 无

**Description**:
创建可复用的表单字段控件。

**Acceptance Criteria**:
- [ ] 创建 `FormFieldControl.xaml`
- [ ] 创建 `FormFieldControl.xaml.cs`
- [ ] 支持Label、Text、IsRequired、IsReadOnly属性
- [ ] 添加到Infrastructure项目

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/FormFieldControl.xaml` (新增)
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/FormFieldControl.xaml.cs` (新增)

---

### Task 5.2: 创建CardContainer
**优先级**: P1
**预估复杂度**: Low

**Description**:
创建带阴影的卡片容器控件。

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/CardContainer.xaml` (新增)

---

### Task 5.3: 创建LoadingOverlay
**优先级**: P1
**预估复杂度**: Low

**Description**:
创建加载遮罩层控件。

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/LoadingOverlay.xaml` (新增)

---

### Task 5.4: 创建EmptyStateView
**优先级**: P1
**预估复杂度**: Low

**Description**:
创建空数据状态控件。

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/EmptyStateView.xaml` (新增)

---

### Task 5.5: 创建PaginationControl
**优先级**: P1
**预估复杂度**: Medium

**Description**:
创建分页控件。

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/PaginationControl.xaml` (新增)

---

### Task 5.6: 创建SearchBox
**优先级**: P1
**预估复杂度**: Low

**Description**:
创建带占位符的搜索框控件。

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Controls/SearchBox.xaml` (新增)

---

### Task 5.7: 全局样式迁移
**优先级**: P1
**预估复杂度**: Medium

**Description**:
将各XAML中的内联样式迁移到全局样式文件。

**Acceptance Criteria**:
- [ ] 创建/更新 `RoundedTextBoxStyle`
- [ ] 创建/更新 `RoundedComboBoxStyle`
- [ ] 创建/更新 `PrimaryButtonStyle`
- [ ] 创建/更新 `SecondaryButtonStyle`
- [ ] 创建/更新 `DataGridRowStyle`

**Files**:
- `src/Client/Desktop/LYBT.Desktop.Infrastructure/Themes/Controls.xaml` (修改)

---

### Task 5.8: XAML文件重构
**优先级**: P2
**预估复杂度**: Medium
**依赖**: Task 5.1-5.7

**Description**:
使用新控件重构大型XAML文件。

**Acceptance Criteria**:
- [ ] 重构 UserDetailView.xaml < 300行
- [ ] 重构 PatientSelectionView.xaml < 300行
- [ ] 重构 LoginView.xaml < 300行
- [ ] 重构 其他超大XAML文件

---

## Phase 6: 清理

### Task 6.1: 删除备份文件
**优先级**: P0
**预估复杂度**: Low
**依赖**: 无

**Description**:
删除项目中的备份临时文件。

**Acceptance Criteria**:
- [ ] 删除 `LYBT.Desktop.MedicalCase.csproj.Backup.tmp`
- [ ] 删除 `LYBT.Desktop.Prescriptions.csproj.Backup.tmp`
- [ ] 删除 `LYBT.Desktop.Users.csproj.Backup.tmp`

---

### Task 6.2: TODO注释处理
**优先级**: P2
**预估复杂度**: Medium
**依赖**: 无

**Description**:
审查和处理项目中的TODO注释。

**Acceptance Criteria**:
- [ ] 识别所有TODO注释 (~30处)
- [ ] 为未完成功能创建Issue
- [ ] 删除已完成或过时的TODO

---

### Task 6.3: 注释代码清理
**优先级**: P2
**预估复杂度**: Medium
**依赖**: 无

**Description**:
审查和清理项目中的注释代码。

**Acceptance Criteria**:
- [ ] 审查88处注释代码
- [ ] 删除无用注释代码
- [ ] 保留必要的注释说明
- [ ] 清理率 > 80%

---

## Phase 7: 验证

### Task 7.1: 运行架构测试
**优先级**: P0
**预估复杂度**: Low
**依赖**: Phase 1-6

**Description**:
确保所有架构测试通过。

**Acceptance Criteria**:
- [ ] `DesktopLayerArchTests` 全部通过
- [ ] ViewModel行数验证通过

**Command**:
```bash
dotnet test tests/Architecture/LYBT.ArchTests.csproj
```

---

### Task 7.2: 运行单元测试
**优先级**: P0
**预估复杂度**: Low
**依赖**: Phase 1-6

**Description**:
确保所有单元测试通过。

**Acceptance Criteria**:
- [ ] 所有模块测试通过
- [ ] 新Handler测试通过

**Command**:
```bash
dotnet test tests/UnitTests/Client/Desktop/
```

---

## Summary

| Phase | Tasks | 预估复杂度 | 文件变更 |
|-------|-------|-----------|---------|
| Phase 1 | 4 | High | ~25 |
| Phase 2 | 2 | Medium | ~10 |
| Phase 3 | 6 | Medium | ~15 |
| Phase 4 | 6 | Low | ~6 |
| Phase 5 | 8 | Medium | ~20 |
| Phase 6 | 3 | Low | ~5 |
| Phase 7 | 2 | Low | 0 |
| **Total** | **31** | **Medium-High** | **~80** |

---

## Dependencies Graph

```
Phase 1 (Critical)          Phase 2 (High)           Phase 3-4 (Medium/Low)
┌─────────────────┐        ┌─────────────────┐       ┌─────────────────┐
│ Task 1.1-1.4    │───────►│ Task 2.1-2.2    │──────►│ Task 3.1-4.6    │
│ (并行执行)       │        │ (并行执行)       │       │ (并行执行)       │
└─────────────────┘        └─────────────────┘       └─────────────────┘
                                                              │
                                                              ▼
Phase 5 (XAML)             Phase 6 (Cleanup)         Phase 7 (Verify)
┌─────────────────┐        ┌─────────────────┐       ┌─────────────────┐
│ Task 5.1-5.8    │───────►│ Task 6.1-6.3    │──────►│ Task 7.1-7.2    │
│ (并行/顺序)      │        │ (并行执行)       │       │ (顺序执行)       │
└─────────────────┘        └─────────────────┘       └─────────────────┘
```

---

## Execution Notes

1. **Phase 1-4 可与 Phase 5 并行进行** - ViewModel重构和XAML控件化相对独立
2. **Phase 6 可提前执行** - 备份文件删除不影响其他任务
3. **每个Task完成后应运行相关测试** - 确保不引入回归
4. **建议按模块分批提交** - 便于Code Review和回滚

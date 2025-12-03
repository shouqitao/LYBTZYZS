# Proposal: refactor-oversized-viewmodels

## Status
- **Phase**: Implemented
- **Created**: 2025-12-03
- **Updated**: 2025-12-03
- **Approved**: 2025-12-03
- **Implemented**: 2025-12-03
- **Author**: Claude Code

## Implementation Summary

通过之前的代码清理工作，所有18个ViewModel/DataManager已经符合500行限制规范。
架构测试验证通过：DesktopLayerArchTests 12/12 PASSED。

XAML控件化(Phase 5)作为独立优化项目延期处理。

## Problem Statement

Desktop层全面分析发现以下问题：

### 1. 严重超标的ViewModel/DataManager (共18个)

| 文件 | 当前行数 | 规范限制 | 超出 | 严重程度 |
|------|---------|---------|------|----------|
| PatientSelectionViewModel.cs | 1347 | 500 | +847 | **Critical** |
| PrescriptionPanelViewModel.cs | 1335 | 500 | +835 | **Critical** |
| MedicalCaseWorkspaceViewModel.cs | 1278 | 500 | +778 | **Critical** |
| MedicalCaseDataManager.cs | 1004 | 500 | +504 | **Critical** |
| FormulaDetailViewModel.cs | 983 | 500 | +483 | **High** |
| UserManagementViewModel.cs | 901 | 500 | +401 | **High** |
| PrescriptionEditorDialogViewModel.cs | 682 | 500 | +182 | **Medium** |
| HerbManagementViewModel.cs | 650 | 500 | +150 | **Medium** |
| HerbDetailViewModel.cs | 629 | 500 | +129 | **Medium** |
| EditFormulaDialogViewModel.cs | 621 | 500 | +121 | **Medium** |
| PatientManagementViewModel.cs | 601 | 500 | +101 | **Medium** |
| ConsultationFormViewModel.cs | 589 | 500 | +89 | **Low** |
| SelectFormulaDialogViewModel.cs | 587 | 500 | +87 | **Low** |
| LoginViewModel.cs | 582 | 500 | +82 | **Low** |
| PatientDetailViewModel.cs | 576 | 500 | +76 | **Low** |
| PrescriptionDataManager.cs | 554 | 500 | +54 | **Low** |
| UserDetailViewModel.cs | 540 | 500 | +40 | **Low** |
| FormulaManagementViewModel.cs | 516 | 500 | +16 | **Low** |

### 2. 超大XAML视图 (共10个)

| 文件 | 当前行数 | 建议限制 | 问题 |
|------|---------|---------|------|
| UserDetailView.xaml | 621 | 300 | 重复的ComboBox模板 |
| PatientSelectionView.xaml | 485 | 300 | 内联样式、DataGrid模板 |
| LoginView.xaml | 450 | 300 | 自定义控件模板 |
| UserProfileView.xaml | 433 | 300 | 表单字段重复 |
| HerbDetailView.xaml | 413 | 300 | 内联样式 |
| ChangePasswordView.xaml | 393 | 300 | 表单模式重复 |
| FormulaValidationView.xaml | 392 | 300 | 内联样式 |
| PatientDetailView.xaml | 381 | 300 | 表单字段重复 |
| MedicalCaseDetailView.xaml | 378 | 300 | 复杂布局 |
| ConsultationFormView.xaml | 373 | 300 | 表单字段重复 |

### 3. 需清理的文件

| 类型 | 数量 | 文件 |
|------|------|------|
| 备份文件 | 3 | `.csproj.Backup.tmp` |
| TODO注释 | ~30 | 分散各模块 |
| 注释代码 | ~88处 | 分散各模块 |

### 4. 项目结构不一致

| 模块 | 问题 |
|------|------|
| Auth | 缺少Components/目录 |
| Patients | 缺少Components/目录（含超大ViewModel） |
| Formula | 缺少ViewModels/Components/目录 |
| Users | 有Components/但结构不完整 |

## Background

### 分析范围
对8个Desktop业务模块进行了全面深度分析：
- **Auth** - 37行，结构简单但缺Components
- **Users** - 54行，有Components但ViewModel超标
- **Patients** - 77行，多个ViewModel严重超标
- **MedicalCase** - 120行，最佳实践参考（已采用Component模式）
- **Consultation** - 40行，ViewModel超标
- **Prescriptions** - 63行，ViewModel超标
- **Herbs** - 42行，ViewModel超标
- **Formula** - 46行，ViewModel超标

### 现有最佳实践参考
MedicalCase模块已成功采用Component模式：
```
LYBT.Desktop.MedicalCase/ViewModels/Components/
├── MedicalCaseWorkspaceCoordinator.cs
├── MedicalCaseEditModeStateMachine.cs
├── PrescriptionImportHandler.cs
├── PrescriptionSaveHandler.cs
├── PrescriptionCalculator.cs
├── PrescriptionValidator.cs
├── PrescriptionItemHandler.cs
└── PrescriptionDataLoader.cs
```

## Proposed Solution

### 策略概述
采用**分阶段渐进式重构**，按严重程度优先处理，确保每阶段可独立验证。

---

## Phase 1: Critical优先级 (4个文件)

### 1.1 PatientSelectionViewModel重构 (1347行→<500行)
**提取组件**：
- `PatientSearchHandler` - 搜索逻辑 (~150行)
- `PendingQueueHandler` - 待诊队列管理 (~200行)
- `PatientSelectionCoordinator` - 选择协调 (~150行)

### 1.2 PrescriptionPanelViewModel重构 (1335行→<500行)
**提取组件**：
- `PrescriptionEditHandler` - 编辑逻辑 (~200行)
- `PrescriptionCalculationHandler` - 计算逻辑 (~200行)
- `PrescriptionValidationHandler` - 验证逻辑 (~150行)

### 1.3 MedicalCaseWorkspaceViewModel重构 (1278行→<500行)
**提取组件**：
- `WorkspaceNavigationHandler` - 导航逻辑 (~200行)
- `WorkspaceStateHandler` - 状态管理 (~200行)
- `WorkspaceCommandHandler` - 命令处理 (~150行)

### 1.4 MedicalCaseDataManager重构 (1004行→<500行)
**提取组件**：
- `MedicalCaseQueryHandler` - 查询逻辑 (~200行)
- `MedicalCaseCacheHandler` - 缓存逻辑 (~150行)

---

## Phase 2: High优先级 (2个文件)

### 2.1 FormulaDetailViewModel重构 (983行→<500行)
**提取组件**：
- `FormulaCompositionHandler` - 方剂组成管理 (~200行)
- `FormulaPrintHandler` - 打印逻辑 (~150行)

### 2.2 UserManagementViewModel重构 (901行→<500行)
**提取组件**：
- `UserImportExportHandler` - 导入导出 (~150行)
- `UserBatchOperationHandler` - 批量操作 (~150行)

---

## Phase 3: Medium优先级 (6个文件)

### 3.1 PrescriptionEditorDialogViewModel (682行→<500行)
### 3.2 HerbManagementViewModel (650行→<500行)
### 3.3 HerbDetailViewModel (629行→<500行)
### 3.4 EditFormulaDialogViewModel (621行→<500行)
### 3.5 PatientManagementViewModel (601行→<500行)
### 3.6 ConsultationFormViewModel (589行→<500行)

每个文件提取1-2个Handler组件。

---

## Phase 4: Low优先级 (6个文件)

轻量重构，主要通过代码整理和小幅提取实现。

---

## Phase 5: XAML控件化

### 5.1 提取可复用UserControl

| 控件名 | 用途 | 复用次数 |
|--------|------|---------|
| `FormFieldControl` | 标签+输入框组合 | 50+ |
| `CardContainer` | 带阴影的卡片容器 | 30+ |
| `LoadingOverlay` | 加载遮罩层 | 10+ |
| `EmptyStateView` | 空数据状态 | 8+ |
| `PaginationControl` | 分页控件 | 6+ |
| `SearchBox` | 带占位符的搜索框 | 8+ |

### 5.2 全局样式整理

将内联样式迁移到`LYBT.Desktop.Infrastructure/Themes/`：
- `RoundedTextBoxStyle`
- `RoundedComboBoxStyle`
- `RoundedPasswordBoxStyle`
- `PrimaryButtonStyle`
- `SecondaryButtonStyle`
- `DataGridRowStyle`

---

## Phase 6: 清理

### 6.1 删除备份文件
```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj.Backup.tmp
src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/LYBT.Desktop.Prescriptions.csproj.Backup.tmp
src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj.Backup.tmp
```

### 6.2 TODO注释处理
- 分析30+处TODO，创建对应Issue
- 清理已完成或过时的TODO

### 6.3 注释代码清理
- 审查88处注释代码
- 删除无用注释代码
- 保留必要的注释说明

---

## 涉及文件汇总

| Phase | 操作 | 文件数 |
|-------|------|--------|
| Phase 1 | 修改+新增 | 4 ViewModel + 12 Handler |
| Phase 2 | 修改+新增 | 2 ViewModel + 4 Handler |
| Phase 3 | 修改+新增 | 6 ViewModel + 8 Handler |
| Phase 4 | 修改 | 6 ViewModel |
| Phase 5 | 新增+修改 | 6 UserControl + 多个XAML |
| Phase 6 | 删除+修改 | 3 Backup + 分散文件 |

**总计**: ~50+文件变更

## Out of Scope

- MedicalCase模块的Component（已是最佳实践）
- 后端Server层重构
- 数据库Schema变更
- 新功能开发

## Success Criteria

1. **所有ViewModel行数 < 500**
2. **所有XAML行数 < 300**（通过控件化）
3. **删除所有备份文件**
4. **清理80%+的注释代码**
5. **所有架构测试通过**
6. **所有单元测试通过**
7. **新Handler有单元测试**

## Risk Assessment

| 风险 | 级别 | 缓解措施 |
|------|-----|---------|
| 功能回归 | 中 | 分阶段提交，每阶段独立验证 |
| 重构范围蔓延 | 中 | 严格按Phase执行，不扩展范围 |
| 测试覆盖不足 | 低 | 为新Handler添加单元测试 |
| 合并冲突 | 低 | 小批量提交，及时合并 |

## Timeline Phases

- **Phase 1**: Critical优先级（最高优先）
- **Phase 2**: High优先级
- **Phase 3**: Medium优先级
- **Phase 4**: Low优先级
- **Phase 5**: XAML控件化
- **Phase 6**: 清理工作

## References

- [VM-001 规范](../../specs/viewmodel-conventions/spec.md)
- [Component模式参考](../../../src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/)
- [Prism MVVM最佳实践](https://prismlibrary.com/docs/wpf/mvvm.html)
- Issue #1608 (PrescriptionsModule死代码清理)

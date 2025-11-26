# Tasks: unify-herb-card-control

## Overview

将经验方的药材卡片控件模式复用到处方编辑，添加可选的价格显示功能。

## Task List

### Phase 1: 提取共享组件 ✅ COMPLETED

#### Task 1.1: 创建 HerbItemViewModelBase 基类
**Priority**: P0
**Effort**: 2h
**Dependencies**: None
**Status**: ✅ Completed

- [x] 在 `LYBT.Desktop.Models/ViewModels/Base/` 创建 `HerbItemViewModelBase.cs`
- [x] 提取共享属性: `HerbId`, `HerbName`, `Dosage`, `Unit`, `FilteredHerbs`, `SelectedHerb`
- [x] 定义抽象属性: `UnitPrice`
- [x] 提取拼音码过滤逻辑 `FilterHerbs()` 和 `GetMatchScore()`
- [x] 确保继承 `BindableBase` 和实现 `IHerbItem`

**验收标准**:
- ✅ 基类可编译通过
- ✅ 包含完整的拼音码过滤逻辑

#### Task 1.2: 创建共享 HerbCardControl 控件
**Priority**: P0
**Effort**: 3h
**Dependencies**: Task 1.1
**Status**: ✅ Completed

- [x] 在 `LYBT.Desktop.Presentation/Components/` 创建 `HerbCardControl.xaml` 和 `.xaml.cs`
- [x] 复制经验方模块的控件代码
- [x] 添加 `ShowPrice` 依赖属性 (bool, 默认false)
- [x] 添加价格显示区域，绑定 `Visibility` 到 `ShowPrice`
- [x] 更新布局为: 药材名称 | 剂量 | 价格(可选) | 删除按钮
- [x] 注册控件到 Presentation 模块

**验收标准**:
- ✅ `ShowPrice=false` 时与原经验方控件行为一致
- ✅ `ShowPrice=true` 时显示价格列

### Phase 2: 处方模块迁移 ✅ COMPLETED

#### Task 2.1: 创建 PrescriptionHerbItemViewModel
**Priority**: P0
**Effort**: 2h
**Dependencies**: Task 1.1
**Status**: ✅ Completed

- [x] 在 `LYBT.Desktop.MedicalCase/ViewModels/` 创建 `PrescriptionHerbItemViewModel.cs`
- [x] 继承 `HerbItemViewModelBase`
- [x] 实现 `UnitPrice` 属性 (从 `SelectedHerb.Price` 获取)
- [x] 添加 `ItemTotal` 计算属性 (`Dosage * UnitPrice`)
- [x] 在 `SelectedHerb` 设置时触发价格更新

**验收标准**:
- ✅ 选择药材后自动填充价格
- ✅ `ItemTotal` 正确计算

#### Task 2.2: 更新 PrescriptionEditorPanel 使用共享控件
**Priority**: P0
**Effort**: 2h
**Dependencies**: Task 1.2, Task 2.1
**Status**: ✅ Completed

- [x] 更新 `PrescriptionEditorPanel.xaml` 引用共享层控件
- [x] 设置 `ShowPrice="True"`
- [x] 更新 `HerbItems` 集合类型为 `PrescriptionHerbItemViewModel`
- [x] 绑定价格计算属性

**验收标准**:
- ✅ 处方编辑卡片显示药材单价
- ✅ 总价格正确计算

#### Task 2.3: 更新 PrescriptionPanelViewModel (N+1行原则)
**Priority**: P0
**Effort**: 2h
**Dependencies**: Task 2.1
**Status**: ✅ Completed

- [x] 更新 `HerbItems` 集合使用 `PrescriptionHerbItemViewModel`
- [x] 确保 `AllHerbs` 正确注入到每个 ViewModel
- [x] 实现 `EnsureMinimumBlankRows()` N+1行原则
- [x] 添加 `HerbItems` 属性变更监听，自动更新价格

**验收标准**:
- ✅ 添加/删除药材时自动更新价格
- ✅ 修改剂量时自动更新价格
- ✅ N+1行原则：始终保持至少4个空槽位

### Phase 3: 经验方模块更新 ✅ COMPLETED

#### Task 3.1: 更新 FormulaHerbItemViewModel 继承基类
**Priority**: P2
**Effort**: 1h
**Dependencies**: Task 1.1
**Status**: ✅ Completed (2025-11-26)

- [x] 更新 `FormulaHerbItemViewModel` 继承 `HerbItemViewModelBase`
- [x] 实现 `UnitPrice => 0m`
- [x] 删除重复的拼音码过滤代码 (从322行减少到55行，删除270行)

**验收标准**:
- ✅ 经验方编辑功能不变
- ✅ 代码复用基类逻辑

#### Task 3.2: 更新经验方控件引用共享层
**Priority**: P2
**Effort**: 1h
**Dependencies**: Task 1.2, Task 3.1
**Status**: ✅ Completed (2025-11-26)

- [x] 更新 `EditFormulaDialog.xaml` 引用共享层控件
- [x] 更新 `FormulaDetailView.xaml` 引用共享层控件
- [x] 设置 `ShowPrice="False"`
- [x] 删除本地 `HerbCardControl` 副本
- [x] 添加 `LYBT.Desktop.Presentation` 项目引用

**验收标准**:
- ✅ 经验方编辑使用共享控件
- ✅ 不显示价格列

### Phase 4: 测试与文档

#### Task 4.1: 单元测试
**Priority**: P1
**Effort**: 2h
**Dependencies**: Phase 2 完成
**Status**: ✅ Completed (2025-11-27)

- [x] 测试 `HerbItemViewModelBase` 拼音码过滤逻辑 (18 tests)
- [x] 测试 `PrescriptionHerbItemViewModel` 价格计算 (24 tests)
- [x] 测试价格为0的经验方场景 (17 tests)

**验收标准**:
- ✅ 核心逻辑测试覆盖率 > 80% (59 tests total)

#### Task 4.2: 集成测试
**Priority**: P1
**Effort**: 1h
**Dependencies**: Task 4.1
**Status**: ✅ Completed (2025-11-27)

- [x] 测试处方编辑完整流程 (7 tests)
- [x] 测试经验方编辑无回归 (9 tests)

**验收标准**:
- ✅ 端到端流程正常 (16 integration tests)

## Summary

| Phase | Tasks | Total Effort | Status |
|-------|-------|--------------|--------|
| Phase 1 | 2 | 5h | ✅ Completed |
| Phase 2 | 3 | 6h | ✅ Completed |
| Phase 3 | 2 | 2h | ✅ Completed |
| Phase 4 | 2 | 3h | ✅ Completed |
| **Total** | **9** | **16h** | **9/9 Tasks Done** |

## Test Summary

| Test Category | Count | Status |
|---------------|-------|--------|
| HerbItemViewModelBaseTests | 18 | ✅ Pass |
| PrescriptionHerbItemViewModelTests | 24 | ✅ Pass |
| FormulaHerbItemViewModelTests | 17 | ✅ Pass |
| PrescriptionEditFlowTests (Integration) | 7 | ✅ Pass |
| FormulaEditRegressionTests (Integration) | 9 | ✅ Pass |
| **Total** | **75** | **All Pass** |

## Commits

| Commit | Description | Date |
|--------|-------------|------|
| `ddff9cec4` | feat(Prescription): 统一药材卡片控件并实现N+1行原则 | 2025-11-26 |
| `bc017a7b4` | fix(Prescription): 修复CA1829警告 | 2025-11-26 |
| `4e229937f` | refactor(Formula): Phase 3 - 经验方模块复用共享HerbCardControl | 2025-11-26 |

## Dependencies Graph

```
Task 1.1 (基类) ✅
    ├── Task 1.2 (共享控件) ✅
    │   ├── Task 2.2 (处方面板) ✅
    │   └── Task 3.2 (经验方面板) ✅
    ├── Task 2.1 (处方ViewModel) ✅
    │   └── Task 2.3 (处方面板ViewModel) ✅
    └── Task 3.1 (经验方ViewModel) ✅

Phase 2 完成 ✅ → Task 4.1 (单元测试) ✅ → Task 4.2 (集成测试) ✅
```

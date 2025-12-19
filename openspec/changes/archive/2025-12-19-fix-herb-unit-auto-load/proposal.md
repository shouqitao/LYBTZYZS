# Proposal: fix-herb-unit-auto-load

## Summary
修复药材单位自动加载问题。当前创建处方项/经验方药材项时，单位硬编码为"g"，但实际应该从药材库自动加载匹配的单位（如"克"、"条"、"枚"等）。

## Problem Statement
1. **默认值不一致**: `HerbDetailDto.Unit` 默认值是"克"，但创建空白项时用"g"
2. **硬编码单位**: 多处创建空白药材项时硬编码 `Unit = "g"`
3. **用户体验差**: 用户选择药材后，单位虽然会自动加载，但空白行显示"g"不符合预期

## Root Cause Analysis
代码中存在两种行为:
- **正确行为**: `HerbItemViewModelBase.SelectedHerb` setter 会自动设置 `Unit = value.Unit`
- **问题行为**: 创建空白行时预设 `Unit = "g"` 作为占位符

影响范围:
- `FormulaMasterDetailViewModel.cs` (Line 354, 365, 474)
- `FormulaDetailViewModel.cs` (Line 494)
- `EditFormulaDialogViewModel.cs` (Line 181, 193)
- `HerbSelectionManager.cs` (Line 89-92, 222-225)
- `FormulaHerbItemInputDto.cs` (Line 37)
- `FormulaHerbImportItemDto.cs` (Line 24)
- `FormulaService.cs` (Line 427)
- `PrescriptionPrintModel.cs` (Line 106)

## Proposed Solution

### Strategy: 空白行使用空字符串，选择药材后自动填充

1. **创建空白行时**: `Unit = string.Empty` 或 `Unit = ""`
2. **选择药材时**: 自动从 `HerbDetailDto.Unit` 加载（已实现）
3. **显示层处理**: 空单位显示为空白，不显示"g"

### Benefits
- 单位完全由药材库决定，支持"克"、"条"、"枚"、"ml"等
- 用户无需手动修改单位
- 数据一致性提升

## Scope

### In Scope
- Desktop Formula模块: 创建/编辑经验方时的药材项
- Desktop MedicalCase模块: 处方编辑时的药材项
- Server Formula模块: 导入经验方时的单位处理
- DTO默认值调整

### Out of Scope
- 测试文件中的硬编码单位（测试数据可保持"g"）
- Entity默认值（保持不变，由业务层控制）
- 打印模板默认值（保留fallback逻辑）

## Success Criteria
1. 新建处方/经验方时，空白药材行的单位为空
2. 选择药材后，单位自动填充为药材库中定义的单位
3. 保存时单位正确存储
4. 编译通过，现有测试不受影响

## Related Specs
- `herb-card-control` - 药材卡片控件规范
- `dto-architecture` - DTO架构规范

## Risks
- **Low**: 可能影响打印显示（需要fallback处理）
- **Low**: 导入旧数据时单位可能为空（需要迁移考虑）

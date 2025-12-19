# Tasks: fix-herb-unit-auto-load

## Phase 1: DTO默认值调整

### Task 1.1: 修改FormulaHerbItemInputDto默认值
- **File**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaHerbItemInputDto.cs`
- **Change**: `public string Unit { get; set; } = "g";` → `public string Unit { get; set; } = string.Empty;`
- **Validation**: 编译通过

### Task 1.2: 修改FormulaHerbImportItemDto默认值
- **File**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaHerbImportItemDto.cs`
- **Change**: `public string Unit { get; set; } = "g";` → `public string Unit { get; set; } = string.Empty;`
- **Validation**: 编译通过

## Phase 2: Desktop Formula模块修复

### Task 2.1: FormulaMasterDetailViewModel空行创建
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- **Lines**: 354, 365, 474
- **Change**:
  - Line 354: `Unit = herb.Unit ?? "g"` → `Unit = herb.Unit ?? string.Empty` (从DTO加载时)
  - Line 365: `Unit = "g"` → `Unit = string.Empty` (添加空行)
  - Line 474: `Unit = "g"` → `Unit = string.Empty` (添加空行)
- **Validation**: 编辑经验方时空行不显示"g"

### Task 2.2: FormulaDetailViewModel空行创建
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`
- **Line**: 494
- **Change**: `Unit = "g"` → `Unit = string.Empty`
- **Validation**: 编译通过

### Task 2.3: EditFormulaDialogViewModel空行创建
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/EditFormulaDialogViewModel.cs`
- **Lines**: 181, 193
- **Change**:
  - Line 181: `currentItem.Unit = selectedHerb.Unit ?? "g"` → `currentItem.Unit = selectedHerb.Unit ?? string.Empty`
  - Line 193: `Unit = "g"` → `Unit = string.Empty`
- **Validation**: 弹窗编辑时空行不显示"g"

## Phase 3: Desktop MedicalCase模块修复

### Task 3.1: HerbSelectionManager空行创建
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/HerbSelectionManager.cs`
- **Lines**: 89-92, 222-225
- **Change**: 所有 `Unit = "g"` → `Unit = string.Empty`
- **Validation**: 处方编辑时空行不显示"g"

## Phase 4: Server模块修复

### Task 4.1: FormulaService导入处理
- **File**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- **Line**: 427
- **Change**: `Unit = herbDto.Unit ?? "g"` → `Unit = herbDto.Unit ?? string.Empty`
- **Validation**: 导入经验方时不强制设置"g"

## Phase 5: 打印模板Fallback保留

### Task 5.1: PrescriptionPrintModel保留默认值
- **File**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Models/PrescriptionPrintModel.cs`
- **Line**: 106
- **Decision**: 保留 `Unit = "g"` 作为打印时的fallback，防止打印出空单位
- **Validation**: 无需修改

## Phase 6: 验证

### Task 6.1: 编译验证
- **Command**: `dotnet build LYBT.All.sln -c Release`
- **Expected**: 0 errors, 0 warnings

### Task 6.2: 功能验证
- 新建经验方 → 添加空行 → 单位为空
- 选择药材 → 单位自动填充为药材库定义的单位
- 新建处方 → 添加空行 → 单位为空
- 选择药材 → 单位自动填充

## Dependencies
- 无外部依赖
- 各Task可并行执行

## Estimated Impact
- 10个文件修改
- 约15处代码变更
- 低风险

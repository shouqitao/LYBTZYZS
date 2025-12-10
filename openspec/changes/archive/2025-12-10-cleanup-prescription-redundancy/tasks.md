# Tasks: cleanup-prescription-redundancy

## Phase 1: 删除确认重复的ViewModels/Components

### TASK-1.1: 删除PrescriptionCalculator重复实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/PrescriptionCalculator.cs`
- **行数**: 128行
- **原因**: MedicalCase模块已有独立实现
- **验证**: 确认无其他引用后删除

### TASK-1.2: 删除PrescriptionValidator重复实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/PrescriptionValidator.cs`
- **行数**: 168行
- **原因**: MedicalCase模块已有独立实现
- **验证**: 确认无其他引用后删除

### TASK-1.3: 删除PrescriptionItemViewModel重复实现
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemViewModel.cs`
- **行数**: 178行
- **原因**: MedicalCase模块已有独立实现
- **验证**: 确认无其他引用后删除

### TASK-1.4: Phase 1编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **预期**: 编译成功，无错误

---

## Phase 2: 分析并清理未使用的Components

### TASK-2.1: 分析BasicValidator使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Components/BasicValidator.cs`
- **行数**: 383行
- **分析**: 检查是否被PrescriptionEditorService或其他保留代码引用
- **决策**: 根据分析结果决定保留或删除

### TASK-2.2: 分析PriceCalculator使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Components/PriceCalculator.cs`
- **行数**: 218行
- **分析**: 检查是否被保留代码引用
- **决策**: 根据分析结果决定保留或删除

### TASK-2.3: 分析PrescriptionEventCoordinator使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/PrescriptionEventCoordinator.cs`
- **行数**: 502行
- **分析**: 检查是否被保留代码引用
- **决策**: 根据分析结果决定保留或删除

### TASK-2.4: Phase 2编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **预期**: 编译成功

---

## Phase 3: 清理Models和Constants

### TASK-3.1: 分析PrescriptionItem模型使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Models/PrescriptionItem.cs`
- **行数**: 480行
- **分析**: 检查是否被保留代码引用
- **决策**: 根据分析结果决定保留或删除

### TASK-3.2: 分析PrescriptionItemRow使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemRow.cs`
- **行数**: 30行
- **分析**: 检查是否被保留代码引用
- **决策**: 根据分析结果决定保留或删除

### TASK-3.3: 分析PrescriptionConstants使用情况
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Constants/PrescriptionConstants.cs`
- **行数**: 129行
- **分析**: 检查常量是否被打印服务或编辑器服务使用
- **决策**: 根据分析结果决定保留或删除

### TASK-3.4: Phase 3编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **预期**: 编译成功

---

## Phase 4: 最终验证与清理

### TASK-4.1: 更新csproj文件
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/LYBT.Desktop.Prescriptions.csproj`
- **操作**: 移除已删除文件的任何显式引用

### TASK-4.2: 清理空目录
- **操作**: 删除因文件移除而变空的目录

### TASK-4.3: 完整编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release`
- **预期**: 编译成功，无警告

### TASK-4.4: 功能验证
- **操作**: 启动应用，测试医案处方功能
- **验证点**:
  - 处方添加药材
  - 处方价格计算
  - 处方保存
  - 处方打印预览

---

## 进度跟踪

| Phase | 任务 | 状态 | 删除行数 |
|-------|------|------|----------|
| 1 | TASK-1.1 PrescriptionCalculator | pending | 128 |
| 1 | TASK-1.2 PrescriptionValidator | pending | 168 |
| 1 | TASK-1.3 PrescriptionItemViewModel | pending | 178 |
| 1 | TASK-1.4 编译验证 | pending | - |
| 2 | TASK-2.1 BasicValidator | pending | TBD |
| 2 | TASK-2.2 PriceCalculator | pending | TBD |
| 2 | TASK-2.3 PrescriptionEventCoordinator | pending | TBD |
| 2 | TASK-2.4 编译验证 | pending | - |
| 3 | TASK-3.1 PrescriptionItem | pending | TBD |
| 3 | TASK-3.2 PrescriptionItemRow | pending | TBD |
| 3 | TASK-3.3 PrescriptionConstants | pending | TBD |
| 3 | TASK-3.4 编译验证 | pending | - |
| 4 | TASK-4.1 更新csproj | pending | - |
| 4 | TASK-4.2 清理空目录 | pending | - |
| 4 | TASK-4.3 完整编译 | pending | - |
| 4 | TASK-4.4 功能验证 | pending | - |

**Phase 1预计删除**: 474行
**Phase 2-3待定**: 根据分析结果

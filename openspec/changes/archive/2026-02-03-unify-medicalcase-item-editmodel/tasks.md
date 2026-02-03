# unify-medicalcase-item-editmodel Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Low
- **预估工作量**: 1-2小时

## Phase 1: 增强 ConsultationItem 并替换

### 1.1 为 ConsultationItem 添加 Reset() 方法
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/ConsultationItem.cs`
- **变更**: 添加 Reset() 方法，重置4个诊断字段
```csharp
public void Reset()
{
    PresentIllness = null;
    TongueDiagnosis = null;
    PulseDiagnosis = null;
    TcmDiagnosis = null;
}
```
- **验证**: 编译通过

### 1.2 更新 ViewModel 属性类型
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- **变更**:
  - 属性 `Consultation` 类型从 `ConsultationEditModel` 改为 `ConsultationItem`
  - 更新 `InitializeEditModels()` 方法中的初始化逻辑
  - 更新 `SaveDetailAsync()` 中的数据提取逻辑
- **验证**: 编译通过

### 1.3 删除 ConsultationEditModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/ConsultationEditModel.cs`
- **变更**: 删除文件
- **验证**: 编译通过

### 1.4 Phase 1 编译验证
- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 2: 增强 PrescriptionItem 并替换

### 2.1 为 PrescriptionItem 添加 DefaultUsage 常量和 Reset() 方法
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/PrescriptionItem.cs`
- **变更**:
  - 添加常量: `public const string DefaultUsage = "水煎服，一日一剂，分早晚两次温服";`
  - 添加 Reset() 方法（不同于 Clear()，不重置 MedicalCaseId）
```csharp
public void Reset()
{
    DosageCount = 7;
    Usage = DefaultUsage;
    FrequencyDescription = null;
    DeliveryMethod = null;
    Notes = null;
    Items.Clear();
}
```
- **验证**: 编译通过

### 2.2 更新 ViewModel 属性类型
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- **变更**:
  - 属性 `Prescription` 类型从 `PrescriptionEditModel` 改为 `PrescriptionItem`
  - 更新 `InitializeEditModels()` 方法中的初始化逻辑
  - 更新 `SaveDetailAsync()` 中的数据提取逻辑
  - 确保 ClearPrescription 命令调用 `Prescription.Reset()`
- **验证**: 编译通过

### 2.3 删除 PrescriptionEditModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/PrescriptionEditModel.cs`
- **变更**: 删除文件
- **验证**: 编译通过

### 2.4 Phase 2 编译验证
- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 3: 清理与验证

### 3.1 删除 Models/Edit 目录
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/`
- **变更**: 如果目录为空，删除目录
- **验证**: 目录已删除

### 3.2 全量编译验证
- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误
- 确保零警告（与 EditModel 相关）

### 3.3 运行时验证
- 启动应用程序
- 检查 Visual Studio 输出窗口无 System.Windows.Data 绑定错误
- 测试医案编辑界面诊断录入
- 测试医案编辑界面处方编辑
- 测试清空处方功能

## Dependencies

```
Phase 1 ─────────────────────┐
                             │
Phase 2 ─────────────────────┼──> Phase 3
                             │
```

Phase 1 和 Phase 2 无依赖关系，可按任意顺序执行，但建议顺序执行以便逐步验证。

## Validation Checklist

- [x] Desktop解决方案编译通过 ✓ (2026-01-17)
- [x] ConsultationEditModel.cs 已删除 ✓
- [x] PrescriptionEditModel.cs 已删除 ✓
- [x] Models/Edit/ 目录已删除 ✓
- [ ] 医案编辑界面诊断录入正常 (需运行时验证)
- [ ] 医案编辑界面处方编辑正常 (需运行时验证)
- [ ] 清空处方功能正常 (需运行时验证)
- [ ] 无 System.Windows.Data 绑定错误 (需运行时验证)

## Notes

1. **XAML 绑定无需修改**: 由于属性名一致且 DependencyProperty 类型为 `object`，支持 duck typing
2. **Reset() vs Clear() 区别**:
   - `Reset()` 仅重置用户可编辑字段，保留 MedicalCaseId
   - `Clear()` 重置所有字段包括 ID
3. **取消操作不受影响**: 取消操作使用 `RestoreFromClone()` 机制，不依赖 Reset()

---

**生成时间**: 2026-01-17
**执行时间**: 2026-01-17
**状态**: 已执行 (代码变更完成，待运行时验证)

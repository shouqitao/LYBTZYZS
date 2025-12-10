# Spec Delta: 重构Client端处方模块代码整合

**Change ID:** refactor-prescription-module-consolidation
**Spec:** desktop-prescription
**Created:** 2025-12-10
**Base Spec:** openspec/specs/desktop-prescription/spec.md

---

## REMOVED Requirements

### REM-001: MedicalCase模块独立验方选择对话框

**原需求**: MedicalCase模块有独立的FormulaSelectionDialogViewModel用于验方选择

**移除理由**: 与Prescriptions模块的SelectFormulaDialogViewModel功能重复（70%代码重复）

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/FormulaSelectionDialogViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/FormulaSelectionDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/FormulaSelectionDialog.xaml.cs`

---

### REM-002: MedicalCase模块独立处方计算器

**原需求**: MedicalCase模块有独立的PrescriptionCalculator服务

**移除理由**: 与Prescriptions模块的PrescriptionCalculator功能重复，且未使用共享基类

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/PrescriptionCalculator.cs`

---

## MODIFIED Requirements

### MOD-001: MedicalCase模块处方面板使用统一组件

**原需求**: PrescriptionPanelViewModel使用模块内的FormulaSelectionDialogViewModel和PrescriptionCalculator

**新需求**: PrescriptionPanelViewModel使用Prescriptions模块的SelectFormulaDialogViewModel和PrescriptionCalculator

**变更详情**:

```csharp
// Before
_dialogService.ShowDialog("FormulaSelectionDialog", parameters, callback);
var calculator = new MedicalCase.Services.PrescriptionCalculator();

// After
_dialogService.ShowDialog("SelectFormulaDialog", parameters, callback);
var calculator = Container.Resolve<Prescriptions.ViewModels.Components.PrescriptionCalculator>();
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`

---

### MOD-002: MedicalCase模块添加Prescriptions依赖

**原需求**: MedicalCase模块不依赖Prescriptions模块

**新需求**: MedicalCase模块依赖Prescriptions模块以使用统一组件

**变更详情**:

```xml
<!-- LYBT.Desktop.MedicalCase.csproj -->
<ItemGroup>
  <!-- 新增 -->
  <ProjectReference Include="..\LYBT.Desktop.Prescriptions\LYBT.Desktop.Prescriptions.csproj" />
</ItemGroup>
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj`

---

### MOD-003: PrescriptionCalculator增强事件支持

**原需求**: Prescriptions模块的PrescriptionCalculator仅提供计算方法

**新需求**: 增加价格计算事件通知机制（从MedicalCase版本迁移）

**变更详情**:

```csharp
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemViewModel>
{
    // 新增事件
    public event EventHandler<PriceCalculatedEventArgs>? PriceCalculated;

    // 新增方法
    public decimal CalculateSingleDosagePrice(
        IEnumerable<IHerbItem> items,
        IEnumerable<HerbDto> allHerbs);

    public List<PrescriptionItemDto> BuildItemsWithPrice(
        IEnumerable<IHerbItem> items,
        IEnumerable<HerbDto> allHerbs);
}
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/PrescriptionCalculator.cs`

---

### MOD-004: MedicalCase处方项ViewModel重命名

**原需求**: 类名为`PrescriptionItemViewModel`

**新需求**: 重命名为`PrescriptionHerbEditorViewModel`以消除与Prescriptions模块同名类的混淆

**变更详情**:

```csharp
// Before
public class PrescriptionItemViewModel : ViewModelBase, IHerbItem

// After
public class PrescriptionHerbEditorViewModel : ViewModelBase, IHerbItem
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionItemViewModel.cs` -> `PrescriptionHerbEditorViewModel.cs`
- 所有引用该类的文件

---

## ADDED Requirements

### ADD-001: PriceCalculatedEventArgs迁移到Prescriptions模块

**新需求**: 在Prescriptions模块添加价格计算事件参数类

**详情**:

```csharp
namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 价格计算完成事件参数
    /// </summary>
    public class PriceCalculatedEventArgs : EventArgs
    {
        public decimal SingleDosagePrice { get; init; }
        public decimal TotalPrice { get; init; }
        public int DosageCount { get; init; }
        public decimal Discount { get; init; }
    }
}
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/Components/PriceCalculatedEventArgs.cs` (新建)

---

### ADD-002: 模块职责边界文档化

**新需求**: 在模块文件中添加清晰的职责说明注释

**Prescriptions模块职责**:
- 处方打印服务
- 处方独立管理（历史记录、搜索）
- 验方选择对话框（统一入口）
- 处方价格计算器（统一组件）
- DTO包装和数据传输

**MedicalCase模块处方相关职责**:
- 医案内的处方编辑面板
- 交互式药材选择（7级拼音过滤）
- 处方与医案的关联管理

---

## Validation Requirements

### VAL-001: 编译验证

- [ ] 解决方案编译无错误
- [ ] 无CS0246（类型未找到）错误
- [ ] 无CS0103（名称不存在）错误

### VAL-002: 功能验证

- [ ] 医案编辑->处方面板->导入验方->功能正常
- [ ] 添加药材->修改数量->价格实时更新
- [ ] 处方打印功能不受影响

### VAL-003: 代码质量验证

- [ ] 代码行数减少 >= 1,000行
- [ ] 无重复代码（验方选择、价格计算）
- [ ] 命名清晰无歧义

---

### ADD-003: MedicalCase级别打印服务

**新需求**: 在MedicalCase模块添加统一打印服务接口

**详情**:

```csharp
namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医案级别打印服务 - 支持完整医案、诊断、处方的打印
    /// </summary>
    public interface IMedicalCasePrintService
    {
        Task<PrintResult> PrintFullCaseAsync(MedicalCaseDto medicalCase);
        Task<PrintResult> PrintConsultationAsync(ConsultationDto consultation);
        Task<PrintResult> PrintPrescriptionAsync(PrescriptionDto prescription);
        Task<PrintResult> PrintSummaryAsync(MedicalCaseDto medicalCase);
    }
}
```

**影响文件**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/IMedicalCasePrintService.cs` (新建)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCasePrintService.cs` (新建)

---

### ADD-004: Server端冗余字段标记

**新需求**: 在Prescription实体中标记冗余字段为`[Obsolete]`

**详情**:

```csharp
// src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs
[Obsolete("通过MedicalCase.PatientId获取，此字段保留仅为兼容性")]
public Guid? PatientId { get; set; }

[Obsolete("通过MedicalCase.UserId获取，此字段保留仅为兼容性")]
public Guid? UserId { get; set; }
```

**影响文件**:
- `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`

---

### ADD-005: Shared层DTO冗余字段标记

**新需求**: 在Prescription相关DTO中标记冗余字段为`[Obsolete]`

**详情**:

```csharp
// src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDtos.cs
public class PrescriptionDto : StatusDto, IRemarkable
{
    [Obsolete("通过MedicalCase.PatientId获取")]
    public Guid PatientId { get; set; }

    [Obsolete("通过MedicalCase.UserId获取")]
    public Guid UserId { get; set; }

    // ... 其他字段保持不变
}
```

**影响文件**:
- `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDtos.cs`

---

## Migration Notes

### 调用方迁移指南

如果有代码直接引用被删除的类，需要进行以下迁移：

```csharp
// FormulaSelectionDialogViewModel -> SelectFormulaDialogViewModel
// Before
using LYBT.Desktop.MedicalCase.ViewModels;
_dialogService.ShowDialog(nameof(FormulaSelectionDialog), ...);

// After
using LYBT.Desktop.Prescriptions.ViewModels;
_dialogService.ShowDialog(nameof(SelectFormulaDialog), ...);

// PrescriptionCalculator
// Before
using LYBT.Desktop.MedicalCase.Services;
var calc = new PrescriptionCalculator();

// After
using LYBT.Desktop.Prescriptions.ViewModels.Components;
var calc = _container.Resolve<PrescriptionCalculator>();

// PrescriptionItemViewModel (MedicalCase)
// Before
var item = new PrescriptionItemViewModel(...);

// After
var item = new PrescriptionHerbEditorViewModel(...);
```

---

## Appendix: Code Statistics

### Before Refactoring

| 文件 | 行数 | 状态 |
|------|------|------|
| MedicalCase/FormulaSelectionDialogViewModel.cs | 216 | 将删除 |
| MedicalCase/PrescriptionCalculator.cs | 186 | 将删除 |
| MedicalCase/PrescriptionItemViewModel.cs | 387 | 将重命名 |
| Prescriptions/SelectFormulaDialogViewModel.cs | 587 | 保留 |
| Prescriptions/PrescriptionCalculator.cs | 128 | 将增强 |
| Prescriptions/PrescriptionItemViewModel.cs | 178 | 保留 |
| **总计** | **1,682** | - |

### After Refactoring (预估)

| 文件 | 行数 | 状态 |
|------|------|------|
| MedicalCase/PrescriptionHerbEditorViewModel.cs | 387 | 重命名 |
| Prescriptions/SelectFormulaDialogViewModel.cs | 587 | 保留 |
| Prescriptions/PrescriptionCalculator.cs | ~180 | 增强 |
| Prescriptions/PriceCalculatedEventArgs.cs | ~25 | 新增 |
| Prescriptions/PrescriptionItemViewModel.cs | 178 | 保留 |
| **总计** | **~1,357** | - |

**净减少**: ~325行（不含删除的Views）

**删除的重复代码**: ~400行（FormulaSelectionDialogViewModel + PrescriptionCalculator）

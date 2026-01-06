# MedicalCase Module

## 模块定位

MedicalCase是医案管理的**聚合根模块**，整合了处方、诊断等核心业务功能。

## 架构演进记录

### 2025-01 迁入的功能

| 来源模块 | 迁入组件 | 位置 |
|----------|----------|------|
| Prescriptions | `PrescriptionHerbItem` | `Models/Items/` |

### OpenSpec: create-printing-module (2025-01) 迁出的功能

| 迁出组件 | 目标模块 | 新位置 |
|----------|----------|--------|
| `IPrescriptionPrintService` | LYBT.Desktop.Printing | 已替换为 `IPrintService<T>` |
| `PrescriptionPrintService` | LYBT.Desktop.Printing | `Services/` |
| `PrescriptionPrintModel` | LYBT.Desktop.Printing | `Models/` |
| `PrescriptionPrintTemplate.xaml` | LYBT.Desktop.Printing | `Templates/` |

### 模块依赖

```csharp
[ModuleDependency("PatientsModule")] // 病历依赖患者
// [已移除] PrescriptionsModule - 所有功能已迁移到本模块
// [已移除] ConsultationModule - MedicalCase是聚合根，不应依赖子实体模块
```

## 关键类型

### Entity→DTO→Item模式

MedicalCase作为聚合根，持有诊断和处方的Item类：

| 层级 | 诊断(Consultation) | 处方(Prescription) |
|------|-------------------|-------------------|
| Entity | 服务端Consultation | 服务端Prescription |
| DTO | ConsultationDetailDto | PrescriptionDetailDto |
| Item | ConsultationItem | PrescriptionItem |

### ConsultationItem

位置: `Models/Items/ConsultationItem.cs`

诊断数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TcmDiagnosis`
- `IsDiagnosisComplete` - 验证必填字段

方法:
- ~~`FromDto()`, `ToDto()`~~ - 已废弃，请使用 `ConsultationMappingService`
- `ToInputDto()`

### PrescriptionItem

位置: `Models/Items/PrescriptionItem.cs`

处方数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `DosageCount`, `Usage`, `Advice`, `ReferencedFormulas`, `Remark`
- `Items` (ObservableCollection<HerbItemDto>) - 药材列表
- `ItemCount`, `SingleDosePrice`, `TotalPrice`, `HasItems`, `IsValid`

方法:
- ~~`FromDto()`, `ToDto()`~~ - 已废弃，请使用 `PrescriptionMappingService`
- `ToInputDto()`, `Clear()`

### PrescriptionHerbItem (已废弃)

位置: `Models/Items/PrescriptionHerbItem.cs`

处方药材项ViewModel，继承自`HerbItemViewModelBase`（Herbs模块）。

被以下组件使用:
- `MedicalCaseMasterDetailViewModel` - 药材列表绑定
- `PrescriptionCalculator` - 价格计算
- `PrescriptionValidator` - 处方验证
- `PrescriptionDataLoader` - 数据加载

**注意**: PrescriptionPanelViewModel已删除，改用PrescriptionItem

### 打印服务

OpenSpec: create-printing-module - 打印功能已迁移到独立的 `LYBT.Desktop.Printing` 模块

- 通过 `IPrintService<PrescriptionPrintModel>` 接口使用打印功能
- `PrescriptionPrintHandler` 负责组装打印数据模型

## Mapperly与CommunityToolkit.Mvvm源生成器兼容性

**重要**: Item类使用`[ObservableProperty]`源生成器时，Mapperly的`[MapProperty]`属性无法正常工作。

### 问题原因

Mapperly源生成器在编译时验证属性是否存在，但`[ObservableProperty]`生成的属性（如`CaseStatus`、`CompletedAt`）在Mapperly运行时尚未生成，导致RMG005/RMG006错误。

### 解决方案

对于源生成的属性，使用`[MapperIgnoreSource]`/`[MapperIgnoreTarget]`忽略，在包装方法中手动映射：

```csharp
// 错误模式（会导致编译错误）
[MapProperty(nameof(Dto.CaseStatus), "CaseStatus")]
public partial Item ToItemCore(Dto dto);

// 正确模式
[MapperIgnoreTarget("CaseStatus")]  // 字符串字面量
[MapperIgnoreSource(nameof(Dto.CaseStatus))]
public partial Item ToItemCore(Dto dto);

public Item ToItem(Dto dto)
{
    var item = ToItemCore(dto);
    item.CaseStatus = dto.CaseStatus;  // 手动映射
    return item;
}
```

### 受影响的Mapper

- `MedicalCaseItemMapper.cs` - CaseStatus, CompletedAt
- `ConsultationMapper.cs` - IsSelected, IsExpanded, 审计字段
- `PrescriptionMapper.cs` - IsSelected, IsExpanded, IsReadOnly, Items

## 注意事项

1. **命名空间**: 从Prescriptions迁入的类使用`LYBT.Desktop.MedicalCase.*`命名空间
2. **依赖方向**: MedicalCase作为聚合根，不应依赖Consultation等子实体模块
3. **打印依赖**: 通过项目引用 `LYBT.Desktop.Printing` 使用打印服务
4. **Mapper属性**: 对`[ObservableProperty]`生成的属性，必须使用字符串字面量而非`nameof()`

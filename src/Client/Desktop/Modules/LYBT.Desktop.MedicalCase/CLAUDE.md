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
- `FromDto()`, `ToDto()`, `ToInputDto()`

### PrescriptionItem

位置: `Models/Items/PrescriptionItem.cs`

处方数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `DosageCount`, `Usage`, `Advice`, `ReferencedFormulas`, `Remark`
- `Items` (ObservableCollection<HerbItemDto>) - 药材列表
- `ItemCount`, `SingleDosePrice`, `TotalPrice`, `HasItems`, `IsValid`

方法:
- `FromDto()`, `ToDto()`, `ToInputDto()`, `Clear()`

### PrescriptionItem

位置: `Models/Items/PrescriptionItem.cs`

处方数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `DosageCount`, `Usage`, `Advice`, `ReferencedFormulas`, `Remark`
- `Items` (ObservableCollection<HerbItemDto>) - 药材列表
- `ItemCount`, `SingleDosePrice`, `TotalPrice`, `HasItems`, `IsValid`

方法:
- `FromDto()`, `ToDto()`, `ToInputDto()`, `Clear()`

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

## 注意事项

1. **命名空间**: 从Prescriptions迁入的类使用`LYBT.Desktop.MedicalCase.*`命名空间
2. **依赖方向**: MedicalCase作为聚合根，不应依赖Consultation等子实体模块
3. **打印依赖**: 通过项目引用 `LYBT.Desktop.Printing` 使用打印服务

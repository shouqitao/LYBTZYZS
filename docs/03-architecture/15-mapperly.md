# Mapperly 映射规范

> 基于 **Mapperly 4.3.1** 的编译时 source-generator 映射，零运行时反射。本文档从 [08-shared.md](08-shared.md) 外移为独立规范（spec S3 批次2）。
>
> **关联**：
> - Shared 层架构：[08-shared.md](08-shared.md)
> - DTO 契约：[08-shared.md「LYBT.Shared.Models」](08-shared.md#lybtsharedmodels-dto-与-contract)
> - MedicalCase 聚合根映射：[04-data-model.md](04-data-model.md)

---

## 概述

项目内共 23 个 Mapper 类，分布在 Server 和 Client 两端。

| 层 | Mapper 数量 | 位置 |
|----|------------|------|
| Server 模块 | 6 | `src/Server/Modules/LYBT.Module.*/Mapping/` |
| Client LocalData | 6 | `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/` |
| Client Desktop 模块 | 10 | `src/Client/Desktop/Modules/LYBT.Desktop.*/Mappers/` |
| Client 内联 | 1 | `PatientRepository.cs` 内 `PatientListToDetailMapper` |

---

## 映射约定

### Mapper 属性配置

```csharp
// Server 端: Target 策略 (只映射目标属性, 未匹配源不报错)
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]

// Client LocalData: 默认 Both 策略 (源和目标都必须匹配)
[Mapper]

// Client Desktop 模块: Target 策略 (与 Server 一致)
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]

// 特殊: 深度克隆
[Mapper(UseDeepCloning = true)]
```

### 标准方法命名

| 方法 | 签名模式 | 说明 |
|------|----------|------|
| `ToListDto` | `Entity → ListDto` | 列表映射 (基础字段) |
| `ToListDtos` | `List<Entity> → List<ListDto>` | 列表批量映射 |
| `ToDetailDto` | `Entity → DetailDto` | 详情映射 (全字段) |
| `ToDetailDtos` | `List<Entity> → List<DetailDto>` | 详情批量映射 |
| `ToEntity` | `InputDto → Entity` | 创建映射 |
| `UpdateEntity` | `(InputDto dto, Entity entity) → void` | 更新映射 (映射到已有实例) |
| `ToEntityFromImport` | `ImportItemDto → Entity` | Excel 导入映射 (忽略更多目标字段) |

### 常用特性

| 特性 | 用途 |
|------|------|
| `[MapperIgnoreSource]` | 忽略源属性 (不参与映射) |
| `[MapperIgnoreTarget]` | 忽略目标属性 (由 Service 或计算填充) |
| `[MapProperty]` | 属性重命名映射 |
| `[UserMapping(Default = false)]` | 手写方法, 禁止自动生成 |

---

## Server 端映射模式

### 1. Core 映射: Entity → DTO

每个实体对应一个 Mapper 类, 提供 `ToListDto` / `ToDetailDto` / `ToEntity` / `UpdateEntity` 方法。

**实体 → 列表 DTO**: 仅映射基础字段, 忽略计算字段和导航属性。

**实体 → 详情 DTO**: 映射全部业务字段, 忽略需要 Service 层计算的字段。

**输入 DTO → 实体**: 忽略所有审计字段 (`CreatedAt`/`UpdatedAt`/`CreatedBy`/`UpdatedBy`)、主键 (`Id`)、状态字段和计算字段。

### 2. Enrich 映射: 聚合根导航属性填充

MedicalCase 是聚合根, 其 `ToDetailDto` 需要 Consultation 和 Prescription 的导航数据。采用 **Core + Enrich** 模式:

```csharp
// 生成器生成的基础映射 (忽略导航属性)
public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);

// 手写 Enrich 方法, 标记 UserMapping 禁止自动生成
[UserMapping(Default = false)]
public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity)
{
    var dto = ToDetailDto(entity);
    // 填充导航属性: CaseNumber, Diagnosis, Consultation, Prescription
    dto.ConsultationId = entity.Consultation?.Id;
    dto.PrescriptionId = entity.Prescription is { IsDeleted: false }
        ? entity.Prescription.Id : null;
    // ... 嵌套 DTO 映射
    return dto;
}
```

### 3. 已知特殊处理

| 实体 | 属性 | 处理方式 |
|------|------|----------|
| MedicalCase | `HasPrescription` | 计算属性 (`Prescription != null && !IsDeleted`), Mapper 忽略, Service 显式设置 |
| Formula | `Indication → Indications` | `MapProperty` 重命名 (单数→复数) |
| MedicalCase | `Consultation.Id → MedicalCaseId` | `MapProperty` 跨实体 ID 映射 |
| Formula | `HerbCount`, `TotalPrice` | 计算字段, Mapper 忽略, Service 计算 |
| Patient | `Age` | 计算属性 (从 BirthDate), Mapper 忽略 |

---

## Client Desktop 模块映射模式

### 1. DTO → UI Model (BindableBase)

Desktop 模块的 Mapper 将 DTO 映射为 WPF 绑定用的 ItemModel:

```csharp
// FormulaMapper: FormulaDetailDto → FormulaItem
// PatientMapper: PatientDetailDto → PatientItem (带 IsSelected, DisplayText 等 UI 状态)
```

### 2. IsShared ↔ IsPersonal 布尔反转 (Formula 模块)

Formula 的 DTO 使用 `IsShared`, 而 UI Model 使用 `IsPersonal`, 语义相反:

```csharp
// DTO → Item: Mapper 忽略两边属性, 手动反转
item.IsPersonal = !dto.IsShared;

// Item → DTO: 反向同理
dto.IsShared = !item.IsPersonal;
```

此模式出现在 `FormulaMapper`、`FormulaDetailModelMapper`、`FormulaHerbItemMapper` 中, 共 4 处映射方向。

### 3. DTO → DTO 转换

`PatientListToDetailMapper` 将 `PatientListDto` 映射为 `PatientDetailDto`, 用于客户端仅持有列表数据时构造详情视图。

---

## Client LocalData 映射模式

LocalData Mapper 映射 LocalDB 实体到共享 DTO, 使用默认 `Both` 策略。MedicalCase 同样采用 **Core + Enrich** 模式:

```csharp
// 生成器方法 (忽略导航属性)
public partial MedicalCaseDetailDto ToDetailDtoCore(MedicalCase entity);

// 手写 Enrich 包装
public MedicalCaseDetailDto ToDetailDto(MedicalCase entity)
{
    var dto = ToDetailDtoCore(entity);
    // 填充 ConsultationId, PrescriptionId, Diagnosis, 嵌套 DTO
    return dto;
}
```

---

## DI 注册

| 模式 | 适用范围 | 说明 |
|------|----------|------|
| `new()` 直接实例化 | 大多数 Mapper | Mapperly 生成无状态代码, 无需 DI |
| `AddSingleton<T>()` | MedicalCaseMapper (Server) | 已注册但部分 Service 仍用 `new()` |

Server 端 Mapper 使用 `new()` 内联实例化 (UserMapper, PatientMapper, HerbMapper, FormulaMapper, RegistrationMapper)。

Client 端所有 Mapper 均使用 `new()` 内联实例化。

> 注: Mapperly 生成的是无状态 partial class, `new()` 实例化安全且高效, DI 注册非必需。

---

## 已知陷阱

| 陷阱 | 说明 | 影响文件 |
|------|------|----------|
| HasPrescription | 从 `PrescriptionId.HasValue` 计算, Mapper 必须忽略并由 Service 显式设置 | MedicalCaseMapper, LocalMedicalCaseMapper |
| Boolean 反转 | Formula `IsShared`/`IsPersonal` 语义相反, 必须手写映射 | FormulaMapper (Desktop), FormulaDetailModelMapper |
| DateTime | 所有 DateTime 存储 UTC, 显示转换在 ViewModel 层 | 全局 |
| Nullable 引用类型 | Mapperly 尊重可空性标注, 不匹配时需显式处理 | 全局 |
| Audit 字段 | `CreatedAt`/`UpdatedAt` 等在 `ToEntity`/`UpdateEntity` 中必须忽略 | 所有 Mapper |
| 未使用 Mapper | Desktop `PatientMapper` (Patients 模块) 未被任何代码实例化 | LYBT.Desktop.Patients/Mappers/ |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v1.0 | **从 08-shared.md 外移**：Mapperly 映射规范整体迁移为独立文档（spec S3 批次2）。08-shared.md 留 Mapperly 概述 + 指向本文档。 |

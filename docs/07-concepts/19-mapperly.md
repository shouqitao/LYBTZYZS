---
type: concept
title: Mapperly 对象映射框架
created: 2026-06-10
updated: 2026-06-12
tags: [工具, 映射器, 源生成器, 性能, mapperly]
related: [ef-core-data-model, mvvm-prism, coding-standards]
sources:
  - "src/Server/Modules/*/Mapping/"
  - "src/Client/Desktop/Modules/*/Mappers/"
  - "Directory.Packages.props"
---

# Mapperly 对象映射框架

Mapperly（Riok.Mapperly）是本系统采用的**编译时源生成器**，用于替代 AutoMapper，负责 Entity ↔ DTO、DTO ↔ ViewModel 之间的对象映射。所有映射代码在编译时由 Roslyn 源生成器生成，零运行时反射开销。

## 为什么选择 Mapperly

| 维度 | AutoMapper | Mapperly |
|------|-----------|----------|
| 映射时机 | 运行时反射 | 编译时源生成 |
| 性能 | 反射开销 + 缓存 | 与手写代码等价 |
| 类型安全 | 运行时才发现映射错误 | 编译时即报错 |
| AOT 兼容 | 不兼容 | 完全兼容 NativeAOT |
| 调试 | 难以调试映射逻辑 | 可直接查看生成的 `.g.cs` |
| 未映射属性检测 | 需配置 `Validate()` | `RequiredMappingStrategy.Target` 编译时强制 |

## 配置

**NuGet 包**：`Riok.Mapperly` **4.3.1**（定义于 `Directory.Packages.props`）

```xml
<PackageVersion Include="Riok.Mapperly" Version="4.3.1" />
```

引用方式：在各模块 `.csproj` 中添加：

```xml
<PackageReference Include="Riok.Mapperly" />
```

Mapperly 是增量源生成器（`Analyzer` + `SourceGenerator`），无需额外 MSBuild 配置，编译时自动运行。

## 项目中的分布

Mapperly 在 Server 和 Client 两侧均有使用，共 **26 个映射器文件**：

| 层 | 路径模式 | 职责 |
|----|---------|------|
| Server 端 | `src/Server/Modules/LYBT.Module.*/Mapping/{Entity}Mapper.cs` | Entity ↔ DTO |
| Client 端 | `src/Client/Desktop/Modules/LYBT.Desktop.*/Mappers/{Entity}Mapper.cs` | DTO ↔ ViewModel (Item) |
| Client 本地数据 | `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/Local*.cs` | 本地模式 DTO 映射 |
| Client LocalWebAPI | `src/Client/Desktop/LocalWebAPI/Mappers/LocalApiMapper.cs` | 本地 API 端映射 |
| 共享异常处理 | `src/Shared/LYBT.Shared.ExceptionHandling/Mappers/` | 异常消息映射 |

## 基本用法

### `[Mapper]` 特性与 partial 类

所有映射器均为 `partial class`，标注 `[Mapper]` 特性。本项目统一使用 `RequiredMappingStrategy.Target`，要求目标类型的**所有属性**必须被映射（否则编译错误）：

```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PatientMapper
{
    public partial PatientListDto ToListDto(Patient entity);
    public partial List<PatientListDto> ToListDtos(List<Patient> entities);
}
```

编译后自动生成 `{MapperName}.g.cs`，包含完整的映射实现。

### 自动映射

同名且类型兼容的属性自动映射，无需额外配置：

```csharp
public partial PatientDetailDto ToDetailDto(Patient entity);
// Name → Name, Gender → Gender, Phone → Phone 等自动映射
```

### `[MapProperty]` — 名称不匹配

当源属性名与目标属性名不同时使用：

```csharp
// FormulaMapper.cs — Indication → Indications 字段名不同
[MapProperty(nameof(Formula.Indication), nameof(FormulaListDto.Indications))]
[MapperIgnoreTarget(nameof(FormulaListDto.HerbCount))]
[MapperIgnoreTarget(nameof(FormulaListDto.TotalPrice))]
public partial FormulaListDto ToListDto(Formula entity);
```

```csharp
// MedicalCaseMapper.cs — Consultation.Id 映射为 ConsultationDetailDto.MedicalCaseId
[MapProperty(nameof(Consultation.Id), nameof(ConsultationDetailDto.MedicalCaseId))]
public partial ConsultationDetailDto ToConsultationDetailDto(Consultation entity);
```

### `[MapperIgnoreTarget]` / `[MapperIgnoreSource]` — 忽略属性

- **`MapperIgnoreTarget`**：跳过目标属性（由 Service 层计算或手动填充）
- **`MapperIgnoreSource`**：跳过源属性（源有但目标不需要）

```csharp
// PatientMapper.cs — 忽略审计字段（由 Service 层自动设置）
[MapperIgnoreSource(nameof(PatientInputDto.Id))]
[MapperIgnoreTarget(nameof(Patient.Id))]
[MapperIgnoreTarget(nameof(Patient.Age))]        // 计算属性
[MapperIgnoreTarget(nameof(Patient.CreatedAt))]   // 审计字段
[MapperIgnoreTarget(nameof(Patient.RowVersion))]  // 并发控制
public partial Patient ToEntity(PatientInputDto dto);
```

### `[UserMapping]` — 手动映射方法

当映射逻辑超出 Mapperly 能力（如依赖导航属性、复杂计算）时，使用 `[UserMapping(Default = false)]` 标记手动方法。Mapperly 不会为该方法生成代码，但会在内部映射中将其作为可调用方法：

```csharp
[UserMapping(Default = false)]
public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity)
{
    var dto = ToDetailDto(entity);  // 调用 Mapperly 生成的基础映射
    dto.Diagnosis = entity.Consultation?.TcmDiagnosis;  // 手动补充导航属性
    dto.Consultation = entity.Consultation != null
        ? EnrichConsultationDetailDto(entity) : null;
    return dto;
}
```

## 项目核心模式

### 模式一：Core + Enrich（Server MedicalCase）

Mapperly 生成基础映射 → 手动方法补充计算字段和导航属性：

```
MedicalCaseMapper.MapToMedicalCaseDetailDto()
  ├── ToDetailDto()           ← Mapperly 自动生成
  ├── EnrichConsultationDetailDto()
  │     └── ToConsultationDetailDto()  ← Mapperly 自动生成
  └── EnrichPrescriptionDetailDto()
        └── ToPrescriptionDetailDto()  ← Mapperly 自动生成
```

### 模式二：Core + Wrapper（Client Desktop）

Mapperly 生成 `ToItemCore()`（忽略大量 UI 属性）→ 公开的 `ToItem()` 方法调用 Core 并手动补充：

```csharp
// FormulaMapper.cs (Client)
private partial FormulaItem ToItemCore(FormulaDetailDto dto);

public FormulaItem ToItem(FormulaDetailDto dto)
{
    var item = ToItemCore(dto);
    item.IsPersonal = !dto.IsShared;  // 反转布尔值
    if (dto.Herbs != null)
        item.Herbs = dto.Herbs.Select(h => _herbMapper.ToItem(h)).ToList();
    return item;
}
```

### 模式三：审计字段统一忽略（Server 所有模块）

所有 `ToEntity` / `UpdateEntity` 方法均忽略审计字段，由 `BaseRepository` 或 Service 层统一设置：

```
忽略列表：Id, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion, IsDeleted
```

## 已知陷阱

### 1. `[NotMapped]` 计算属性不会被自动映射

`Patient.Age` 是 EF Core `[NotMapped]` 计算属性（由 `BirthDate` 实时计算）。Mapperly 无法自动映射，**必须** `[MapperIgnoreTarget]` 并在需要时手动赋值。

### 2. `IsShared ↔ !IsPersonal` 布尔反转

DTO 使用 `IsShared`，ViewModel 使用 `IsPersonal`，语义相反。Mapperly 无法自动处理布尔反转，必须手动映射：

```csharp
item.IsPersonal = !dto.IsShared;  // Client 端
dto.IsShared = !item.IsPersonal;  // 反向映射
```

### 3. MedicalCase.HasPrescription 必须手动设置

`HasPrescription` 是计算属性（`PrescriptionId.HasValue`），不参与 Mapperly 自动映射，由 Service 层或查询时显式设置。

### 4. 集合导航属性需手动映射

`ObservableCollection`、嵌套集合（如 `Formula.Herbs`）不能直接自动映射，需在 `Core + Wrapper` 模式中手动调用子映射器。

### 5. `RequiredMappingStrategy.Target` 的严格性

使用 `Target` 策略时，目标类型的**每个**属性都必须被映射或显式忽略。新增 DTO 属性后若忘记处理，会导致编译错误——这是特性而非 Bug，确保映射完整性。

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-06-12 | 全面扩展：配置、用法、项目模式、已知陷阱 |
| 2026-06-10 | 初版创建 |

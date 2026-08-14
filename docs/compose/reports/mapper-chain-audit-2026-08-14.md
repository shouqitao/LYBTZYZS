# Mapper 转换链审计报告

**项目**: LYBTZYZS .NET 8 WebAPI + WPF Desktop
**转换链**: Entity → DTO → Desktop Model
**审计日期**: 2026-08-14
**审计范围**: 12 个 Mapper 源文件（5 Server + 7 Desktop）

---

## 1. 审计概览

| 指标 | 值 |
|------|-----|
| Server Mapper 文件 | 5 个，共 448 行 |
| Desktop Mapper 文件 | 7 个，共 1110 行 |
| 总行数 | 1558 行 |
| Mapperly 自动映射方法 | 22 个 |
| 手动映射方法 | 31 个 |
| 自动/手动比 | 42% : 58% |
| 高复杂度 Mapper | 3 个（MedicalCaseMapper, MedicalCaseDetailModelMapper, PrescriptionMapper） |
| 严重重复 | 2 处（PrescriptionItem 双向映射 x2、FormulaHerbItem 三向映射 x3） |

**复杂度分布**:

```
极低  ██                            RegistrationMapper (24行, 0手动)
极低  ███                           PatientMapper/Server (37行, 1手动)
低    ██████                        PatientMapper/Desktop (81行, 3手动)
低-中 ███████                       IdentityMapper (86行, 2手动)
中    █████████                     HerbDetailModelMapper (104行, 3手动)
中    ████████████                  ConsultationMapper (127行, 3手动)
中    █████████████                 UserMapper (120行, 5手动)
中-高 ███████████████               CatalogDtoMapper (118行, 5手动)
高    █████████████████████████     FormulaDetailModelMapper (206行, 4手动+大量Ignore)
高    █████████████████████████████ PrescriptionMapper (232行, 5手动+大量Ignore)
高    █████████████████████████████ MedicalCaseMapper (183行, 3手动+Enrich)
极高  ████████████████████████████████████ MedicalCaseDetailModelMapper (240行, 4手动+26个Ignore)
```

---

## 2. Server Mapper 逐个分析

### 2.1 RegistrationMapper — 极简模板

| 属性 | 值 |
|------|-----|
| 文件 | `src/Server/Modules/LYBT.Module.Registration/Mappers/RegistrationMapper.cs` |
| 行数 | 24 |
| 方法数 | 2 (ToListDto, ToDetailDto) |
| 自动/手动 | 2/0 (100% 自动) |
| 复杂度 | ★☆☆☆☆ |

纯 Mapperly 自动映射，零手写代码。是理想模板。无需优化。

---

### 2.2 PatientMapper (Server) — 干净工厂模式

| 属性 | 值 |
|------|-----|
| 文件 | `src/Server/Modules/LYBT.Module.Patients/Application/Mappers/PatientMapper.cs` |
| 行数 | 37 |
| 方法数 | 3 (ToEntity, ToListDto, ToDetailDto) |
| 自动/手动 | 2/1 (67% 自动) |
| 复杂度 | ★☆☆☆☆ |

- `ToListDto` / `ToDetailDto` — Mapperly 自动，属性全同名
- `ToEntity` — 手写，委托到 `Patient.Create()` 领域工厂（校验 + Trim），Mapperly 无法表达

无需优化。ToEntity 的手写是正确的（领域工厂校验）。

---

### 2.3 IdentityMapper — null 合并防御

| 属性 | 值 |
|------|-----|
| 文件 | `src/Server/Modules/LYBT.Module.Identity/Application/Mappers/IdentityMapper.cs` |
| 行数 | 86 |
| 方法数 | 5 (ToUserDetailDto, ToListDto, ToDetailDto, ToBasicDto, ToCredentialDto) |
| 自动/手动 | 3/2 (60% 自动) |
| 复杂度 | ★★☆☆☆ |

**分析**:
- `ToUserDetailDto` — Mapperly 自动（UserCredentialDto→UserDetailDto，登录响应）
- `ToBasicDto` / `ToCredentialDto` — Mapperly 自动 + `[MapProperty]` 重命名（LastLoginTime→LastLoginTime, AccessFailedCount→FailedLoginCount）
- `ToListDto` / `ToDetailDto` — **手写**，因为 `ApplicationUser.UserName` / `RealName` 为 `string?`（Identity 基类），需要 `?? string.Empty` 合并防御

**手写原因**: Identity 基类可空注解导致 Mapperly 无法自动处理 null→empty。

**优化建议**:
- 已定义 `ToNonNullString` 自定义映射方法但未被 `[UserMapping]` 绑定到 ToListDto/ToDetailDto
- 若将 ToListDto/ToDetailDto 改为 `partial` 并配合 `[MapProperty]` + 自定义方法，可消除手写
- 预计可减少 ~30 行手写代码

---

### 2.4 CatalogDtoMapper — 工厂+嵌套双模式

| 属性 | 值 |
|------|-----|
| 文件 | `src/Server/Modules/LYBT.Module.Catalog/Application/Mappers/CatalogDtoMapper.cs` |
| 行数 | 118 |
| 方法数 | 8 (ToEntity×2, ToHerbListDto, ToHerbDetailDto, ToFormulaListDto, ToFormulaDetailDto, ToHerbItemDto) |
| 自动/手动 | 3/5 (38% 自动) |
| 复杂度 | ★★★☆☆ |

**分析**:
- `ToHerbListDto` / `ToHerbDetailDto` — Mapperly 自动
- `ToFormulaListDto` — Mapperly 自动 + `[MapperIgnoreTarget(TotalPrice)]`
- `ToEntity(HerbInputDto)` / `ToEntity(FormulaInputDto)` — 手写，委托到领域工厂
- `ToFormulaDetailDto` — **完全手写**（15 字段 + Herbs 嵌套映射），原因：
  - `Category` 空值回退 `"验方"`（DTO getter 兜底不一致）
  - `TotalPrice` 恒 0（无源字段）
  - `Herbs` 需嵌套 `ToHerbItemDto` 映射
- `ToHerbItemDto` — **完全手写**（10 字段），原因：`ProcessingMethod` / `DecocteMethod` 属性名与 Entity 不一致

**优化建议**:
1. **ToHerbItemDto**: 可改用 `[MapProperty]` 注解让 Mapperly 处理属性名映射，消除手写（`ProcessingMethod→ProcessingMethod`, `DecocteMethod→DecocteMethod` — 实际同名，手写不必要）
2. **ToFormulaDetailDto**: `Category` 回退逻辑可在 DTO 层解决（DTO 构造函数/默认值）；`TotalPrice = 0` 可用 `[MapperIgnoreTarget]` + DTO 默认值替代
3. **Herbs 嵌套映射**: Mapperly 支持嵌套对象映射，若 Herbs 属性类型对齐可转为自动

---

### 2.5 ⚠️ MedicalCaseMapper — Enrich 模式（高复杂度）

| 属性 | 值 |
|------|-----|
| 文件 | `src/Server/Modules/LYBT.Module.MedicalCases/Mappers/MedicalCaseMapper.cs` |
| 行数 | 183 |
| 方法数 | 10 |
| 自动/手动 | 7/3 (70% 自动，但手动逻辑最重) |
| 复杂度 | ★★★★☆ |

这是已知问题的核心——**手动 Enrich 逻辑**。

| 方法 | 类型 | MapperIgnore 数 | 说明 |
|------|------|-----------------|------|
| `ToListDto` | 自动 | 6 target | 忽略 CaseNumber/PatientGender/PatientAge/Diagnosis/HasConsultation/HasPrescription |
| `ToDetailDto` | 自动 | 13 target | 忽略嵌套/计算字段 |
| `ToConsultationDetailDto` | 自动 | 4 target | 忽略 PatientId/UserId/PatientName/DoctorName |
| `ToPrescriptionDetailDto` | 自动 | 7 target | 忽略计算字段+Items |
| `ToPrescriptionItemDto` | 自动 | 5 target | 忽略 TotalPrice/TotalWeight/Subtotal/Notes/Role |
| `MapToMedicalCaseDetailDto` | **手动** | — | 调用 ToDetailDto → 手动填充 5 字段 → 嵌套 Enrich |
| `EnrichConsultationDetailDto` | **手动** | — | Mapperly 映射 + 补充 5 个父级上下文字段 |
| `EnrichPrescriptionDetailDto` | **手动** | — | Mapperly 映射 + 计算字段 + Items 映射含金额快照 |

**根因分析 — MedicalCaseDetailDto 字段来源分散**:

| 字段来源 | 示例字段 | 映射方式 |
|----------|----------|----------|
| Entity 直接映射 | Id, PatientName, DoctorName, CaseStatus | Mapperly ✓ |
| Entity 计算属性 | CaseNumber | Mapperly 可处理但被 Ignore ✗ |
| 导航属性提取 | Diagnosis, PresentIllness | 手动 Enrich |
| 聚合计算字段 | SingleDosePrice, TotalPrice, TotalWeight | 手动 Enrich |
| 条件判断 | ConsultationId, PrescriptionId | 手动 Enrich |

**EnrichPrescriptionDetailDto 详细分析**:
```
1. ToPrescriptionDetailDto(prescription)           — Mapperly 自动映射基础字段
2. dto.Items = prescription.Items?.Select(...)      — 手动映射 Items 列表
   └─ ToPrescriptionItemDto(item)                   — Mapperly 自动映射每项
   └─ itemDto.Subtotal = item.Amount                — 手动：金额快照
   └─ itemDto.TotalPrice = item.Amount * dosageCount — 手动：金额快照
3. dto.SingleDosePrice = Items.Sum(Amount)          — 手动：聚合计算
4. dto.TotalPrice = SingleDosePrice * Dosage * Disc — 手动：聚合计算
5. dto.TotalWeight = Items.Sum(Dosage)              — 手动：聚合计算
6. dto.Status = Enabled                             — 手动：常量赋值
```

**优化建议（优先级排序）**:

| # | 建议 | 预期收益 | 难度 |
|---|------|----------|------|
| 1 | CaseNumber 移除 `[MapperIgnoreTarget]`，让 Mapperly 自动映射 | -1 手动字段 | 低 |
| 2 | Diagnosis/PresentIllness 用 `[MapProperty]` + 导航属性深度映射 | -2 手动字段 | 中 |
| 3 | ConsultationId/PrescriptionId 条件逻辑移到 DTO 构造函数 | -2 手动字段 | 中 |
| 4 | 计算字段（SingleDosePrice/TotalPrice/TotalWeight）移到 DTO 的 `init` 访问器或保留手动 | 计算逻辑合理 | — |
| 5 | Items 映射抽取为独立 `MapPrescriptionItems` 方法减少重复 | 代码整洁 | 低 |

---

## 3. Desktop Mapper 逐个分析

### 3.1 PatientMapper (Desktop) — 干净的编辑上下文模式

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mappers/PatientMapper.cs` |
| 行数 | 81 |
| 方法数 | 3 (ToEditContext, ToInputDto, ApplyToDetailModel) |
| 自动/手动 | 0/3 (100% 手动) |
| 复杂度 | ★★☆☆☆ |

**分析**:
- `ToEditContext` — 手动，PinYinCode 回退逻辑（`dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.Name)`）
- `ToInputDto` — 手动，Trim 行为 + Id 空值处理
- `ApplyToDetailModel` — 手动回填（8 字段）

**模式**: 三个方法职责清晰，无冗余。可作为 Desktop Mapper 的参考模式。

---

### 3.2 HerbDetailModelMapper — PinYin 回退

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.Catalog/Mappers/HerbDetailModelMapper.cs` |
| 行数 | 104 |
| 方法数 | 4 (ToItemCore, ToItem, ToEditContext, ToInputDto) |
| 自动/手动 | 1/3 (25% 自动) |
| 复杂度 | ★★★☆☆ |

**分析**:
- `ToItemCore` — Mapperly 自动（忽略审计字段 + UI 状态字段）
- `ToItem` — 手动包装：PinYinCode 回退（`dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.Name)`）
- `ToEditContext` — 完全手写（14 字段），PinYinCode 回退
- `ToInputDto` — 完全手写（12 字段 + Trim），空值 Trim 防御

**优化建议**:
- `ToEditContext` 可部分用 Mapperly 自动映射，PinYinCode 单独手动处理
- `ToInputDto` 的 Trim 行为是合理手写（Mapperly 不支持 Trim）

---

### 3.3 ⚠️ FormulaDetailModelMapper — 多向映射+大量 Ignore

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.Catalog/Mappers/FormulaDetailModelMapper.cs` |
| 行数 | 206 |
| 方法数 | 8 (ToItemCore, ToItem, ToDtoCore, ToDto, ToInputDtoCore, ToInputDto, ToEditContext) |
| 自动/手动 | 3/5 (38% 自动) |
| 复杂度 | ★★★★☆ |

**分析**:

| 方法 | Ignore 数 | 手动逻辑 |
|------|-----------|----------|
| `ToItemCore` | 15 (7 source + 8 target) | — |
| `ToItem` | 0 | Herbs 集合手动映射（6 字段 × N） |
| `ToDtoCore` | 15 (6 source + 9 target) | — |
| `ToDto` | 0 | Herbs 集合手动映射（6 字段 × N） |
| `ToInputDtoCore` | 17 (11 source + 6 target) | — |
| `ToInputDto` | 0 | Herbs 集合 + Id 空值处理 |
| `ToEditContext` | 0 | 完全手写（8 字段 + Herbs） |

**FormulaHerbItem 三向重复映射**:

```
ToItem:       DTO.Herbs → Model.Herbs (6 字段手动)
ToDto:        Model.Herbs → DTO.Herbs (6 字段手动，字段名同)
ToEditContext: DTO.Herbs → Context.Herbs (6 字段手动，字段名同)
```

三个方法中 `FormulaHerbItemModel` 的构造完全相同（HerbId, HerbName, Dosage, Unit, ProcessingMethod, DecocteMethod），是严重的重复。

**优化建议**:

| # | 建议 | 预期收益 | 难度 |
|---|------|----------|------|
| 1 | 抽取 `ToFormulaHerbItemModel(FormulaHerbItemDto)` 和 `ToFormulaHerbItemDto(FormulaHerbItemModel)` 通用方法 | 消除 3 处重复，-30 行 | 低 |
| 2 | 或用 Mapperly `[CreateNew]` 嵌套映射 + `[MapperIgnore]` 控制集合 | 全自动 | 中 |
| 3 | ToEditContext 改为 Mapperly 自动映射（Herbs 除外） | -8 行手写 | 低 |

---

### 3.4 ConsultationMapper — 标准 Core+包装模式

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/ConsultationMapper.cs` |
| 行数 | 127 |
| 方法数 | 6 (ToItemCore, ToItem, ToDtoCore, ToDto, ToInputDto) |
| 自动/手动 | 3/3 (50% 自动) |
| 复杂度 | ★★★☆☆ |

**分析**:
- 三对 Core+包装模式：ToItemCore→ToItem, ToDtoCore→ToDto, ToInputDto
- Core 方法忽略 UI 状态字段（IsSelected, IsExpanded, IsDiagnosisComplete, DisplayText, ValidationMessage）
- 包装方法处理 null 防御和 CreatedBy 清空

**优化建议**: 结构清晰，无严重问题。ToInputDto 的 13 个 IgnoreSource 可考虑用 `RequiredMappingStrategy.Source` 反向控制。

---

### 3.5 ⚠️ MedicalCaseDetailModelMapper — 最高复杂度

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMapper.cs` |
| 行数 | 240 |
| 方法数 | 9 (ToItemCore, ToItem, ToInputDtoCore, ToInputDto×2, ToPrescriptionItemModel, ToPrescriptionItemDto, MapToPrescriptionInput) |
| 自动/手动 | 4/5 (44% 自动) |
| 复杂度 | ★★★★★ |

**MapperIgnore 注解统计**:

| 方法 | Source Ignore | Target Ignore | 总计 |
|------|---------------|---------------|------|
| `ToItemCore` | 14 | 12 | **26** |
| `ToInputDtoCore` | 19 | 6 | **25** |
| `ToInputDto(Dto)` | 0 | 3 | 3 |
| `MapToPrescriptionInput` | 0 | 2 | 2 |
| **合计** | **33** | **23** | **56** |

**ToItem 方法分析 — 嵌套 DTO 手动提取（40 行）**:

```
1. ToItemCore(dto)                    — Mapperly 自动映射基础字段
2. model.CaseNumber = dto.CaseNumber  — 手动
3. if (dto.Consultation != null)      — 手动：从嵌套 Consultation 提取 4 字段
   ├─ PresentIllness
   ├─ TongueDiagnosis
   ├─ PulseDiagnosis
   └─ TcmDiagnosis
4. if (dto.Prescription != null)      — 手动：从嵌套 Prescription 提取 5 字段
   ├─ HerbCount (Items.Count)
   ├─ DoseCount
   ├─ Discount
   ├─ ReferencedFormulas (null→"自拟方")
   └─ PrescriptionItems (ObservableCollection 转换)
```

**⚠️ 严重重复 — PrescriptionItemDto↔Model 双向映射**:

以下两个方法在 `MedicalCaseDetailModelMapper` 和 `PrescriptionMapper` 中**完全相同**（14 字段逐字段映射）:

```csharp
// 出现在 MedicalCaseDetailModelMapper (private) 和 PrescriptionMapper (public)
static PrescriptionItemModel ToPrescriptionItemModel(PrescriptionItemDto dto)
static PrescriptionItemDto ToPrescriptionItemDto(PrescriptionItemModel model)
```

两个文件中这两个方法的实现逐行一致（Id, PrescriptionId, HerbId, HerbName, Unit, UnitPrice, Dosage, TotalPrice, TotalWeight, Subtotal, Usage, DecocteMethod, Role, Remark）。

**优化建议**:

| # | 建议 | 预期收益 | 难度 |
|---|------|----------|------|
| 1 | **抽取 `PrescriptionItemMapper` 共享类**，两个 Mapper 引用同一个 | 消除 2 处完全重复（-40 行） | 低 |
| 2 | MedicalCaseDetailModelMapper 用 `[UserMapping]` 调用 PrescriptionMapper 的静态方法 | 消除重复 | 低 |
| 3 | ToItemCore 的 26 个 Ignore 可通过 DTO 拆分减少（基础字段 vs 计算字段分离） | 降低复杂度 | 中 |

---

### 3.6 ⚠️ PrescriptionMapper — 高 Ignore 密度

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/PrescriptionMapper.cs` |
| 行数 | 232 |
| 方法数 | 8 (ToItemCore, ToItem, ToDtoCore, ToDto, ToInputDtoCore, ToInputDto, ToPrescriptionItemModel, ToPrescriptionItemDto) |
| 自动/手动 | 3/5 (38% 自动) |
| 复杂度 | ★★★★☆ |

**MapperIgnore 注解统计**:

| 方法 | Source Ignore | Target Ignore | 总计 |
|------|---------------|---------------|------|
| `ToItemCore` | 2 | 10 | **12** |
| `ToDtoCore` | 9 | 2 | **11** |
| `ToInputDtoCore` | 15 | 4 | **19** |
| **合计** | **26** | **16** | **42** |

**ToInputDto 分析 — 最复杂的保存路径**:

```
1. ToInputDtoCore(item)                    — Mapperly 自动映射可写字段
2. dto.Id = item.Id == Guid.Empty ? null   — 手动：空 Guid→null（创建语义）
3. dto.NeedsPrescription = item.HasItems   — 手动：计算属性映射
4. dto.TotalPrice = item.TotalPrice        — 手动：价格传递
5. dto.Items = item.Items?.Select(...)     — 手动：Items 集合映射
   └─ Subtotal = h.Dosage * h.UnitPrice    — 手动：金额计算
```

**与 MedicalCaseDetailModelMapper 的 PrescriptionItem 重复**: 见 §3.5。

**优化建议**:
1. 抽取 `PrescriptionItemMapper` 共享类（同 §3.5 #1）
2. `Id == Guid.Empty ? null` 模式在 FormulaDetailModelMapper、MedicalCaseDetailModelMapper、PrescriptionMapper 中各出现一次，可抽取扩展方法
3. ToInputDto 的 19 个 Source Ignore 反映了 UI 状态字段的泛滥，考虑 DTO 设计是否过于扁平

---

### 3.7 UserMapper (Desktop) — 标准三向模式

| 属性 | 值 |
|------|-----|
| 文件 | `src/Client/Desktop/Modules/LYBT.Desktop.Users/Mappers/UserMapper.cs` |
| 行数 | 120 |
| 方法数 | 6 (ToDetailModelCore, ToDetailModel, ToEditContext, ToInputDto, ApplyToDetailModel) |
| 自动/手动 | 1/5 (17% 自动) |
| 复杂度 | ★★★☆☆ |

**分析**:
- `ToDetailModelCore` — Mapperly 自动（忽略 PinYinCode + UI 状态字段）
- `ToDetailModel` — 手动包装：PinYinCode 回退（`dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.RealName)`）
- `ToEditContext` — 完全手写（13 字段 + PinYinCode 回退）
- `ToInputDto` — 完全手写（10 字段 + Trim + Id 空值）
- `ApplyToDetailModel` — 手动回填（13 字段），PinYinCode 特殊处理（保留原值当 DTO 为空）

**优化建议**:
- `ToEditContext` 可改为 Mapperly 自动映射（PinYinCode 单独处理），减少 ~13 行手写
- `ApplyToDetailModel` 模式（回填）与 PatientMapper 一致，可抽取基类

---

## 4. 跨 Mapper 冗余分析

### 4.1 ⚠️ PrescriptionItemDto↔Model 映射重复（严重）

**重复位置**:
1. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseDetailModelMapper.cs`（L179-222，private static）
2. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/PrescriptionMapper.cs`（L186-229，public static）

**重复内容**: 两个文件中 `ToPrescriptionItemModel` 和 `ToPrescriptionItemDto` 的实现逐行一致，均为 14 字段的纯属性复制。

**额外消费者**:
- `MedicalCaseCommandsViewModel.cs` 调用 `PrescriptionMapper.ToPrescriptionItemModel`
- `HerbListControlViewModel.cs` 有独立的 `ToPrescriptionItemDto`（通过 IHerbItemEditable 接口）
- `PrescriptionImportExtensions.cs` 有独立的 DTO 构造

**建议**: 创建 `PrescriptionItemMapper` 共享静态类，两处引用同一实现。

---

### 4.2 ⚠️ FormulaHerbItem 映射重复（严重）

**重复位置** (均在 `FormulaDetailModelMapper.cs` 内):

| 方法 | 方向 | 行号 |
|------|------|------|
| `ToItem` | DTO→Model | L65-73 |
| `ToDto` | Model→DTO | L111-119 |
| `ToEditContext` | DTO→Context.Model | L169-177 |
| `ToInputDto` | Model→InputDto | L195-202 |

**重复内容**: `FormulaHerbItemModel` 的构造（HerbId, HerbName, Dosage, Unit, ProcessingMethod, DecocteMethod）在三处完全相同。`ToInputDto` 方向不同（缺少 HerbName/OriginalHerbName/IsValidated），但构造逻辑相同。

**建议**: 抽取 `ToFormulaHerbItemModel` / `ToFormulaHerbItemDto` 通用方法。

---

### 4.3 🔄 PinYinCode 回退模式重复（5 处）

**模式**: `PinYinCode ?? PinYinHelper.GetPinYinCode(name)`

| # | Mapper | 方法 | 源名称 |
|---|--------|------|--------|
| 1 | HerbDetailModelMapper | ToItem | dto.Name |
| 2 | HerbDetailModelMapper | ToEditContext | dto.Name |
| 3 | PatientMapper/Desktop | ToEditContext | dto.Name |
| 4 | UserMapper | ToDetailModel | dto.RealName |
| 5 | UserMapper | ToEditContext | dto.RealName |

**建议**: 将 PinYinCode 回退逻辑抽取为 `PinYinHelper.EnsurePinYinCode(string? pinYin, string name)` 扩展方法，在 DTO 层或 Model 构造函数中统一处理。

---

### 4.4 🔄 Id 空值转换模式重复（3 处）

**模式**: `Id == Guid.Empty ? null : Id`

| # | Mapper | 方法 |
|---|--------|------|
| 1 | FormulaDetailModelMapper | ToInputDto |
| 2 | MedicalCaseDetailModelMapper | ToInputDto |
| 3 | PrescriptionMapper | ToInputDto |

**建议**: 抽取扩展方法 `Guid?.OrNullIfEmpty()` 或在 InputDto 基类处理。

---

### 4.5 🔄 ApplyToDetailModel 回填模式重复（2 处）

| # | Mapper | 字段数 |
|---|--------|--------|
| 1 | PatientMapper/Desktop | 8 字段 |
| 2 | UserMapper/Desktop | 13 字段 |

两者都是 `void ApplyToDetailModel(TModel target, TDto source)` 模式，逐字段回填。

**建议**: 考虑抽取 `IApplyable<TDto>` 接口或基类方法，但因字段差异较大，当前重复可接受。

---

### 4.6 🔄 Core+包装映射模式（5 处）

Desktop Mapper 普遍采用 `ToXxxCore` (Mapperly partial) + `ToXxx` (手写包装) 模式：

| Mapper | Core→包装对 |
|--------|------------|
| HerbDetailModelMapper | ToItemCore→ToItem |
| FormulaDetailModelMapper | ToItemCore→ToItem, ToDtoCore→ToDto, ToInputDtoCore→ToInputDto |
| ConsultationMapper | ToItemCore→ToItem, ToDtoCore→ToDto |
| MedicalCaseDetailModelMapper | ToItemCore→ToItem, ToInputDtoCore→ToInputDto |
| PrescriptionMapper | ToItemCore→ToItem, ToDtoCore→ToDto, ToInputDtoCore→ToInputDto |
| UserMapper | ToDetailModelCore→ToDetailModel |

**分析**: 这是项目的标准模式（Core 由 Mapperly 生成，包装处理 null 防御/集合转换/PinYin 回退等），不是冗余，而是合理的设计选择。

---

## 5. Service 层手动映射分析

### 5.1 MedicalCaseQueryService.QueryRecentAsync — Detail→ListDto 手动映射

**位置**: `src/Server/Modules/LYBT.Module.MedicalCases/Services/MedicalCaseQueryService.cs` L312-329

```csharp
// 从DetailDto手动映射为ListDto（DetailDto包含ListDto的所有字段）
var listDtos = recentCases.Select(detail => new MedicalCaseListDto
{
    Id = detail.Id,
    CaseNumber = detail.CaseNumber,
    PatientId = detail.PatientId,
    // ... 14 个字段
}).ToList();
```

**问题**: 14 个字段逐字段手动映射，且 Mapperly 已有 `ToListDto(MedicalCase entity)` 方法，但此处输入是 `MedicalCaseDetailDto` 而非 `MedicalCase` 实体。

**建议**: 在 MedicalCaseMapper 中增加 `MedicalCaseDetailDto → MedicalCaseListDto` 的映射方法，消除 Service 层的手动映射。

---

### 5.2 MedicalCaseMapper.EnrichPrescriptionDetailDto — 计算逻辑在 Mapper 中

**位置**: `src/Server/Modules/LYBT.Module.MedicalCases/Mappers/MedicalCaseMapper.cs` L156-178

```csharp
dto.SingleDosePrice = prescription.Items?.Sum(x => x.Amount) ?? 0;
dto.TotalPrice = dto.SingleDosePrice * prescription.DosageCount * prescription.Discount;
dto.TotalWeight = prescription.Items?.Sum(x => x.Dosage) ?? 0;
```

**问题**: Mapper 层包含了业务计算逻辑（金额聚合、折扣计算），违反 Mapper 的纯映射职责。

**建议**: 将计算字段移到：
- Option A: DTO 的构造函数/init 访问器（若计算逻辑稳定）
- Option B: 专用 `PrescriptionDtoCalculator` 静态类（Mapper 调用）
- Option C: 保留当前模式（计算逻辑集中在一个方法中，可接受）

---

## 6. 优化建议汇总

### 6.1 高优先级（可立即实施）

| # | 问题 | 建议 | 涉及文件 | 预期收益 |
|---|------|------|----------|----------|
| H1 | PrescriptionItemDto↔Model 双向映射重复 2 处 | 抽取 `PrescriptionItemMapper` 共享类 | MedicalCaseDetailModelMapper, PrescriptionMapper | -40 行重复代码 |
| H2 | FormulaHerbItem 映射重复 3 处 | 抽取通用方法 | FormulaDetailModelMapper | -20 行重复代码 |
| H3 | MedicalCaseQueryService 手动 Detail→ListDto | 增加 Mapper 方法 | MedicalCaseMapper, MedicalCaseQueryService | -17 行手动映射 |
| H4 | MedicalCaseMapper.ToDetailDto 不必要的 Ignore | 移除 CaseNumber 的 IgnoreTarget | MedicalCaseMapper | -1 手动字段 |

### 6.2 中优先级（架构改善）

| # | 问题 | 建议 | 涉及文件 | 预期收益 |
|---|------|------|----------|----------|
| M1 | MedicalCaseMapper Enrich 模式（50% 字段手动） | 用 `[MapProperty]` + 导航属性深度映射减少手动 | MedicalCaseMapper | -5 手动字段 |
| M2 | PinYinCode 回退逻辑重复 5 处 | 抽取 `PinYinHelper.EnsurePinYinCode` | 5 个 Desktop Mapper | 减少重复 |
| M3 | Id 空值转换重复 3 处 | 抽取 `Guid?.OrNullIfEmpty()` 扩展 | 3 个 Desktop Mapper | 减少重复 |
| M4 | FormulaDetailModelMapper ToEditContext 完全手写 | 改为 Mapperly 自动映射（Herbs 除外） | FormulaDetailModelMapper | -8 行手写 |
| M5 | ToInputDto 的高密度 Ignore（15-19 个） | 考虑 DTO 设计优化（拆分 UI 状态 vs 业务字段） | PrescriptionMapper, ConsultationMapper | 降低复杂度 |

### 6.3 低优先级（长期优化）

| # | 问题 | 建议 | 涉及文件 | 预期收益 |
|---|------|------|----------|----------|
| L1 | IdentityMapper ToListDto/ToDetailDto 手写 | 用 `[MapProperty]` + ToNonNullString 自动化 | IdentityMapper | -30 行手写 |
| L2 | CatalogDtoMapper.ToHerbItemDto 手写 | 属性名同名，可用 Mapperly 自动 | CatalogDtoMapper | -15 行手写 |
| L3 | EnrichPrescriptionDetailDto 计算逻辑在 Mapper 中 | 移到 DTO 构造或 Calculator 类 | MedicalCaseMapper | 职责清晰 |

---

## 7. 总结

### 当前状态

1. **Server Mapper** 整体质量良好：RegistrationMapper 和 PatientMapper 是干净模板，IdentityMapper 和 CatalogDtoMapper 合理使用手写。MedicalCaseMapper 是主要技术债（Enrich 模式）。

2. **Desktop Mapper** 普遍采用 Core+包装模式（合理设计），但存在大量 Ignore 注解（总计 56+42=98 个）和 2 处严重重复。

3. **转换链断点**: MedicalCase 模块的 Entity→DTO→Desktop Model 转换链最复杂，因为 MedicalCase 是 DDD 聚合根，嵌套 Consultation 和 Prescription，字段来源分散。

### 核心发现

- **2 处严重重复**: PrescriptionItem 双向映射（2 文件 × 2 方法 = 4 个方法中 2 个完全重复）、FormulaHerbItem 三向映射（3 处相同构造）
- **1 处 Service 层泄漏**: MedicalCaseQueryService 中 14 字段手动 Detail→ListDto 映射
- **3 处跨文件模式重复**: PinYinCode 回退（5 处）、Id 空值转换（3 处）、ApplyToDetailModel（2 处）
- **MedicalCaseMapper Enrich 模式**: 3 个手动 Enrich 方法处理了 ~15 个 Mapperly 无法自动映射的字段，是整个转换链的最大复杂度来源

### 行动优先级

```
立即: H1 (PrescriptionItem 抽取) → H2 (FormulaHerbItem 抽取) → H3 (Detail→ListDto)
短期: M1 (MedicalCaseMapper 减少 Enrich) → M2 (PinYinCode 统一) → M3 (Id 扩展方法)
长期: M4 (FormulaEditContext Mapperly) → L1 (IdentityMapper 自动化) → L3 (计算逻辑分离)
```

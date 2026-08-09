# DTO/Entity 实例对齐审计报告

> 日期: 2026-08-09 | 审计人: 技术总监（Hermes）
> 范围: Server Entity ↔ Desktop Model ↔ Shared DTO 三层字段对齐
> 依据: 代码实际定义（非文档声明）

---

## 一、审计目标

1. **字段定义位置规范**：哪些字段该在哪层定义
2. **命名一致**：同义字段 Server/DTO/Desktop 使用同一名称
3. **属性一致**：Required/StringLength/Range 等特性 Server Entity 与 Desktop Model 对齐

---

## 二、命名不一致（必须统一）

| # | Entity 字段 | Desktop/DTO 字段 | 模块 | 建议统一为 |
|---|------------|-----------------|------|-----------|
| N-1 | `ApplicationUser.LastLoginAt` | `UserDetailModel.LastLoginTime` / `UserListDto.LastLoginTime` / `UserDetailDto.LastLoginTime` | User | **LastLoginAt**（Entity 为准，DTO/Desktop 跟随） |
| N-2 | `Formula.Indication` | `FormulaListDto.Indications` / `FormulaDetailDto.Indications` | Formula | **Indication**（Entity 为准，Mapperly `[MapProperty]` 处理） |
| N-3 | `FormulaHerbItem.ProcessingMethod` | `FormulaHerbItemDto.Preparation` + `ProcessingMethod`（双字段并存） | FormulaHerbItem | **ProcessingMethod**（Entity 为准，删 Preparation 或保留为兼容别名） |

---

## 三、属性不一致（必须对齐）

### 3.1 Herb — 4 项不一致

| 字段 | Entity 属性 | Desktop Model 属性 | 差异 | 建议 |
|------|-----------|-------------------|------|------|
| Name | `[StringLength(100, MinLength=1)]` + `[Required]` | `[Required]` + `[StringLength(ValidationConstants.NameMaxLength)]` (100) | **Entity 有 MinLength=1，Desktop 无** | Desktop 补 `MinLength=1` |
| Category | `[StringLength(50)]` | `[StringLength(ValidationConstants.NameMaxLength)]` (100) | **长度不一致：50 vs 100** | Entity 改 100 或 Desktop 改 50，需决策 |
| Origin | `[StringLength(100)]` | `[StringLength(ValidationConstants.AddressMaxLength)]` (200) | **长度不一致：100 vs 200** | Entity 改 200 或 Desktop 改 100，需决策 |
| Spec | `[StringLength(100)]` | `[StringLength(ValidationConstants.NameMaxLength)]` (100) | 一致（巧合同值） | ✅ 一致 |

### 3.2 Patient — 2 项不一致

| 字段 | Entity 属性 | Desktop Model 属性 | 差异 | 建议 |
|------|-----------|-------------------|------|------|
| Name | `[StringLength(100)]` | `[StringLength(ValidationConstants.NameMaxLength)]` (100) | 一致 | ✅ |
| IdNumber | `[StringLength(50)]` | `[StringLength(ValidationConstants.IdCardMaxLength)]` (18) + 正则 `^[1-9]\d{5}(19\|20)\d{2}...` | **长度不一致：50 vs 18** + Desktop 有严格正则 | Entity 保持 50（兼容旧数据）；Desktop 18 是正确的 UI 校验 |
| PhoneNumber | `[StringLength(20)]` | `[StringLength(ValidationConstants.PhoneMaxLength)]` (20) | 一致 | ✅ |

### 3.3 User — 1 项不一致

| 字段 | Entity 属性 | Desktop Model 属性 | 差异 | 建议 |
|------|-----------|-------------------|------|------|
| UserName | `IdentityUser.UserName` (nvarchar(256)) + `Create()` 校验 3-32 | `[StringLength(ValidationConstants.UserNameMaxLength)]` (50) MinLength=3 | **长度不一致：32 vs 50** | Desktop 改 32 对齐 Entity 的 `Create()` 校验 |

---

## 四、字段定义位置规范

### 4.1 三层定义原则

| 层 | 职责 | 定义内容 | 示例 |
|----|------|---------|------|
| **Entity** (Server) | 数据库 Schema + 业务规则 | 所有持久化字段 + Domain 方法 + 验证 | `Herb.Name [StringLength(100)]` |
| **DTO** (Shared) | API 传输契约 | 按 Use Case 裁剪的字段子集 + 传输验证 | `HerbListDto.Name` (无特性) |
| **Model** (Desktop) | UI 绑定 + 本地验证 | Entity 字段子集 + UI 计算属性 + WPF 验证 | `HerbDetailModel.Name [StringLength(100)] [Required]` |

### 4.2 字段归属决策规则

| 规则 | 说明 |
|------|------|
| **持久化字段 → Entity 定义** | Name/Status/CreatedAt 等数据库列 |
| **计算属性 → 使用方定义** | Age（Desktop 计算）、HerbCount（Desktop 计算）、IsNew（Desktop 计算） |
| **UI 专有字段 → Desktop Model 定义** | DiagnosisSummary、PrescriptionSummary、StatusText |
| **服务端审计字段 → 仅 Entity** | CreatedBy、UpdatedBy、IsDeleted、RowVersion |
| **DTO 独有字段 → 必须有 Entity 对应** | 禁止 DTO 虚构 Entity 不存在的字段（Description/Source/Contraindications 违规） |

### 4.3 特性对齐规则

| 规则 | 说明 |
|------|------|
| **Entity 是 SSOT** | Entity 的 `[StringLength]`/`[Required]`/`[Range]` 是权威定义 |
| **Desktop Model 跟随 Entity** | Desktop Model 的特性必须 ≤ Entity（更严格可以，更宽松禁止） |
| **DTO 无特性** | DTO 仅用于传输，验证由 Entity/DataAnnotations 或 FluentValidation 执行 |
| **共用常量** | `ValidationConstants` 中的常量必须与 Entity `[StringLength]` 值一致 |

---

## 五、完整字段对齐矩阵

### 5.1 Herb

| 字段 | Entity 类型 | Entity 特性 | Desktop 类型 | Desktop 特性 | DTO 类型 | 对齐 |
|------|-----------|-----------|------------|-------------|---------|:---:|
| Id | Guid | — | Guid | — | Guid | ✅ |
| Name | string | Required, StringLength(100,Min=1) | string | Required, StringLength(100) | string | ⚠️ MinLength |
| PinYinCode | string? | StringLength(50) | string | — | string? | ✅ |
| Category | string? | StringLength(**50**) | string? | StringLength(**100**) | string? | 🔴 50vs100 |
| Properties | string? | StringLength(100) | string? | — | string? | ✅ |
| Origin | string? | StringLength(**100**) | string? | StringLength(**200**) | string? | 🔴 100vs200 |
| Spec | string? | StringLength(100) | string? | StringLength(100) | string? | ✅ |
| Unit | string | Required, StringLength(10) | string | Required | string | ✅ |
| Price | decimal | — | decimal | Required, Range(0.01,100000) | decimal | ✅ |
| CostPrice | decimal? | — | decimal? | Range(0,100000) | decimal? | ✅ |
| Effect | string? | StringLength(500) | string? | StringLength(2000) | string? | ⚠️ 500vs2000 |
| Usage | string? | StringLength(500) | string? | StringLength(200/500) | string? | ✅ |
| Remark | string? | StringLength(500) | string? | StringLength(1000) | string? | ⚠️ 500vs1000 |
| Status | CommonStatus | — | CommonStatus | — | CommonStatus | ✅ |
| CreatedBy | Guid? | — | — | — | Guid? | ✅ 仅Entity+DTO |
| CreatedAt | DateTime | — | DateTime? | — | DateTime | ⚠️ DateTime vs DateTime? |

### 5.2 Formula

| 字段 | Entity 类型 | Entity 特性 | Desktop 类型 | Desktop 特性 | DTO 类型 | 对齐 |
|------|-----------|-----------|------------|-------------|---------|:---:|
| Id | Guid | — | Guid | — | Guid | ✅ |
| Name | string | Required, StringLength(200) | string | Required, StringLength(**100**) | string | 🔴 200vs100 |
| Effect | string? | StringLength(500) | string? | StringLength(500) | string? | ✅ |
| Indication | string? | StringLength(1000) | — | — | string? (Indications) | 🔴 Desktop 缺 |
| Usage | string? | StringLength(500) | string? | StringLength(500) | string | ✅ |
| Remark | string? | StringLength(500) | string? | StringLength(**1000**) | string? | ⚠️ 500vs1000 |
| Property | string? | StringLength(300) | string? | StringLength(**100**) | string? | 🔴 300vs100 |
| Status | CommonStatus | — | CommonStatus | — | CommonStatus | ✅ |
| IsShared | bool | — | bool | — | bool | ✅ |
| ValidationStatus | FormulaValidationStatus | — | — | — | FormulaValidationStatus | 🔴 Desktop 缺 |
| Category | string? | StringLength(50) | string? | StringLength(**100**) | string? | ⚠️ 50vs100 |
| FormulaType | FormulaType | — | — | — | — | 🔴 Desktop 缺 |
| UserId | Guid? | — | — | — | — | — Desktop 不需要 |
| Source | — | — | string? | — | string? | 🔴 Entity 无 |
| Description | — | — | — | — | string? | 🔴 Entity 无 |

### 5.3 Patient

| 字段 | Entity 类型 | Entity 特性 | Desktop 类型 | Desktop 特性 | DTO 类型 | 对齐 |
|------|-----------|-----------|------------|-------------|---------|:---:|
| Id | Guid | — | Guid | — | Guid | ✅ |
| Name | string | Required, StringLength(100) | string | Required, StringLength(100) | string | ✅ |
| PinYinCode | string? | StringLength(50) | string | — | string? | ✅ |
| Gender | Gender | — | Gender | — | Gender | ✅ |
| BirthDate | DateTime? | — | DateTime? | — | DateTime? | ✅ |
| Age | int? | NotMapped (computed) | int? | computed | int? (ListDto only) | ✅ |
| IdNumber | string? | StringLength(**50**) | string? | StringLength(**18**)+Regex | string? | ⚠️ 50vs18 |
| PhoneNumber | string? | StringLength(20) | string? | StringLength(20)+Phone | string? | ✅ |
| Status | CommonStatus | — | CommonStatus | — | CommonStatus | ✅ |
| CreatedBy | Guid? | — | — | — | Guid? | ✅ 仅Entity+DTO |
| CreatedAt | DateTime | — | DateTime? | — | DateTime | ⚠️ DateTime vs DateTime? |

### 5.4 User (ApplicationUser)

| 字段 | Entity 类型 | Entity 特性 | Desktop 类型 | Desktop 特性 | DTO 类型 | 对齐 |
|------|-----------|-----------|------------|-------------|---------|:---:|
| Id | Guid | — | Guid | — | Guid | ✅ |
| UserName | string | Identity (nvarchar 256), Create() 3-32 | string | StringLength(**50**), Min=3 | string | 🔴 32vs50 |
| RealName | string | StringLength(100) | string | Required, StringLength(100) | string | ✅ |
| PinYinCode | string? | StringLength(50) | string | — | string? | ✅ |
| PhoneNumber | string? | Identity (nvarchar) | string? | StringLength(20)+Phone | string? | ✅ |
| Email | string? | Identity (nvarchar) | string? | EmailAddress | string? | ✅ |
| Role | UserRole | — | UserRole | — | UserRole | ✅ |
| IsSysAdmin | bool | — | — | — | — | 🔴 Desktop 缺 |
| Status | CommonStatus | — | CommonStatus | — | CommonStatus | ✅ |
| MustChangeOnNextLogin | bool | — | — | — | — | 🔴 Desktop 缺 |
| LastLoginAt | DateTime? | — | DateTime? (**LastLoginTime**) | — | DateTime? (**LastLoginTime**) | 🔴 命名不一致 |
| RegistrationFee | decimal | Column decimal(10,2) | decimal | — | decimal | ✅ |
| Remark | string? | StringLength(500) | string? | StringLength(1000) | string? | ⚠️ 500vs1000 |
| CreatedBy | Guid? | — | — | — | Guid? | ✅ 仅Entity+DTO |
| CreatedAt | DateTime | — | DateTime | — | DateTime | ✅ |
| UpdatedAt | DateTime? | — | DateTime? | — | DateTime? | ✅ |

---

## 六、修复建议汇总

### 🔴 必须修复（命名/长度严重不一致）

| # | 修复项 | 改什么 | 理由 |
|---|--------|--------|------|
| F-1 | **Herb.Category**: Entity `[StringLength(50)]` → `[StringLength(100)]` | Entity 对齐 Desktop | 验方分类名可能超 50 |
| F-2 | **Herb.Origin**: Desktop `[StringLength(200)]` → `[StringLength(100)]` | Desktop 对齐 Entity | 数据库列 nvarchar(100) |
| F-3 | **Formula.Name**: Desktop `[StringLength(100)]` → `[StringLength(200)]` | Desktop 对齐 Entity | Entity 允许 200 |
| F-4 | **Formula.Property**: Desktop `[StringLength(100)]` → `[StringLength(300)]` | Desktop 对齐 Entity | 性味归经可能较长 |
| F-5 | **User.UserName**: Desktop `[StringLength(50)]` → `[StringLength(32)]` | Desktop 对齐 Entity 的 Create() 校验 | 3-32 字符 |
| F-6 | **LastLoginAt → LastLoginTime 统一**: Entity 改 `LastLoginTime` 或 Desktop 改 `LastLoginAt` | 统一命名 | 同义字段必须同名 |

### 🟡 建议修复（特性不一致）

| # | 修复项 | 改什么 |
|---|--------|--------|
| F-7 | **Herb.Name**: Desktop 补 `MinLength=1` | 对齐 Entity |
| F-8 | **Formula.Remark**: Desktop `[StringLength(1000)]` → `[StringLength(500)]` | 对齐 Entity |
| F-9 | **User.Remark**: Desktop `[StringLength(1000)]` → `[StringLength(500)]` | 对齐 Entity |
| F-10 | **Herb.Effect**: Entity `[StringLength(500)]` vs Desktop `[StringLength(2000)]` | 需决策：哪个为准 |
| F-11 | **Formula.Category**: Entity `[StringLength(50)]` vs Desktop `[StringLength(100)]` | 需决策：哪个为准 |

### 📋 不需要修复（有意设计）

| 项 | 理由 |
|----|------|
| Patient.IdNumber Entity 50 vs Desktop 18 | Entity 兼容旧数据，Desktop UI 校验正确 |
| Desktop 省略 CreatedBy/UpdatedBy/IsDeleted/RowVersion | 正确分层：审计字段仅服务端 |
| MedicalCase Desktop Model 展平导航属性 | 正确设计：ViewModel 层数据形状 |
| Registration 无 Desktop Model | DTO 直接绑定 ViewModel，无需中间 Model |

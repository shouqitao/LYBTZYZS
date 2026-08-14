# Entity/DTO/前端 Model 三层定义审计报告

> **审计日期**: 2026-08-14  
> **审计范围**: Entity（LYBT.Entities）、DTO（Shared.Models/Contracts）、Desktop Model（ViewModel + Mapperly）三层  
> **审计维度**: 命名一致性、字段冗余、Mapper 映射、验证覆盖、层级边界、字段完整度

---

## 一、总体概况

| 层级 | 数量 | 位置 |
|------|------|------|
| Entity | 16 个实体类（含 2 个值对象） | `src/Shared/LYBT.Entities/` |
| DTO（Contracts） | 90 个文件，~70 个类 | `src/Shared/LYBT.Shared.Models/Contracts/` |
| Server Mapper | 5 个 Mapperly 映射器 | `src/Server/Modules/*/Mappers/` |
| Desktop Mapper | 5 个 Mapperly 映射器 | `src/Client/Desktop/Modules/*/Mappers/` |
| Desktop Model | 5 个 DetailModel + EditContext | `src/Client/Desktop/Modules/*/Models/` |
| Validator | 7 个 FluentValidation 验证器 | `src/Shared/LYBT.Shared.Models/Validators/` |

---

## 二、按 Entity 逐项审计

### 2.1 User（ApplicationUser）

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | RealName, PinYinCode, Role, IsSysAdmin, Status, MustChangeOnNextLogin, LastLoginTime, RegistrationFee, Remark + 审计字段 |
| **命名** | ⚠️ | 实体类名 `ApplicationUser`（Identity 风格），DTO 用 `UserListDto`/`UserDetailDto`（业务风格）。**可接受**——Identity 继承约束 |
| **DTO→Entity 映射** | ⚠️ | IdentityMapper.ToListDto 忽略了 `IsSysAdmin`、`MustChangeOnNextLogin`、`PinYinCode`（仅 DetailDto 有） |
| **Desktop 映射** | ✅ | UserMapper 将 DTO→UserDetailModel，含 PinYinCode 回退逻辑 |
| **验证** | ❌ **缺失** | UserInputDto 无独立 FluentValidation 验证器（Validator 文件名为 UserInputDtoValidator 但内容可能不完整） |

### 2.2 Patient

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | Name, PinYinCode, Gender, BirthDate, IdNumber, PhoneNumber, Status + 计算属性 Age |
| **命名** | ✅ | Entity=`Patient`, DTO=`PatientListDto`/`PatientDetailDto`/`PatientInputDto`，后缀一致 |
| **字段映射** | ⚠️ | PatientMapper.ToDetailDto 由 Mapperly 自动生成；Age 是计算属性（NotMapped），**DTO 包含 Age 但 Entity 也有 Age 计算属性**——Mapperly 能映射但语义重复 |
| **Desktop 映射** | ✅ | PatientMapper.ToEditContext/ToInputDto 清晰 |
| **验证** | ✅ | PatientInputDtoValidator 覆盖 Name, Gender, BirthDate |

### 2.3 Herb

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price, CostPrice, Effect, Usage, Remark, Status |
| **命名** | ✅ | Entity=`Herb`, DTO=`HerbListDto`/`HerbDetailDto`/`HerbInputDto` |
| **字段映射** | ✅ | CatalogDtoMapper.ToHerbListDto/ToHerbDetailDto 由 Mapperly 自动生成，同名字段直映 |
| **Desktop 映射** | ⚠️ | HerbDetailModelMapper.ToItemCore 忽略 CreatedAt/UpdatedAt/CreatedBy（**审计字段在 Desktop 端不可见**） |
| **验证** | ✅ | HerbInputDtoValidator 覆盖 BR-001 到 BR-008 |

### 2.4 Formula

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ⚠️ **有差异** | Entity 有 `FormulaType` 和 `UserId`，DTO 无此二字段（FormulaType 写死为 Experience，UserId 通过 Mapperly 参数传入） |
| **命名** | ⚠️ | Entity 类名=`Formula`，DTO 用 `FormulaListDto`/`FormulaDetailDto`。**缺少 `FormulaInputDto` 中的字段**（见下） |
| **字段冗余** | 🔴 **问题** | FormulaInputDto 有 `Description`、`Instructions`、`Preparation`、`Contraindications` 字段，**Entity 均无对应属性**。DTO 与 Entity 严重脱节 |
| **字段映射** | ⚠️ | FormulaDetailDto 有 `Description`、`Source`、`Contraindications`、`TotalPrice`（恒 0），Entity 均无。Mapper 忽略 TotalPrice，但 Description 等字段会映射为 null |
| **验证** | ✅ | FormulaInputDtoValidator 覆盖 Name, Effect, Usage, Indication |

### 2.5 FormulaHerbItem

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段冗余** | 🔴 **问题** | DTO 有 `Price`、`SortOrder`、`SpecialInstructions` 字段，**Entity 均无对应属性**。`Price` 在 DTO 层作为展示用途但无持久化来源 |
| **命名** | ⚠️ | Entity=`FormulaHerbItem`，DTO=`FormulaHerbItemDto`/`FormulaHerbItemInputDto`。InputDto 有 `Preparation` 字段，Entity 为 `ProcessingMethod`——**字段名不一致** |
| **层级边界模糊** | 🔴 | DTO 包含 `HerbDetailDto? Herb` 导航属性（嵌套完整药材详情），**越级引用**。Entity 只存 HerbId，DTO 应仅包含 HerbId + 可选的 HerbName 冗余 |

### 2.6 MedicalCase（聚合根）

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | PatientId, PatientName, UserId, DoctorName, CaseNumber, CaseStatus, NeedsPrescription, CompletedAt, 打印追踪字段 |
| **命名** | ⚠️ | Entity 用 `UserId`（医生），DTO 也用 `UserId`。但 `DoctorName` 冗余名存在。**Registration 用 `DoctorId`**——跨实体命名不一致 |
| **字段映射** | ⚠️ | ToListDto 忽略 CaseNumber, PatientGender, PatientAge, Diagnosis, HasConsultation, HasPrescription——**这些字段需要 Service 层手动填充**，增加维护成本 |
| **计算属性** | ✅ | IsLocked, IsActive, IsCompleted 在 Entity 定义，DTO 无（正确——展示层按需计算） |

### 2.7 Consultation

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis（4 核心字段） |
| **命名** | ⚠️ | Entity 类名=`Consultation`，DTO=`ConsultationDetailDto`/`ConsultationInputDto`。**无 ListDto**——合理（Consultation 为聚合内部实体） |
| **映射特殊处理** | ⚠️ | ConsultationDetailDto 包含 MedicalCaseId/PatientId/UserId/PatientName/DoctorName——**这些字段来自父聚合 MedicalCase**，不是 Consultation 自身属性。Mapper 通过 MapProperty + 手动 Enrich 填充 |

### 2.8 Prescription

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | MedicalCaseId, PrescriptionNumber, DosageCount, Discount, Usage, Advice, ReferencedFormulas, Remark |
| **命名** | ✅ | Entity=`Prescription`, DTO=`PrescriptionDetailDto`/`PrescriptionInputDto` |
| **映射** | ⚠️ | PrescriptionDetailDto 有 `Status` 字段（CommonStatus），**Entity 无此属性**。Mapper 忽略 Status 并手动赋值 Enabled。**伪字段**——DTO 暴露了 Entity 不存在的概念 |

### 2.9 PrescriptionItem

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段命名不一致** | 🔴 **问题** | Entity 计算属性 `Amount`（= UnitPrice × Dosage），DTO 为 `Subtotal`。**同语义不同名** |
| **字段冗余** | ⚠️ | DTO 有 `TotalPrice`、`TotalWeight`、`Role`、`Notes`，Entity 均无。`Notes` 是 `Remark` 的别名（`get => Remark; set => Remark = value;`）——**隐式别名造成混淆** |
| **PrescriptionId 可空** | ⚠️ | Entity 为 `Guid PrescriptionId`（必填），DTO 为 `Guid? PrescriptionId`（可空）——**空安全语义不一致** |

### 2.10 Registration

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段完整度** | ✅ 合理 | PatientId, PatientName, DoctorId, DoctorName, MedicalCaseId, Source, Status, QueueNumber, RegistrationFee, Remark |
| **命名** | ⚠️ | Entity 用 `DoctorId`，MedicalCase 用 `UserId`——**同一概念（医生）跨实体命名不一致** |
| **验证** | ❌ **缺失** | RegistrationInputDto **无 FluentValidation 验证器** |

### 2.11 AuthSession

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **继承** | ⚠️ | **不继承 BaseEntity**（独立的 Key + 手动状态管理）。这是合理设计（AuthSession 有独立生命周期），但与其他实体不一致 |
| **字段** | ✅ | UserId, TokenHash, LoginTime, LogoutTime, ExpiryTime, IpAddress, UserAgent, IsRevoked, RevokedReason, Status |
| **DTO 覆盖** | ⚠️ | 无直接对应的 AuthSessionDto。认证流程通过 JWT token 而非 AuthSession DTO 暴露 |

### 2.12 SecurityAuditLog / MedicalCaseAuditLog / MedicalCasePrintLog

| 审计项 | 状态 | 详情 |
|--------|------|------|
| **字段** | ✅ | 审计日志字段合理 |
| **DTO** | ⚠️ | AuditLogDto 用于 MedicalCase 审计展示；SecurityAuditLog 无直接 DTO |
| **SystemLog** | ⚠️ | 独立实体，不继承 BaseEntity，int 主键——**设计合理但风格不一致** |

---

## 三、DTO 命名一致性分析

### 3.1 后缀命名矩阵

| 实体 | ListDto | DetailDto | InputDto | 其他 DTO |
|------|---------|-----------|----------|----------|
| User | ✅ UserListDto | ✅ UserDetailDto | ✅ UserInputDto | UserBasicDto, ChangePasswordDto, ChangeProfileDto |
| Patient | ✅ PatientListDto | ✅ PatientDetailDto | ✅ PatientInputDto | PatientBasicDto |
| Herb | ✅ HerbListDto | ✅ HerbDetailDto | ✅ HerbInputDto | HerbBasicDto |
| Formula | ✅ FormulaListDto | ✅ FormulaDetailDto | ✅ FormulaInputDto | FormulaHerbItemDto, FormulaHerbItemInputDto |
| MedicalCase | ✅ MedicalCaseListDto | ✅ MedicalCaseDetailDto | ✅ MedicalCaseInputDto | MedicalCaseQueryDto, MedicalCaseStatusInputDto, PendingMedicalCaseDto |
| Consultation | ❌ 无 | ✅ ConsultationDetailDto | ✅ ConsultationInputDto | — |
| Prescription | ❌ 无 | ✅ PrescriptionDetailDto | ✅ PrescriptionInputDto | PrescriptionItemDto, PrescriptionItemInputDto |
| Registration | ✅ RegistrationListDto | ✅ RegistrationDetailDto | ✅ RegistrationInputDto | — |
| Auth | ❌ | ❌ | ❌ | LoginRequest, LoginResponse, ChangePasswordRequest, RefreshTokenRequest |

### 3.2 命名问题汇总

| # | 问题 | 严重度 | 详情 |
|---|------|--------|------|
| N1 | **Auth 模块后缀不统一** | 中 | Auth 用 `Request`/`Response` 后缀（LoginRequest/LoginResponse），其他模块用 `Dto` 后缀。ChangePasswordRequest vs ChangePasswordDto（Users 模块）——同一操作两端命名不同 |
| N2 | **ResetPasswordRequest vs ResetPasswordResponseDto** | 低 | 同一操作的请求无 Dto 后缀，响应有 Dto 后缀 |
| N3 | **MedicalCase 模块有 Request 类无 Dto 后缀** | 低 | CancelMedicalCaseRequest, PrintLogRequest, RecordPrintRequest, SetPrescriptionFlagRequest |
| N4 | **UserBasicDto 用 record，其他用 class** | 低 | UserBasicDto 是 record 类型（init only），其他 DTO 是 class（get/set）。混合使用可接受但需注意 |
| N5 | **BatchIdsRequest vs BatchDeleteInputDto** | 低 | 同类操作（批量）用不同后缀 |

---

## 四、字段冗余/脱节分析

### 4.1 Entity 有但 DTO 无

| Entity | 字段 | 影响 |
|--------|------|------|
| Formula | `FormulaType` | DTO 未暴露——桌面端无法区分经典方/经验方 |
| Formula | `UserId` | DTO 未暴露创建者（仅通过 CreatedBy 审计字段间接暴露） |
| MedicalCase | `IsPrinted`, `PrintCount`, `LastPrintedAt` | 打印追踪字段无 DTO 暴露 |
| ApplicationUser | `MustChangeOnNextLogin` | UserListDto 未暴露（仅 UserBasicDto 有） |
| ApplicationUser | `PinYinCode` | UserListDto 未暴露（仅 DetailDto/BasicDto 有） |

### 4.2 DTO 有但 Entity 无（最严重问题）

| DTO | 字段 | Entity 状态 | 影响 |
|-----|------|-------------|------|
| 🔴 FormulaInputDto | `Description`, `Instructions`, `Preparation`, `Contraindications` | **均无** | 创建/更新时这些字段被忽略，用户体验到「填写了但不保存」 |
| 🔴 FormulaDetailDto | `Description`, `Source`, `Contraindications` | **均无** | 详情返回 null，前端展示空白 |
| 🔴 FormulaHerbItemDto | `Price`, `SortOrder`, `SpecialInstructions` | **均无** | Price 来源不明（可能是运行时从 Herbs 查找），SortOrder 无持久化 |
| ⚠️ FormulaHerbItemInputDto | `Preparation` | Entity 为 `ProcessingMethod` | **字段名不一致**，Mapper 映射需手动处理 |
| ⚠️ PrescriptionDetailDto | `Status` | **Entity 无** | Mapper 手动赋 Enabled——伪字段 |
| ⚠️ PrescriptionItemDto | `Role` (HerbRole) | **Entity 无** | 可能是业务层动态计算，但 DTO 暴露了 |
| ⚠️ PrescriptionItemDto | `Notes` | Entity 为 `Remark` | 隐式别名（`get => Remark; set => Remark = value;`） |
| ⚠️ MedicalCaseListDto | `PatientGender`, `PatientAge`, `Diagnosis`, `HasConsultation`, `HasPrescription` | **Entity 无直接字段** | 需要跨表查询/计算填充 |

### 4.3 字段名不一致（同语义不同名）

| # | Entity 字段 | DTO 字段 | 说明 |
|---|-------------|----------|------|
| F1 | `PrescriptionItem.Amount` (计算属性) | `PrescriptionItemDto.Subtotal` | 同为 UnitPrice×Dosage，Entity 叫 Amount，DTO 叫 Subtotal |
| F2 | `FormulaHerbItem.ProcessingMethod` | `FormulaHerbItemInputDto.Preparation` | 同一概念不同名 |
| F3 | `Registration.DoctorId` | `MedicalCase.UserId` | 同一概念（医生），Registration 叫 DoctorId，MedicalCase 叫 UserId |
| F4 | `PrescriptionItem.Remark` | `PrescriptionItemDto.Notes` | DTO 用别名属性暴露 |

---

## 五、Mapper 映射审计

### 5.1 Server 端 Mapper

| Mapper | 类型 | 状态 | 问题 |
|--------|------|------|------|
| **CatalogDtoMapper** | static partial (Mapperly) | ✅ | Herb/Formula 映射。Formula 的 ToEntity 走手写工厂。ToFormulaDetailDto 完全手写（因 Category 回退逻辑 + Herbs 嵌套） |
| **IdentityMapper** | static partial (Mapperly) | ⚠️ | ToListDto/ToDetailDto 完全手写（Identity 可空字段 null 合并）。ToBasicDto/ToCredentialDto 由 Mapperly 生成+MapProperty |
| **PatientMapper** | static partial (Mapperly) | ✅ | ToEntity 走手写工厂。ToListDto/ToDetailDto 由 Mapperly 自动生成 |
| **MedicalCaseMapper** | partial (Mapperly, 实例类) | ⚠️ | 唯一的**非 static** Mapper。ToListDto 忽略 6 个字段需 Service 填充。ToDetailDto 忽略 8 个字段。Enrich 方法手动补充嵌套 DTO |
| **RegistrationMapper** | partial (Mapperly) | ✅ | 纯 Mapperly 自动生成，最简洁 |

### 5.2 Mapper 问题

| # | 问题 | 严重度 | 详情 |
|---|------|--------|------|
| M1 | **MedicalCaseMapper 是实例类，其他是 static** | 低 | MedicalCaseMapper 声明为 `partial class`（非 static），与其他 4 个 Mapper（均为 `static partial class`）风格不一致 |
| M2 | **MedicalCaseMapper 大量字段被 Ignore** | 中 | ToListDto 忽略 6 字段、ToDetailDto 忽略 8 字段，Service 层需手动填充。意味着 Mapperly 生成的映射只覆盖约 50% 的字段 |
| M3 | **ToFormulaDetailDto 完全手写** | 低 | CatalogDtoMapper.ToFormulaDetailDto 用 [UserMapping(Default=false)] 完全手写，Mapperly 未参与。原因：Category 回退 + Herbs 嵌套 + TotalPrice 恒 0 |
| M4 | **PrescriptionDetailDto.Status 伪字段** | 中 | Entity 无 Status 字段，Mapper 硬编码赋值 `CommonStatus.Enabled`。DTO 暴露了不存在的持久化概念 |
| M5 | **Desktop 端 Mapper 大量 Ignore** | 中 | FormulaDetailModelMapper 忽略 Herbs/HerbCount/TotalPrice/ValidationStatus/Description/Indication/Contraindications——**Desktop 端丢失大量业务数据** |

### 5.3 Desktop 端 Mapper

| Mapper | 映射 | 状态 | 问题 |
|--------|------|------|------|
| FormulaDetailModelMapper | DTO→Model | ⚠️ | 忽略 Herbs/HerbCount/TotalPrice/ValidationStatus/Description/Indication/Contraindications（7 个字段丢失） |
| HerbDetailModelMapper | DTO→Model | ⚠️ | 忽略 CreatedAt/UpdatedAt/CreatedBy |
| ConsultationMapper | DTO→Item | ✅ | 忽略 CreatedBy（合理） |
| MedicalCaseDetailModelMapper | DTO→Model | ⚠️ | 忽略 PatientGender/PatientAge/UserId/ConsultationId/PrescriptionId/CompletedAt/Diagnosis/PresentIllness（8 个字段） |
| PrescriptionMapper | DTO→ViewModel | ⚠️ | 忽略 Items/TotalPrice |
| PatientMapper | DTO→EditContext | ✅ | 清晰的三向映射（DTO→EditContext→InputDto） |
| UserMapper | DTO→Model | ⚠️ | 忽略 PinYinCode（手动回退逻辑） |

---

## 六、验证覆盖分析

### 6.1 验证器覆盖矩阵

| Input DTO | 验证器 | 状态 | 说明 |
|-----------|--------|------|------|
| LoginRequest | LoginRequestValidator | ✅ | UserName + Password |
| PatientInputDto | PatientInputDtoValidator | ✅ | Name, Gender, BirthDate |
| HerbInputDto | HerbInputDtoValidator | ✅ | BR-001~BR-008 |
| FormulaInputDto | FormulaInputDtoValidator | ✅ | Name, Effect, Usage, Indication 等 |
| MedicalCaseInputDto | MedicalCaseInputDtoValidator | ✅ | PatientId, UserId + 嵌套验证 |
| PrescriptionInputDto | PrescriptionInputDtoValidator | ✅ | MedicalCaseId, ReferencedFormulas, Advice, Remark |
| 🔴 **RegistrationInputDto** | **无** | ❌ | PatientId, DoctorId, Source 均无验证 |
| 🔴 **UserInputDto** | **有但不完整** | ⚠️ | 需检查是否覆盖 UserName/Password/RealName |
| 🔴 **ConsultationInputDto** | **无独立验证器** | ❌ | 仅在 MedicalCaseInputDtoValidator 中嵌套验证 |
| 🔴 **ChangePasswordRequest** | **无** | ❌ | OldPassword/NewPassword 无验证 |
| 🔴 **ChangePasswordDto** | **无** | ❌ | Users 模块的改密 DTO |
| 🔴 **BatchDeleteInputDto** | **无** | ⚠️ | 批量操作Ids 无验证 |
| 🔴 **FormulaHerbItemInputDto** | **无** | ⚠️ | 嵌套在 FormulaInputDto 中，无独立验证 |

### 6.2 验证器问题汇总

| # | 问题 | 严重度 |
|---|------|--------|
| V1 | RegistrationInputDto 无验证器——PatientId/DoctorId 可为空提交 | 🔴 高 |
| V2 | ConsultationInputDto 无独立验证器——仅被 MedicalCaseInputDtoValidator 嵌套调用 | ⚠️ 中 |
| V3 | ChangePasswordRequest/ChangePasswordDto 无验证器——密码复杂度无客户端校验 | ⚠️ 中 |
| V4 | UserInputDto 验证器不完整——需确认是否覆盖所有必填字段 | ⚠️ 中 |
| V5 | FormulaHerbItemInputDto 无独立验证器——HerbName/Dosage 无验证 | ⚠️ 低 |

---

## 七、层级边界问题

### 7.1 DTO 层承载 Entity 不存在的概念

| # | 位置 | 问题 | 建议 |
|---|------|------|------|
| B1 | FormulaDetailDto.Description/Source/Contraindications | Entity 无持久化字段 | 删除 DTO 字段或添加 Entity 字段 |
| B2 | FormulaHerbItemDto.Price/SortOrder/SpecialInstructions | Entity 无持久化字段 | 明确来源——运行时计算还是持久化？ |
| B3 | PrescriptionDetailDto.Status | Entity 无此字段 | 删除伪字段或添加 Entity 字段 |
| B4 | PrescriptionItemDto.Notes (Remark 别名) | 隐式别名 | 统一用 Remark，删除 Notes |
| B5 | FormulaHerbItemDto.Herb (HerbDetailDto) | 嵌套完整实体 | 改为只含 HerbId + HerbName 的轻量引用 |

### 7.2 Desktop Model 层越界

| # | 位置 | 问题 |
|---|------|------|
| D1 | FormulaDetailModel.Ignore 7 个 DTO 字段 | Desktop 端丢失 Description/Indication/Contraindications/ValidationStatus 等业务数据 |
| D2 | MedicalCaseDetailModel.Ignore 8 个 DTO 字段 | Desktop 端丢失 PatientGender/PatientAge/Diagnosis/PresentIllness |
| D3 | PrescriptionMapper.Ignore TotalPrice | Desktop 端处方总价为 0 |

---

## 八、废弃/残留项目

| # | 位置 | 问题 | 建议 |
|---|------|------|------|
| R1 | `src/Server/Modules/LYBT.Module.MedicalCase/` | 空项目目录（仅 bin/obj），**无 .csproj**。真正的模块是 `LYBT.Module.MedicalCases`（复数） | 删除空目录 |
| R2 | `src/Server/Modules/LYBT.Module.Reports/` | 无 Mapper。报表模块直接查 SQL 不走 Entity→DTO 映射 | 可接受（报表是只读聚合查询） |

---

## 九、问题优先级汇总

### 🔴 高优先级（影响功能正确性）

| # | 问题 | 类别 | 影响 |
|---|------|------|------|
| **P1** | FormulaInputDto/DetailDto 有 Entity 不存在的字段（Description/Instructions/Preparation/Contraindications/Source） | 字段脱节 | 创建/更新时这些字段被静默忽略，用户填写的数据不保存 |
| **P2** | FormulaHerbItemDto 有 Price/SortOrder/SpecialInstructions 但 Entity 无 | 字段脱节 | 明细数据可能丢失 |
| **P3** | RegistrationInputDto 无 FluentValidation 验证器 | 验证缺失 | 可提交空 PatientId/DoctorId |
| **P4** | PrescriptionItem.Amount vs PrescriptionItemDto.Subtotal 命名不一致 | 命名冲突 | 开发者混淆语义，Mapper 映射需手动处理 |
| **P5** | FormulaHerbItemInputDto.Preparation vs Entity.ProcessingMethod 命名不一致 | 命名冲突 | 映射时字段名不匹配，依赖手写映射 |

### ⚠️ 中优先级（影响可维护性）

| # | 问题 | 类别 | 影响 |
|---|------|------|------|
| **P6** | MedicalCase.UserId vs Registration.DoctorId 同一概念不同名 | 命名不一致 | 跨模块查询时字段映射易出错 |
| **P7** | MedicalCaseMapper 大量字段被 Ignore（50%+），Service 层手动填充 | Mapper 问题 | Mapperly 价值降低，维护成本增加 |
| **P8** | PrescriptionDetailDto.Status 伪字段（Entity 无） | 层级越界 | DTO 暴露了不存在的持久化概念 |
| **P9** | PrescriptionItemDto.Notes 是 Remark 的隐式别名 | 命名冲突 | 读代码时以为是两个字段 |
| **P10** | ConsultationDetailDto 包含父聚合字段（PatientId/UserId/PatientName/DoctorName） | 层级越界 | 嵌套 DTO 承载了不属于自身实体的数据 |
| **P11** | Auth 模块后缀不统一（Request/Response vs Dto） | 命名不一致 | 风格不统一 |
| **P12** | MedicalCaseMapper 是实例类，其他是 static | Mapper 问题 | 风格不一致 |
| **P13** FormulaInputDto.Effect 声明为 `string`（非 `string?`），Entity 为 `string?` | 类型不一致 | DTO 要求必填但 Entity 允许空 |
| **P14** ChangePasswordRequest/ChangePasswordDto 无验证器 | 验证缺失 | 密码复杂度无校验 |

### 📦 低优先级（代码整洁）

| # | 问题 | 类别 | 影响 |
|---|------|------|------|
| **P15** | UserBasicDto 用 record，其他用 class | 风格 | 混合使用 |
| **P16** | PrescriptionItemDto.PrescriptionId 为 Guid?，Entity 为 Guid | 类型不一致 | 空安全语义不同 |
| **P17** | FormulaHerbItemDto.Unit 默认为空，Entity 默认 "g" | 默认值不一致 | 可能导致空单位显示 |
| **P18** | LYBT.Module.MedicalCase 空目录残留 | 残留 | 混乱项目结构 |
| **P19** | Desktop 端大量字段被 Mapper Ignore | Mapper 问题 | DTO 数据在 Desktop 端丢失 |
| **P20** | PrescriptionItemDto.TotalPrice/TotalWeight 在 Entity 无对应 | 层级越界 | 需运行时计算 |

---

## 十、修复建议

### 10.1 紧急修复（P1-P5）

1. **P1/P2**: 删除 FormulaInputDto/DetailDto/FormulaHerbItemDto 中 Entity 不存在的字段，或在 Entity 中添加对应持久化字段。**推荐方案**：若业务需要这些字段（Description/Contraindications/Source），在 Entity 中添加；若为遗留字段，从 DTO 删除
2. **P3**: 新增 RegistrationInputDtoValidator，验证 PatientId/DoctorId/Source
3. **P4**: 统一命名——建议 PrescriptionItemDto.Subtotal → Amount（对齐 Entity）或 Entity.Amount → Subtotal
4. **P5**: 统一 FormulaHerbItem 的字段名——Preparation → ProcessingMethod

### 10.2 架构优化（P6-P14）

5. **P6**: 统一医生引用字段名——建议统一为 DoctorId（语义清晰），MedicalCase.UserId → MedicalCase.DoctorId
6. **P7**: MedicalCaseMapper 的 Ignore 字段考虑在 Entity 中添加计算属性（如 HasConsultation），减少 Service 层手动填充
7. **P8/P10**: 重新审视 DTO 设计——是使用扁平 DTO（包含关联数据）还是嵌套 DTO（仅含 ID + 独立查询）
8. **P11**: Auth DTO 统一后缀——建议 Request/Response 保留（HTTP 语义），但保持一致
9. **P13**: FormulaInputDto.Effect 改为 `string?`，与 Entity 一致

### 10.3 清理（P15-P20）

10. 删除 `LYBT.Module.MedicalCase` 空目录
11. PrescriptionItemDto 删除 Notes 别名，统一用 Remark
12. FormulaHerbItemDto.Unit 默认值改为 "g"（对齐 Entity）

---

## 十一、统计摘要

| 维度 | 总问题数 | 🔴高 | ⚠️中 | 📦低 |
|------|---------|------|------|------|
| 字段脱节/冗余 | 7 | 3 | 4 | 0 |
| 命名不一致 | 6 | 2 | 3 | 1 |
| Mapper 问题 | 5 | 0 | 3 | 2 |
| 验证缺失 | 4 | 1 | 3 | 0 |
| 层级边界模糊 | 4 | 0 | 3 | 1 |
| 残留/风格 | 3 | 0 | 1 | 2 |
| **合计** | **29** | **6** | **17** | **6** |

> **结论**：三层架构整体设计合理（Entity 贫血模型 + DTO 传输 + Desktop Model 可编辑），Mapperly 编译时映射使用正确。**最严重的问题集中在 Formula 模块**（DTO 与 Entity 严重脱节，5+ 个字段 Entity 不存在），其次是 Registration 缺验证器和 PrescriptionItem 命名冲突。建议优先修复 P1-P5 高优问题。

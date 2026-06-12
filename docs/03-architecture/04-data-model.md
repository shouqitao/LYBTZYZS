# 数据模型

## 概述

系统使用 EF Core 8.0 管理数据模型，所有业务实体继承 `BaseEntity` 基类。MedicalCase 是唯一的 DDD 聚合根，Consultation 和 Prescription 是其内部实体。数据库使用 SQL Server (远程) 或 SQL Server (嵌入式 LocalWebAPI)。

## 实体关系图

```mermaid
erDiagram
    MedicalCase ||--o| Consultation : "1:0..1"
    MedicalCase ||--o| Prescription : "1:0..1"
    MedicalCase }o--|| Patient : "N:1"
    MedicalCase }o--|| User : "N:1 (医生)"
    Prescription ||--o{ PrescriptionItem : "1:N"
    MedicalCase ||--o{ MedicalCasePrintLog : "1:N"
    Formula ||--o{ FormulaHerbItem : "1:N"
    FormulaHerbItem }o--o| Herb : "N:0..1 (延迟绑定)"
    PrescriptionItem }o--|| Herb : "N:1"
    User ||--o{ AuthSession : "1:N"
    User ||--o{ RefreshToken : "1:N"
    Registration }o--|| Patient : "N:1"
    Registration }o--|| User : "N:1 (指派医生)"
    Registration ||--o| MedicalCase : "1:0..1"
```

## 聚合根边界

```mermaid
graph TB
    subgraph AggregateRoot["MedicalCase 聚合根"]
        MC["MedicalCase<br>(聚合根)"]
        C["Consultation<br>(内部实体, 1:1)"]
        P["Prescription<br>(内部实体, 1:0..1)"]
        PI["PrescriptionItem<br>(内部实体, 1:N)"]
        MCPL["MedicalCasePrintLog<br>(内部实体, 1:N)"]
        MC --- C
        MC --- P
        MC --- MCPL
        P --- PI
    end

    subgraph Independent["独立实体"]
        Patient
        User
        Herb
        Formula
        Registration
    end

    MC -.->|PatientId| Patient
    MC -.->|UserId| User
    PI -.->|HerbId| Herb
    Registration -.->|PatientId| Patient
    Registration -.->|DoctorId| User
    Registration -.->|MedicalCaseId| MC
```

**规则**: 聚合根内的实体 (Consultation, Prescription) 只能通过 MedicalCase 访问和操作，禁止独立的 Repository。

## BaseEntity 基类

所有业务实体继承此基类:

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| CreatedAt | DateTime | 创建时间 (UTC) |
| UpdatedAt | DateTime? | 更新时间 |
| CreatedBy | Guid? | 创建人 |
| UpdatedBy | Guid? | 更新人 |
| RowVersion | byte[]? | 并发控制 (乐观锁) |
| IsDeleted | bool | 软删除标记 |

## 实体定义

### MedicalCase (医案 -- 聚合根)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| PatientId | Guid | 是 | 关联患者 |
| PatientName | string(50) | 是 | 患者姓名 (冗余) |
| UserId | Guid | 是 | 医生 ID |
| DoctorName | string(50) | 是 | 医生姓名 (冗余) |
| CaseNumber | string(50) | 否 | 医案编号 |
| CaseStatus | MedicalCaseStatus | 是 | 状态 (默认 Active) |
| NeedsPrescription | bool? | 否 | 是否需要处方 |
| CompletedAt | DateTime? | 否 | 完成时间 |
| Remark | string(500) | 否 | 备注 |
| PrintVersion | int | 是 | 打印版本号 (默认 1)。医案内容修改后自增，标记需重新打印 |
| LastPrintedAt | DateTime? | 否 | 最后打印时间 |
| PrintCount | int | 是 | 打印次数 (默认 0) |
| IsPrinted | bool | 是 | 是否已打印 (默认 false)。聚合根级打印保护: 为 true 时修改 Consultation 或 Prescription 内容需提供 EditReason (MC-D15) |
| Consultation | Consultation? | - | 导航属性 (1:1) |
| Prescription | Prescription? | - | 导航属性 (1:0..1) |

**计算属性**: IsLocked (跨日锁定), IsActive, IsCompleted

**DDD 域方法** (聚合根行为):

| 方法 | 说明 |
|------|------|
| `Complete()` | 设置 CaseStatus=Completed + CompletedAt=DateTime.Now |
| `Suspend()` | 设置 CaseStatus=Suspended (MC-D20) |
| `SoftDelete()` | 设置 IsDeleted=true (取消医案) |
| `UpdateConsultation(request)` | 从 ConsultationInputDto 更新 Consultation 子实体字段 |

> MedicalCase 是系统中唯一采用充血模型的实体，状态变更逻辑封装在聚合根域方法中，Service 层委托调用。

### Consultation (诊断)

共享 MedicalCase 主键 (1:1 关系):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| PresentIllness | string(2000) | 否 | 现病史 |
| TongueDiagnosis | string(500) | 否 | 舌诊 |
| PulseDiagnosis | string(500) | 否 | 脉诊 |
| TcmDiagnosis | string(500) | 否 | 中医辨证 |

### Prescription (处方)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| MedicalCaseId | Guid | 是 | 外键 |
| PrescriptionNumber | string(20) | 否 | 处方编号 |
| DosageCount | int | 是 | 剂数 (默认 7) |
| Discount | decimal(5,4) | 是 | 折扣 (默认 1.0) |
| Usage | string(500) | 否 | 用法 |
| Advice | string(500) | 否 | 医嘱 |
| ReferencedFormulas | string(500) | 否 | 引用验方 (逗号分隔) |
| Remark | string(500) | 否 | 备注 |

**价格计算公式** (MC-D14):
- Items[i].Amount = UnitPrice x Dosage (单味药小计，PrescriptionItem 计算属性)
- SingleDosePrice = SUM(Items.Amount) (一剂所有药材小计之和，Prescription 计算属性)
- TotalPrice = SingleDosePrice x DosageCount x Discount (最终总价)
- Discount 语义: 1.0=无折扣, 0.9=九折, 0.85=八五折

### PrescriptionItem (处方药材项)

不继承 BaseEntity:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Id | Guid | 是 | 主键 |
| PrescriptionId | Guid | 是 | 外键 |
| HerbId | Guid | 是 | 药材 ID |
| HerbName | string(100) | 是 | 药材名称 (冗余) |
| Dosage | int | 是 | 用量 |
| Unit | string(16) | 是 | 单位 (默认 "g") |
| DecocteMethod | DecocteMethod | 是 | 煎煮方法 |
| UnitPrice | decimal(18,2) | 是 | 单价 |
| Usage | string(200) | 否 | 用法 |
| Remark | string(200) | 否 | 备注 |

### Patient (患者)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Name | string(100) | 是 | 姓名 |
| PinYinCode | string(50) | 否 | 拼音码 (搜索用) |
| Gender | Gender | 是 | 性别 |
| MaritalStatus | int | 是 | 婚姻状况 |
| BirthDate | DateTime? | 否 | 出生日期 |
| IdType | int | 是 | 证件类型 (默认 0) |
| IdNumber | string(50) | 否 | 身份证号 (敏感) |
| PhoneNumber | string(20) | 否 | 手机号 (敏感) |
| Address | string(256) | 否 | 地址 (敏感) |
| AllergyHistory | string(500) | 否 | 过敏史 (敏感) |
| MedicalHistory | string(1000) | 否 | 病史 (敏感) |
| BloodType | int | 是 | 血型 |
| EmergencyContactName | string? | 否 | 紧急联系人姓名 |
| EmergencyContactPhone | string? | 否 | 紧急联系人电话 (敏感) |
| EmergencyContactRelation | string? | 否 | 紧急联系人关系 |
| Status | CommonStatus | 是 | 状态 (PAT-D05: 禁用主要场景为患者已故; 禁用后禁止创建新医案) |
| DisableReason | string(128) | 否 | 禁用原因 |
| LastVisitTime | DateTime? | 否 | 最后就诊 |
| VisitCount | int | 是 | 就诊次数 |

**计算属性**: Age (从 BirthDate 计算，NotMapped)

### User (用户)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| UserName | string(50) | 是 | 用户名 |
| RealName | string(50) | 是 | 真实姓名 |
| PinYinCode | string(50) | 否 | 拼音码 |
| PhoneNumber | string(20) | 否 | 手机号 |
| Email | string(100) | 否 | 邮箱 |
| Role | UserRole | 是 | 角色 (默认 Doctor) |
| Status | CommonStatus | 是 | 状态 |
| PasswordHash | string(256) | 是 | BCrypt 密码哈希 |
| FailedLoginCount | int | 是 | 登录失败次数 |
| LockoutEnd | DateTime? | 否 | 锁定截止时间 |
| LastLoginTime | DateTime? | 否 | 最后登录时间 |
| Remark | string(500) | 否 | 备注 |

### Herb (药材)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Name | string(100) | 是 | 药材名称 |
| PinYinCode | string(50) | 否 | 拼音码 |
| Category | string(50) | 否 | 分类 |
| Origin | string(100) | 否 | 产地 |
| Spec | string(100) | 否 | 规格 |
| Unit | string(10) | 是 | 单位 (默认 "克") |
| Price | decimal(18,2) | 是 | 售价 |
| CostPrice | decimal(18,2)? | 否 | 成本价 |
| Effect | string(500) | 否 | 功效 |
| Usage | string(500) | 否 | 用法 |
| Remark | string(500) | 否 | 备注 |
| Status | CommonStatus | 是 | 状态 |

**显示规则** (MC-D07): 禁用药材 (Status=Disabled) 在历史处方中展示时，名称后缀"(已停用)"，如"黄芪(已停用)"。禁用药材仅可查看不可修改剂量，不可添加到新处方中。

### Formula (验方)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Name | string(200) | 是 | 验方名称 |
| Effect | string(500) | 否 | 功效 |
| Indication | string(1000) | 否 | 适应症 |
| Usage | string(500) | 否 | 用法 |
| Property | string(300) | 否 | 性味归经 |
| Status | CommonStatus | 是 | 状态 |
| IsShared | bool | 是 | 是否共享 |
| ValidationStatus | FormulaValidationStatus | 是 | 验证状态 |
| Category | string(50) | 否 | 分类 |
| FormulaType | FormulaType | 是 | 类型 (经典方/经验方) |
| UserId | Guid? | 否 | 创建医生 |

### Registration (挂号记录)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| PatientId | Guid | 是 | 关联患者 (FK) |
| DoctorId | Guid | 是 | 指派医生 (FK -> User) |
| MedicalCaseId | Guid? | 否 | 关联医案 (FK, 接诊后填入) |
| Source | RegistrationSource | 是 | 创建来源: Receptionist / Doctor |
| Status | RegistrationStatus | 是 | 状态: Waiting / InProgress / Completed / Cancelled |

> Registration 继承 BaseEntity (含 Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion, IsDeleted)。与 MedicalCase 为 1:0..1 关系: Waiting 状态时无医案，接诊后填入 MedicalCaseId。

**状态机**:
- `Waiting -> InProgress`: 医生从队列选中 (自动创建 MedicalCase)
- `Waiting -> Cancelled`: 前台手动取消 (REG-BR-001 校验)
- `InProgress -> Completed`: 医案 Completed 时自动跟随
- `InProgress -> Waiting`: 医案 Cancelled 且 Source=Receptionist (回退)
- `InProgress -> Cancelled`: 医案 Cancelled 且 Source=Doctor (自动)

### FormulaHerbItem (验方药材项)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Id | Guid | 是 | 主键 |
| FormulaId | Guid | 是 | 外键 |
| HerbId | Guid? | 否 | 药材 ID (延迟绑定) |
| OriginalHerbName | string(100) | 否 | 原始药材名 |
| IsValidated | bool | 是 | 是否已验证 |
| HerbName | string(100) | 是 | 药材名称 |
| Dosage | int | 是 | 用量 |
| Unit | string(16) | 是 | 单位 |
| DecocteMethod | DecocteMethod | 是 | 煎煮方法 |
| ProcessingMethod | string(100) | 否 | 炮制方法 |

### AuthSession (认证会话)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Id | Guid | 是 | 主键 |
| UserId | Guid | 是 | 用户 ID |
| TokenHash | string(256) | 是 | Token 哈希 |
| LoginTime | DateTime | 是 | 登录时间 |
| LogoutTime | DateTime? | 否 | 登出时间 |
| ExpiryTime | DateTime | 是 | 过期时间 |
| IpAddress | string(45) | 是 | IP 地址 |
| IsRevoked | bool | 是 | 是否撤销 |

### RefreshToken (刷新令牌)

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Token | string(512) | 是 | 令牌值 |
| UserId | Guid | 是 | 用户 ID |
| UserType | string(50) | 是 | 用户类型 |
| Jti | string(128) | 是 | JWT ID |
| ExpiresAt | DateTime | 是 | 过期时间 |
| IsRevoked | bool | 是 | 是否撤销 |
| FamilyId | string(128) | 否 | Token 族 (重放检测) |
| IsUsed | bool | 是 | 是否已使用 |
| UsageCount | int | 是 | 使用次数 |

## 辅助实体

以下实体不是独立业务概念，而是为主实体提供支撑功能 (关联关系、打印追踪、会话管理等)。

### MedicalCasePrintLog (打印记录)

MedicalCase 聚合根的内部实体 (继承 BaseEntity)，记录每次打印操作用于合规追溯。

| 字段 | 类型 | 说明 |
|------|------|------|
| MedicalCaseId | Guid | 外键 (关联 MedicalCase) |
| PrintType | PrintType | 打印类型 (处方/验方) |
| PrintVersion | int | 打印时的医案版本号 |
| PrintedAt | DateTime | 打印时间 |
| PrintedBy | Guid? | 打印人 ID |
| PrintedByName | string(50)? | 打印人姓名 |
| PrinterName | string(100)? | 打印机名称或 IP |
| IsSuccess | bool | 打印状态 (默认 true) |
| ErrorMessage | string(500)? | 失败错误信息 |
| Remark | string(200)? | 备注 |

**用途**: 已打印医案修改后需提供 EditReason (MC-D15)，打印日志提供变更审计链。

### PrescriptionItem (处方药材项)

Prescription 的子实体，表示处方中的单味药材。不继承 BaseEntity (无软删除、无审计字段)，随处方整体操作。

**关键计算**: `Amount = UnitPrice x Dosage` (计算属性，不持久化)

> 详细字段定义见上方 [PrescriptionItem 章节](#prescriptionitem-处方药材项)。

### FormulaHerbItem (验方药材项)

Formula 的子实体，实现验方与药材的 N:N 关系。支持**延迟绑定**: 导入验方时 `HerbId` 可为 null，通过 `OriginalHerbName` 保留原始名称，后续校验时绑定到系统药材 (`IsValidated=true`)。

> 详细字段定义见上方 [FormulaHerbItem 章节](#formulaherbitem-验方药材项)。

### AuthSession + RefreshToken (会话与令牌)

User 的关联实体，管理用户登录会话和 JWT 刷新令牌:

- **AuthSession**: 记录登录/登出时间、Token 哈希、IP 地址，支持会话撤销 (`IsRevoked`)
- **RefreshToken**: 实现 Token 轮换机制，通过 `FamilyId` 检测重放攻击，`IsUsed` + `UsageCount` 防止 Token 重复使用

> 详细字段定义见上方 [AuthSession](#authsession-认证会话) 和 [RefreshToken](#refreshtoken-刷新令牌) 章节。

## 枚举定义

### MedicalCaseStatus

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Suspended | 已挂起 (医生暂时离开，稍后继续) |
| 1 | Active | 进行中 |
| 2 | Completed | 已完成 |

> **注意**: `Draft` (原值=0) 已重命名为 `Suspended` (MC-D20)。`Cancelled` (原值=3) 已移除，取消操作统一通过 `IsDeleted=true` 软删除实现。

### UserRole

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Receptionist | 前台接待 |
| 1 | Doctor | 医生 |
| 10 | Admin | 管理员 |
| 100 | SuperAdmin | 超级管理员 |

### Gender

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Unknown | 未知 |
| 1 | Male | 男 |
| 2 | Female | 女 |

### CommonStatus

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Disabled | 禁用 |
| 1 | Enabled | 启用 |

### DecocteMethod (煎煮方法)

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Default | 默认 |
| 1 | PreDecoct | 先煎 |
| 2 | PostAdd | 后下 |
| 3 | MeltIn | 烊化 |
| 4 | TakeWithWater | 冲服 |
| 5 | WrapDecoct | 包煎 |
| 6 | SeparateDecoct | 另煎 |

### FormulaValidationStatus

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Draft | 草稿/未验证 |
| 1 | Validated | 已验证 |

### FormulaType

| 值 | 名称 | 说明 |
|----|------|------|
| 1 | Classic | 经典方 |
| 2 | Experience | 经验方 |

### RegistrationSource

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Receptionist | 前台挂号 |
| 1 | Doctor | 医生直接看诊 |

### RegistrationStatus

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Waiting | 等待接诊 |
| 1 | InProgress | 接诊中 |
| 2 | Completed | 已完成 |
| 3 | Cancelled | 已取消 |

## 数据库约定

### 命名规范

| 对象 | 规范 | 示例 |
|------|------|------|
| 表名 | PascalCase 复数 | MedicalCases |
| 列名 | PascalCase | PatientId |
| 外键 | {RelatedEntity}Id | PatientId |
| 索引 | IX_{Table}_{Column} | IX_MedicalCases_PatientId |

### EF Core 配置

- Fluent API 优先于 Data Annotations
- 全局查询过滤器: `entity.HasQueryFilter(e => !e.IsDeleted)`
- DateTime 统一 UTC
- decimal 使用 `HasPrecision(18, 2)` 或 `HasPrecision(5, 4)`

### 索引策略

| 索引名 | 表 | 列 | 类型 | 说明 |
|--------|----|-----|------|------|
| IX_MedicalCases_PatientId | MedicalCases | PatientId | 普通索引 | 按患者查询医案 |
| IX_MedicalCases_UserId | MedicalCases | UserId | 普通索引 | 按医生查询医案 |
| IX_MedicalCases_PatientId_Active | MedicalCases | PatientId | **筛选唯一索引** | BR-001 同一患者单活跃医案约束 (MC-D06) |

**BR-001 筛选唯一索引** (MC-D06): 仅对 `CaseStatus IN (Active, Suspended)` 的记录建立唯一索引。EF Core 配置:

```csharp
entity.HasIndex(e => e.PatientId)
    .HasFilter("[CaseStatus] IN (0, 1) AND [IsDeleted] = 0")
    .IsUnique()
    .HasDatabaseName("IX_MedicalCases_PatientId_Active");
```

> **设计取舍**: NFR 并发用户 1-3 人，并发创建重复草稿概率极低。代码层 BR-001 检查为主，DB 唯一索引为兜底保障。

### 敏感数据

Patient 实体的以下字段标记为敏感数据，日志和序列化时脱敏:
- IdNumber (身份证号)
- PhoneNumber (手机号)
- Address (地址)
- AllergyHistory (过敏史)
- MedicalHistory (病史)

### 软删除

- 所有继承 BaseEntity 的实体支持软删除
- 通过 `IsDeleted = true` 标记
- 全局查询过滤器自动排除
- 使用 `IgnoreQueryFilters()` 查询已删除记录

## 架构决策记录

- [ADR-0001: MedicalCase 聚合根](decisions/0001-medicalcase-aggregate-root.md) — MedicalCase 为唯一充血模型，域方法封装状态变更逻辑

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 LYBT.Entities 代码逆向工程 |
| 2026-02-18 | v1.1 | PRD同步: MedicalCase 新增 IsPrinted 字段 (MC-D15, 从 Prescription 提升到聚合根); Prescription 移除 IsPrinted (打印保护由聚合根统一管理); Patient.Status 补充禁用语义 (PAT-D05) |
| 2026-02-19 | v1.2 | 设计补全: 索引策略章节 (BR-001 筛选唯一索引 MC-D06); Herb 禁用药材显示规则 (MC-D07); Prescription 价格计算公式 (MC-D14) |
| 2026-02-21 | v1.3 | 打印层级提升: ER 图和聚合根图 PrescriptionPrintLog->MedicalCasePrintLog (FK 改为 MedicalCase); MedicalCase 新增 PrintVersion; Prescription 移除 PrintVersion (保留 PrintCount/LastPrintedAt) |
| 2026-02-21 | v1.4 | 深度重构同步: MedicalCaseStatus 移除 Cancelled=3 (取消统一为 IsDeleted=true); MedicalCase 新增 DDD 域方法 (Complete/Suspend/SoftDelete/UpdateConsultation)，从贫血模型演进为充血模型 |
| 2026-02-26 | v1.5 | DOC3-10: 新增"辅助实体"章节，汇总 MedicalCasePrintLog/PrescriptionItem/FormulaHerbItem/AuthSession+RefreshToken 的职责和设计要点 |
| 2026-02-28 | v1.6 | **PRD 偏差修复**: Patient 补充 IdType/EmergencyContact*/DisableReason 字段 (PRD-01); Herb 补充 Remark 字段 (PRD-05); PrintCount/LastPrintedAt 从 Prescription 移到 MedicalCase (PRD-06) |
| 2026-03-06 | v1.7 | **Draft->Suspended 对齐**: MedicalCaseStatus 枚举 Draft=0 更新为 Suspended=0 (MC-D20); DDD 域方法 SaveAsDraft() 更新为 Suspend(); BR-001 索引描述更新 |
| 2026-03-06 | v1.8 | **Registration 实体**: ER 图新增 Registration 关系; 独立实体图新增 Registration; 新增 Registration 实体字段表 + 状态机; 新增 RegistrationSource/RegistrationStatus 枚举 |

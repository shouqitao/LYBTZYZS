# LYBTZYZS Solution Architecture — Method/Field-Level Analysis

> Generated via codegraph full-solution analysis on 2026-06-28  
> Precision: every property, method signature, enum value, route, and configuration field

---

## [S1] Solution Structure

**40 projects** in `LYBTZYZS.sln` (VS 2022, .NET 8.0):

```
src/
├── Server/
│   ├── Server.Core/
│   │   ├── LYBT.Entities              — Domain entities (13 types)
│   │   └── LYBT.Infrastructure         — DbContext, Repositories, Auth, DI
│   ├── Server.BusinessModules/
│   │   ├── LYBT.Module.Auth            — JWT authentication
│   │   ├── LYBT.Module.Users           — User/role management
│   │   ├── LYBT.Module.Patients        — Patient CRUD
│   │   ├── LYBT.Module.Herbs           — Herb management
│   │   ├── LYBT.Module.Formula         — Formula management
│   │   ├── LYBT.Module.MedicalCase     — Medical cases (DDD aggregate)
│   │   ├── LYBT.Module.Registration    — Registration/queue
│   │   └── LYBT.Module.Reports         — Daily reports
│   └── Server.Services/
│       └── LYBT.WebAPI                 — ASP.NET Core host (port 5000)
├── Client/Desktop/
│   ├── Desktop.Core/                   — 8 infrastructure projects
│   ├── Desktop.BusinessModules/        — 8 mirror modules
│   ├── Desktop.Roles/                  — 4 role workspaces
│   ├── Shell/                          — Prism.DryIoc entry point
│   └── LocalWebAPI/                    — Embedded ASP.NET Core (port 5300)
├── Shared/                             — 8 shared libraries
└── Tools/                              — 4 utility projects
tests/
├── LYBT.Tests.Server/                  — Integration (real SQL + Respawn)
├── LYBT.Tests.Desktop/                 — Desktop (LocalDB)
└── LYBT.Tests.Architecture/            — Module isolation guards
```

---

## [S2] Entities — Every Property & Method

### BaseEntity (abstract)
**Path**: `src/Server/Core/LYBT.Entities/Common/BaseEntity.cs`  
**Implements**: `IAuditableEntity`, `ISoftDeletable`

| Property | Type | Nullable | Attributes | Default |
|----------|------|----------|------------|---------|
| `Id` | `Guid` | N | `[Key]` `[DisplayName("唯一标识")]` | `Guid.NewGuid()` |
| `CreatedAt` | `DateTime` | N | `[DisplayName("创建时间")]` | `DateTime.UtcNow` |
| `UpdatedAt` | `DateTime?` | Y | `[DisplayName("更新时间")]` | `null` |
| `CreatedBy` | `Guid?` | Y | `[DisplayName("创建者")]` | `null` |
| `UpdatedBy` | `Guid?` | Y | `[DisplayName("更新者")]` | `null` |
| `RowVersion` | `byte[]?` | Y | `[Timestamp]` `[DisplayName("版本")]` | `null` |
| `IsDeleted` | `bool` | N | `[DisplayName("删除标记")]` | `false` |

### IAuditableEntity (interface)
```csharp
DateTime CreatedAt { get; set; }
Guid? CreatedBy { get; set; }
DateTime? UpdatedAt { get; set; }
Guid? UpdatedBy { get; set; }
```

### ISoftDeletable (interface)
```csharp
bool IsDeleted { get; set; }
```

---

### Patient
**Path**: `src/Server/Core/LYBT.Entities/Patients/PatientModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Name` | `string` | N | `[Required]` `[StringLength(50)]` `[DisplayName("患者姓名")]` |
| `PinYinCode` | `string?` | Y | `[StringLength(50)]` `[DisplayName("拼音码")]` |
| `Gender` | `Gender` | N | `[DisplayName("性别")]` |
| `BirthDate` | `DateTime?` | Y | `[DisplayName("出生日期")]` |
| `IdNumber` | `string?` | Y | `[StringLength(18)]` `[SensitiveData(SensitiveDataType.IdentityInfo)]` `[DisplayName("身份证号")]` |
| `PhoneNumber` | `string?` | Y | `[StringLength(20)]` `[SensitiveData(SensitiveDataType.ContactInfo)]` `[DisplayName("手机号码")]` |
| `Status` | `CommonStatus` | N | `[DisplayName("状态")]` default `CommonStatus.Enabled` |
| `Age` | `int?` | Y | `[NotMapped]` `[DisplayName("年龄")]` — computed from BirthDate |

**Methods**: None (anemic model). Business logic in `IPatientService`.

---

### MedicalCase (DDD Aggregate Root)
**Path**: `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseModel.cs`  
**Inherits**: `BaseEntity`

**Persisted columns:**

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `PatientId` | `Guid` | N | `[Required]` `[DisplayName("患者ID")]` |
| `PatientName` | `string` | N | `[Required]` `[StringLength(50)]` `[DisplayName("患者姓名")]` |
| `UserId` | `Guid` | N | `[Required]` `[DisplayName("医生ID")]` |
| `DoctorName` | `string` | N | `[Required]` `[StringLength(50)]` `[DisplayName("医生姓名")]` |
| `CaseNumber` | `string?` | Y | `[StringLength(50)]` `[DisplayName("医案编号")]` |
| `CaseStatus` | `MedicalCaseStatus` | N | `[DisplayName("医案状态")]` default `MedicalCaseStatus.Active` |
| `NeedsPrescription` | `bool?` | Y | `[DisplayName("是否需要处方")]` |
| `CompletedAt` | `DateTime?` | Y | `[DisplayName("完成时间")]` |

**Navigation properties:**
- `Consultation` → `virtual Consultation?` (1:1, shared PK)
- `Prescription` → `virtual Prescription?` (1:0..1, FK MedicalCaseId)

**Computed properties:**
- `IsLocked` → `bool`: `IsCompleted && CompletedAt.HasValue && CompletedAt.Value.Date < DateTime.UtcNow.Date`
- `IsActive` → `bool`: `CaseStatus == Suspended || CaseStatus == Active`
- `IsCompleted` → `bool`: `CaseStatus == Completed`

**Domain methods:**
```csharp
void Complete()          // CaseStatus = Completed, CompletedAt = UpdatedAt = UtcNow
void Suspend()           // CaseStatus = Suspended, UpdatedAt = UtcNow
void SoftDelete()        // IsDeleted = true, UpdatedAt = UtcNow
void UpdateConsultation(string? presentIllness, string? tongueDiagnosis, string? pulseDiagnosis, string? tcmDiagnosis)
```

---

### Consultation
**Path**: `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `PresentIllness` | `string?` | Y | `[StringLength(2000)]` `[DisplayName("主诉")]` |
| `TongueDiagnosis` | `string?` | Y | `[StringLength(500)]` `[DisplayName("舌诊")]` |
| `PulseDiagnosis` | `string?` | Y | `[StringLength(500)]` `[DisplayName("脉诊")]` |
| `TcmDiagnosis` | `string?` | Y | `[StringLength(500)]` `[DisplayName("中医诊断")]` |

---

### Prescription
**Path**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `MedicalCaseId` | `Guid` | N | `[Required]` `[DisplayName("医案ID")]` |
| `PrescriptionNumber` | `string?` | Y | `[StringLength(50)]` `[DisplayName("处方编号")]` |
| `DosageCount` | `int` | N | `[DisplayName("剂数")]` default `1` |
| `Discount` | `decimal` | N | `[Precision(3,2)]` `[DisplayName("折扣")]` default `1.00` |
| `Usage` | `string?` | Y | `[StringLength(500)]` `[DisplayName("用法")]` |
| `Advice` | `string?` | Y | `[StringLength(1000)]` `[DisplayName("医嘱")]` |
| `ReferencedFormulas` | `string?` | Y | `[StringLength(500)]` `[DisplayName("引用验方")]` |
| `Remark` | `string?` | Y | `[StringLength(1000)]` `[DisplayName("备注")]` |
| `Items` | `ICollection<PrescriptionItem>` | Y | navigation |

---

### PrescriptionItem
**Path**: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItem.cs`  
**Does NOT inherit BaseEntity**

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Id` | `Guid` | N | `[Key]` |
| `PrescriptionId` | `Guid` | N | FK to Prescription |
| `HerbId` | `Guid` | N | FK to Herb (NOT nullable) |
| `HerbName` | `string` | N | `[StringLength(100)]` |
| `Dosage` | `int` | N | `[DisplayName("剂量")]` |
| `Unit` | `string` | N | `[StringLength(10)]` default `"g"` |
| `DecocteMethod` | `DecocteMethod` | N | default `DecocteMethod.Default` |
| `UnitPrice` | `decimal` | N | `[Precision(18,2)]` |
| `Amount` | `decimal` | N | computed: `Dosage * UnitPrice` |
| `Usage` | `string?` | Y | `[StringLength(100)]` |
| `Remark` | `string?` | Y | `[StringLength(500)]` |

---

### Herb
**Path**: `src/Server/Core/LYBT.Entities/Herbs/HerbModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Name` | `string` | N | `[Required]` `[StringLength(100)]` `[DisplayName("药材名称")]` |
| `PinYinCode` | `string?` | Y | `[StringLength(50)]` `[DisplayName("拼音码")]` |
| `Category` | `string?` | Y | `[StringLength(50)]` `[DisplayName("分类")]` |
| `Properties` | `string?` | Y | `[StringLength(100)]` `[DisplayName("性味")]` |
| `Origin` | `string?` | Y | `[StringLength(100)]` `[DisplayName("产地")]` |
| `Spec` | `string?` | Y | `[StringLength(50)]` `[DisplayName("规格")]` |
| `Unit` | `string` | N | `[Required]` `[StringLength(20)]` `[DisplayName("单位")]` |
| `Price` | `decimal` | N | `[Precision(18,2)]` `[Range(0, 999999.99)]` `[DisplayName("售价")]` |
| `CostPrice` | `decimal?` | Y | `[Precision(18,2)]` `[Range(0, 999999.99)]` `[DisplayName("成本价")]` |
| `Effect` | `string?` | Y | `[StringLength(1000)]` `[DisplayName("功效")]` |
| `Usage` | `string?` | Y | `[StringLength(500)]` `[DisplayName("用法")]` |
| `Status` | `CommonStatus` | N | `[DisplayName("状态")]` default `CommonStatus.Enabled` |

---

### Formula
**Path**: `src/Server/Core/LYBT.Entities/Formulas/FormulaModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Name` | `string` | N | `[Required]` `[StringLength(100)]` `[DisplayName("验方名称")]` |
| `Effect` | `string?` | Y | `[StringLength(200)]` `[DisplayName("功效")]` |
| `Indication` | `string?` | Y | `[StringLength(500)]` `[DisplayName("适应症")]` |
| `Usage` | `string?` | Y | `[StringLength(200)]` `[DisplayName("用法")]` |
| `Remark` | `string?` | Y | `[StringLength(500)]` `[DisplayName("备注")]` |
| `Property` | `string?` | Y | `[StringLength(200)]` `[DisplayName("属性")]` |
| `Status` | `CommonStatus` | N | default `CommonStatus.Enabled` |
| `IsShared` | `bool` | N | default `false` |
| `ValidationStatus` | `FormulaValidationStatus` | N | default `FormulaValidationStatus.Draft` |
| `Category` | `string?` | Y | `[StringLength(100)]` |
| `FormulaType` | `FormulaType` | N | default `FormulaType.Experience` |
| `UserId` | `Guid?` | Y | FK to user |
| `Herbs` | `ICollection<FormulaHerbItem>` | Y | navigation |

---

### FormulaHerbItem
**Path**: `src/Server/Core/LYBT.Entities/Formulas/FormulaHerbItem.cs`  
**Does NOT inherit BaseEntity**

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Id` | `Guid` | N | `[Key]` |
| `FormulaId` | `Guid` | N | FK to Formula |
| `Formula` | `Formula?` | Y | navigation |
| `HerbId` | `Guid?` | Y | FK to Herb |
| `OriginalHerbName` | `string?` | Y | `[StringLength(100)]` |
| `IsValidated` | `bool` | N | default `false` |
| `HerbName` | `string` | N | `[StringLength(100)]` |
| `Dosage` | `int` | N | default `1` |
| `Unit` | `string` | N | default `"g"` |
| `Usage` | `string?` | Y | `[StringLength(100)]` |
| `Remark` | `string?` | Y | `[StringLength(500)]` |
| `ProcessingMethod` | `string?` | Y | `[StringLength(100)]` |
| `DecocteMethod` | `DecocteMethod` | N | default `DecocteMethod.Default` |

---

### Registration
**Path**: `src/Server/Core/LYBT.Entities/Registrations/RegistrationModel.cs`  
**Inherits**: `BaseEntity`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `PatientId` | `Guid` | N | `[Required]` `[DisplayName("患者ID")]` |
| `PatientName` | `string` | N | `[Required]` `[StringLength(100)]` `[DisplayName("患者姓名")]` |
| `DoctorId` | `Guid` | N | `[Required]` `[DisplayName("医生ID")]` |
| `DoctorName` | `string` | N | `[Required]` `[StringLength(100)]` `[DisplayName("医生姓名")]` |
| `MedicalCaseId` | `Guid?` | Y | `[DisplayName("关联医案ID")]` |
| `Source` | `RegistrationSource` | N | `[DisplayName("挂号来源")]` |
| `Status` | `RegistrationStatus` | N | `[DisplayName("挂号状态")]` default `RegistrationStatus.Waiting` |
| `QueueNumber` | `int` | N | `[Required]` `[DisplayName("排队号")]` |
| `RegistrationFee` | `decimal` | N | `[Precision(10,2)]` `[DisplayName("挂号费")]` |
| `Remark` | `string?` | Y | `[StringLength(500)]` `[DisplayName("备注")]` |

---

### ApplicationUser
**Path**: `src/Server/Core/LYBT.Entities/Users/ApplicationUser.cs`  
**Inherits**: `IdentityUser<Guid>` + implements `IAuditableEntity`, `ISoftDeletable`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `RealName` | `string` | N | `[Required]` `[StringLength(100)]` `[DisplayName("真实姓名")]` |
| `PinYinCode` | `string?` | Y | `[StringLength(50)]` `[DisplayName("拼音码")]` |
| `Role` | `UserRole` | N | `[DisplayName("用户角色")]` |
| `IsSysAdmin` | `bool` | N | `[DisplayName("系统管理员")]` default `false` |
| `Status` | `CommonStatus` | N | `[DisplayName("状态")]` default `CommonStatus.Enabled` |
| `MustChangeOnNextLogin` | `bool` | N | `[DisplayName("下次登录必须修改密码")]` default `false` |
| `LastLoginAt` | `DateTime?` | Y | `[DisplayName("最后登录时间")]` |
| `Remark` | `string?` | Y | `[StringLength(500)]` `[DisplayName("备注")]` |
| `CreatedAt` | `DateTime` | N | audit |
| `CreatedBy` | `Guid?` | Y | audit |
| `UpdatedAt` | `DateTime?` | Y | audit |
| `UpdatedBy` | `Guid?` | Y | audit |
| `IsDeleted` | `bool` | N | soft-delete |
| `RowVersion` | `byte[]?` | Y | `[Timestamp]` |

---

### ApplicationRole
**Path**: `src/Server/Core/LYBT.Entities/Users/ApplicationRole.cs`  
**Inherits**: `IdentityRole<Guid>`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Description` | `string?` | Y | `[StringLength(200)]` |

---

### AuthSession
**Path**: `src/Server/Core/LYBT.Entities/Auth/AuthSessionModel.cs`

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Id` | `Guid` | N | `[Key]` |
| `UserId` | `Guid` | N | |
| `TokenHash` | `string` | N | `[MaxLength(255)]` |
| `LoginTime` | `DateTime` | N | |
| `LogoutTime` | `DateTime?` | Y | |
| `ExpiryTime` | `DateTime` | N | |
| `IpAddress` | `string?` | Y | `[MaxLength(45)]` |
| `UserAgent` | `string?` | Y | `[MaxLength(500)]` |
| `IsRevoked` | `bool` | N | default `false` |
| `Status` | `CommonStatus` | N | |

---

### SystemLog
**Path**: `src/Server/Core/LYBT.Entities/Common/SystemLog.cs`  
**Does NOT inherit BaseEntity**

| Property | Type | Nullable | Attributes |
|----------|------|----------|------------|
| `Id` | `int` | N | `[Key]` (auto-increment) |
| `Timestamp` | `DateTime` | N | |
| `Level` | `string` | N | `[MaxLength(50)]` |
| `Message` | `string` | N | |
| `Exception` | `string?` | Y | |
| `LoggerName` | `string?` | Y | `[MaxLength(255)]` |
| `UserId` | `Guid?` | Y | |
| `RequestId` | `string?` | Y | `[MaxLength(36)]` |
| `CorrelationId` | `string?` | Y | `[MaxLength(36)]` |
| `MachineName` | `string?` | Y | `[MaxLength(100)]` |
| `ThreadId` | `int?` | Y | |
| `Properties` | `string?` | Y | |

---

## [S3] Enums — Every Value

### UserRole
```csharp
Receptionist = 0    // [Description("前台")]
Doctor = 1          // [Description("医生")]
Admin = 10          // [Description("管理员")]
SuperAdmin = 100    // [Description("超级管理员")]
```

### MedicalCaseStatus
```csharp
Suspended = 0       // [Description("暂停")]
Active = 1          // [Description("进行中")]
Completed = 2       // [Description("已完成")]
```

### MedicalCaseQueryType
```csharp
All = 0             // [Description("全部")]
ByPatient = 1       // [Description("按患者")]
Pending = 2         // [Description("待诊")]
Unfinished = 3      // [Description("未完成")]
Recent = 4          // [Description("最近")]
```

### RegistrationSource
```csharp
Receptionist = 0    // [Description("前台挂号")]
Doctor = 1          // [Description("医生挂号")]
```

### RegistrationStatus
```csharp
Waiting = 0         // [Description("等待中")]
InProgress = 1      // [Description("就诊中")]
Completed = 2       // [Description("已完成")]
Cancelled = 3       // [Description("已取消")]
```

### Gender
```csharp
Unknown = 0         // [Description("未知")]
Male = 1            // [Description("男")]
Female = 2          // [Description("女")]
```

### DecocteMethod
```csharp
Default = 0         // [Description("默认")]
PreDecoct = 1       // [Description("先煎")]
PostAdd = 2         // [Description("后下")]
MeltIn = 3          // [Description("烊化")]
TakeWithWater = 4   // [Description("冲服")]
WrapDecoct = 5      // [Description("包煎")]
SeparateDecoct = 6  // [Description("另煎")]
```

### HerbRole
```csharp
None = 0            // [Description("无")]
Sovereign = 1       // [Description("君药")]
Minister = 2        // [Description("臣药")]
Assistant = 3       // [Description("佐药")]
Guide = 4           // [Description("使药")]
```

### FormulaType
```csharp
Classic = 1         // [Description("经典方")]
Experience = 2      // [Description("经验方")]
```

### FormulaValidationStatus
```csharp
Draft = 0           // [Description("草稿")]
Validated = 1       // [Description("已验证")]
```

### CommonStatus
```csharp
Disabled = 0        // [Description("禁用")]
Enabled = 1         // [Description("启用")]
```

### PasswordStrength
```csharp
Weak = 1            // [Description("弱")]
Fair = 2            // [Description("一般")]
Good = 3            // [Description("良好")]
Strong = 4          // [Description("强")]
VeryStrong = 5      // [Description("非常强")]
```

### ErrorCategory
```csharp
General = 0         // [Description("通用")]
Validation = 1      // [Description("验证")]
Authentication = 2  // [Description("认证")]
Authorization = 3   // [Description("授权")]
NotFound = 4        // [Description("未找到")]
Conflict = 5        // [Description("冲突")]
Business = 6        // [Description("业务")]
External = 7        // [Description("外部")]
System = 8          // [Description("系统")]
Network = 9         // [Description("网络")]
Unknown = 99        // [Description("未知")]
```

### ErrorSeverity
```csharp
Info = 0            // [Description("信息")]
Warning = 1         // [Description("警告")]
Error = 2           // [Description("错误")]
Critical = 3        // [Description("严重")]
Fatal = 4           // [Description("致命")]
```

### DuplicateStrategy
```csharp
Skip = 0            // [Description("跳过")]
Update = 1          // [Description("更新")]
Error = 2           // [Description("报错")]
```

---

## [S4] Repository Interfaces & Methods

### IRepository\<T\> (base, 15 methods)
**Path**: `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepository.cs`  
**Constraint**: `where T : class`

```
GetByIdAsync(Guid id, CancellationToken) → Task<T?>
GetAllAsync(CancellationToken) → Task<IEnumerable<T>>
GetPagedAsync(int pageNumber, int pageSize, string? keyword, CancellationToken) → Task<PagedResult<T>>
FindAsync(Expression<Func<T,bool>>, CancellationToken) → Task<IEnumerable<T>>
GetSingleAsync(Expression<Func<T,bool>>, CancellationToken) → Task<T?>
AddAsync(T entity, CancellationToken) → Task<T>
UpdateAsync(T entity, CancellationToken) → Task<T>
DeleteAsync(Guid id, CancellationToken) → Task<bool>
AddRangeAsync(IEnumerable<T>, CancellationToken) → Task<IEnumerable<T>>
CountAsync(CancellationToken) → Task<int>
SaveChangesAsync(CancellationToken) → Task<int>
AddWithoutSaveAsync(T entity, CancellationToken) → Task<T>
UpdateWithoutSaveAsync(T entity, CancellationToken) → Task<T>
DeleteWithoutSaveAsync(Guid id, CancellationToken) → Task<bool>
SaveAllChangesAsync(CancellationToken) → Task<int>
```

### BaseRepository\<TEntity\> (abstract, adds 14 extra methods)
**Path**: `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`  
**Constraint**: `where TEntity : BaseEntity`

**Additional public/virtual methods:**

```
FindAsync(Expression<Func<TEntity,bool>>?, Func<IQueryable,IOrderedQueryable>?, string[]? includes, int? skip, int? take) → Task<List<TEntity>>
SelectAsync<TResult>(Expression<Func<TEntity,bool>>?, Expression<Func<TEntity,TResult>>) → Task<List<TResult>>
GetPagedAsync(int page, int size, Expression<Func<TEntity,bool>>?, Expression<Func<TEntity,object>>?, bool ascending) → Task<PagedResult<TEntity>>
ExistsAsync(Expression<Func<TEntity,bool>>) → Task<bool>
ExistsAsync(Guid) → Task<bool>
CountAsync(Expression<Func<TEntity,bool>>?) → Task<int>
UpdateRangeAsync(IEnumerable<TEntity>) → Task
DeleteRangeAsync(Expression<Func<TEntity,bool>>) → Task<int>
RestoreAsync(Guid) → Task<bool>
HardDeleteAsync(Guid) → Task<bool>
GetQueryable() → IQueryable<TEntity>
GetNoTrackingQueryable() → IQueryable<TEntity>
FromSqlRawAsync(string sql, params object[]) → Task<List<TEntity>>
```

**Protected helpers:**
```
GetPagedResultAsync(IQueryable<TEntity>, int page, int size, CancellationToken) → Task<PagedResult<TEntity>>
```

**Template hooks (virtual, overridable):**
```
ApplyKeywordFilter(IQueryable<TEntity>, string?) → IQueryable<TEntity>  // default: no-op
ApplyDefaultOrdering(IQueryable<TEntity>) → IOrderedQueryable<TEntity>  // default: OrderByDescending(CreatedAt)
```

---

### PatientRepository (6 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Patients/Repositories/PatientRepository.cs`

```
GetByNameAsync(string name) → Task<List<Patient>>
ExistsAsync(string name, Guid? excludeId) → Task<bool>
GetByPhoneNumberAsync(string phoneNumber) → Task<Patient?>
GetByIdNumberAsync(string idNumber) → Task<Patient?>
GetPagedWithStatusFilterAsync(int page, int pageSize, string? keyword, CommonStatus status) → Task<PagedResult<Patient>>
GetByIdIncludingDeletedAsync(Guid id) → Task<Patient?>
```

**Template overrides:**
- `ApplyKeywordFilter`: matches `Name.Contains(keyword)` OR `PinYinCode.Contains(keyword)`
- `ApplyDefaultOrdering`: `OrderBy(Name)` ascending

---

### MedicalCaseRepository (10 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

```
GetByPatientIdAsync(Guid patientId, CancellationToken) → Task<List<MedicalCase>>
GetByIdWithDetailsAsync(Guid id, CancellationToken) → Task<MedicalCase>
GetByIdWithDetailsFreshAsync(Guid id, CancellationToken) → Task<MedicalCase?>
GetPagedWithDetailsAsync(int page, int size, MedicalCaseStatus?, Guid? patientId, Guid? doctorId, bool isAdmin, string? keyword, CancellationToken) → Task<PagedResult<MedicalCase>>
GetPendingCasesAsync(Guid doctorId, Guid? patientId, CancellationToken) → Task<List<PendingMedicalCaseDto>>
GetAllPendingCasesAsync(CancellationToken) → Task<List<PendingMedicalCaseDto>>
QueryAsync(string? patientName, DateTime? startDate, DateTime? endDate, string? diagnosisKeyword, CancellationToken) → Task<List<MedicalCase>>
GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken) → Task<MedicalCase?>
GetBatchWithDetailsAsync(List<Guid> ids, CancellationToken) → Task<List<MedicalCase>>
CountByPrefixAsync(string prefix, CancellationToken) → Task<int>
CountPrescriptionsByPrefixAsync(string prefix, CancellationToken) → Task<int>
```

**Private query builders:**
- `GetBaseQuery()` → `_dbSet.Where(m => !m.IsDeleted)` (no includes)
- `GetDetailQuery()` → `.Include(Consultation).Include(Prescription).ThenInclude(Prescription.Items).Where(m => !m.IsDeleted)`

**Overridden UpdateAsync**: Handles Prescription/PrescriptionItem entity state transitions (Added vs Modified), Detached-entity handling via `GetOrLoadExistingEntityAsync`.

---

### MedicalCaseReferenceRepository (3 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseReferenceRepository.cs`

```
CountUnfinishedAsync(Guid patientId, CancellationToken) → Task<int>
CountAllAsync(Guid patientId, CancellationToken) → Task<int>
GetRecentAsync(Guid patientId, int count, CancellationToken) → Task<List<MedicalCaseReferenceDto>>
```

---

### HerbRepository (5 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Herbs/Repositories/HerbRepository.cs`

```
GetByNameAsync(string name) → Task<Herb?>
GetByNameOrPinyinAsync(string searchTerm) → Task<Herb?>
ExistsByNameAsync(string name, Guid? excludeId) → Task<bool>
GetPagedAsync(int page, int size, string? keyword, string? category) → Task<PagedResult<Herb>>
GetByIdIncludingDeletedAsync(Guid id) → Task<Herb?>
```

**Template overrides:**
- `ApplyKeywordFilter`: matches `Name.Contains` OR `PinYinCode.Contains`
- `ApplyDefaultOrdering`: `OrderBy(h => string.IsNullOrEmpty(h.Name)).ThenBy(h => h.PinYinCode).ThenBy(h => h.Name)`

---

### HerbReferenceRepository (3 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Herbs/Repositories/HerbReferenceRepository.cs`

```
GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken) → Task<int>
GetFormulaReferenceCountAsync(Guid herbId, CancellationToken) → Task<int>
GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken) → Task<List<PrescriptionReferenceDto>>
```

---

### FormulaRepository (8 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs`

```
GetTemplatesAsync() → Task<List<Formula>>
GetByIdWithHerbsAsync(Guid id) → Task<Formula>
FindWithHerbsAsync(Expression<Func<Formula,bool>>, CancellationToken) → Task<List<Formula>>
GetPagedWithDetailsAsync(int page, int size, string? keyword) → Task<PagedResult<Formula>>
GetPagedWithDetailsAsync(int page, int size, string? keyword, string? category, Guid? userId, bool isAdmin) → Task<PagedResult<Formula>>
GetByUserIdAsync(Guid userId) → Task<List<Formula>>
GetAllWithHerbsAsync() → Task<List<Formula>>
GetByIdIncludingDeletedAsync(Guid id) → Task<Formula?>
```

**Template override**: keyword matches `Name.Contains` OR `Effect.Contains`

---

### RegistrationRepository (7 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Registration/Repositories/RegistrationRepository.cs`

```
GetPagedAsync(int page, int size, string? keyword, DateTime? startDate, DateTime? endDate, Guid? patientId, Guid? doctorId) → Task<PagedResult<Registration>>
GetWaitingQueueAsync(Guid? doctorId) → Task<List<Registration>>
GetByStatusAsync(RegistrationStatus status, Guid? doctorId) → Task<List<Registration>>
HasWaitingRegistrationAsync(Guid patientId) → Task<bool>
GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken) → Task<Registration?>
GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken) → Task<int>
GetTodayMaxQueueNumberAsync() → Task<int>
```

---

### ReportRepository (5 extra methods)
**Path**: `src/Server/Modules/LYBT.Module.Reports/Repositories/ReportRepository.cs`

```
GetTodayRegistrationFeeTotalAsync(CancellationToken) → Task<decimal>
GetTodayMedicineFeeTotalAsync(CancellationToken) → Task<decimal>
GetTodayConsultationCountAsync(CancellationToken) → Task<int>
GetTodayConsultationsByDoctorAsync(CancellationToken) → Task<List<DoctorCountDto>>
GetTodayHerbUsageAsync(CancellationToken) → Task<List<HerbUsageItemDto>>
```

---

## [S5] Service Interfaces — Every Method

### IJwtService (5 methods)
**Path**: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IJwtService.cs`

```
GenerateToken(string userId, string userName, UserRole role, string userType = "user") → string
GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType = "user") → string
ValidateToken(string token) → ClaimsPrincipal?
RefreshToken(string expiredToken) → Result<LoginResponse>
ValidateAutoLoginToken(string autoLoginToken) → Result<LoginResponse>
```

### IAuthService (7 methods)
**Path**: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAuthService.cs`

```
LoginAsync(LoginRequest request, CancellationToken) → Task<Result<LoginResponse>>
LogoutAsync(LogoutRequest request) → Task<Result<bool>>
VerifyCredentialsAsync(LoginRequest request, CancellationToken) → Task<Result<string>>
ValidateTokenAsync(string token) → Task<Result<bool>>
GetSessionInfoAsync(string token) → Task<Result<object>>
LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken) → Task<Result<LoginResponse>>
ChangePasswordAsync(string userId, ChangePasswordRequest request) → Task<Result<bool>>
```

### IUserService (estimated ~10 methods)
**Path**: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserService.cs`

```
GetUsersAsync(int page, int pageSize, string? keyword) → Task<Result<PagedResult<UserListDto>>>
GetUserByIdAsync(Guid id) → Task<Result<UserDetailDto>>
CreateUserAsync(UserInputDto input) → Task<Result<UserDetailDto>>
UpdateUserAsync(Guid id, UserInputDto input) → Task<Result<UserDetailDto>>
DeleteUserAsync(Guid id) → Task<Result<bool>>
ToggleStatusAsync(Guid id) → Task<Result<UserDetailDto>>
ResetPasswordAsync(Guid id, bool mustChangeOnNextLogin) → Task<Result<ResetPasswordResponseDto>>
ChangePasswordAsync(Guid id, ChangePasswordRequest request) → Task<Result<bool>>
ChangeProfileAsync(Guid id, ChangeProfileDto input) → Task<Result<UserDetailDto>>
BatchDeleteAsync(List<Guid> ids) → Task<Result<BatchOperationResultDto>>
GetCurrentUserAsync(Guid userId) → Task<Result<UserDetailDto>>
```

### IPatientService (estimated ~10 methods)
**Path**: `src/Server/Modules/LYBT.Module.Patients/Interfaces/IPatientService.cs`

```
GetPatientsAsync(int page, int pageSize, string? keyword) → Task<Result<PagedResult<PatientListDto>>>
GetPatientByIdAsync(Guid id) → Task<Result<PatientDetailDto>>
CreatePatientAsync(PatientInputDto input) → Task<Result<PatientDetailDto>>
UpdatePatientAsync(Guid id, PatientInputDto input) → Task<Result<PatientDetailDto>>
DeletePatientAsync(Guid id) → Task<Result<bool>>
ToggleStatusAsync(Guid id) → Task<Result<PatientDetailDto>>
BatchDeleteAsync(List<Guid> ids) → Task<Result<BatchOperationResultDto>>
BatchImportAsync(PatientBatchImportInputDto input) → Task<Result<PatientBatchImportResultDto>>
CheckReferenceAsync(Guid patientId) → Task<Result<PatientReferenceCheckDto>>
BatchCheckReferenceAsync(PatientBatchCheckReferenceInputDto input) → Task<Result<List<PatientReferenceCheckDto>>>
```

### IHerbService (estimated ~10 methods)
**Path**: `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbService.cs`

```
GetHerbsAsync(int page, int pageSize, string? keyword, string? category) → Task<Result<PagedResult<HerbListDto>>>
GetHerbByIdAsync(Guid id) → Task<Result<HerbDetailDto>>
CreateHerbAsync(HerbInputDto input) → Task<Result<HerbDetailDto>>
UpdateHerbAsync(Guid id, HerbInputDto input) → Task<Result<HerbDetailDto>>
DeleteHerbAsync(Guid id) → Task<Result<bool>>
ToggleStatusAsync(Guid id) → Task<Result<HerbDetailDto>>
BatchDeleteAsync(List<Guid> ids) → Task<Result<BatchOperationResultDto>>
BatchImportAsync(HerbBatchImportInputDto input) → Task<Result<HerbBatchImportResultDto>>
CheckReferenceAsync(Guid herbId) → Task<Result<HerbReferenceCheckDto>>
BatchCheckReferenceAsync(HerbBatchCheckReferenceInputDto input) → Task<Result<List<HerbReferenceCheckDto>>>
```

### IFormulaService (estimated ~10 methods)
**Path**: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`

```
GetFormulasAsync(int page, int pageSize, string? keyword, string? category) → Task<Result<PagedResult<FormulaListDto>>>
GetFormulaByIdAsync(Guid id) → Task<Result<FormulaDetailDto>>
CreateFormulaAsync(FormulaInputDto input) → Task<Result<FormulaDetailDto>>
UpdateFormulaAsync(Guid id, FormulaInputDto input) → Task<Result<FormulaDetailDto>>
DeleteFormulaAsync(Guid id) → Task<Result<bool>>
ToggleStatusAsync(Guid id) → Task<Result<FormulaDetailDto>>
BatchDeleteAsync(List<Guid> ids) → Task<Result<BatchOperationResultDto>>
GetPendingValidationAsync() → Task<Result<List<FormulaListDto>>>
ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto input) → Task<Result<bool>>
```

### IFormulaImportExportService (estimated ~4 methods)
**Path**: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaImportExportService.cs`

```
BatchImportAsync(FormulaBatchImportInputDto input) → Task<Result<FormulaBatchImportResultDto>>
ExportFormulasAsync(List<Guid>? ids) → Task<Result<List<FormulaDetailDto>>>
ImportFromFileAsync(Stream fileStream, string fileName) → Task<Result<FormulaBatchImportResultDto>>
ExportToFileAsync(List<Guid>? ids) → Task<Result<byte[]>>
```

### IMedicalCaseFacade (estimated ~12 methods)
**Path**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseFacade.cs`

```
CreateAsync(MedicalCaseInputDto input, Guid doctorId, string doctorName) → Task<Result<MedicalCaseDetailDto>>
UpdateAsync(Guid id, MedicalCaseInputDto input) → Task<Result<MedicalCaseDetailDto>>
DeleteAsync(Guid id) → Task<Result<bool>>
GetByIdAsync(Guid id) → Task<Result<MedicalCaseDetailDto>>
GetListAsync(int page, int pageSize, MedicalCaseStatus? status, Guid? patientId, string? keyword) → Task<Result<PagedResult<MedicalCaseListDto>>>
QueryAsync(MedicalCaseQueryDto query) → Task<Result<List<MedicalCaseListDto>>>
SearchAsync(string? patientName, DateTime? startDate, DateTime? endDate, string? diagnosisKeyword) → Task<Result<List<MedicalCaseListDto>>>
GetConsultationsAsync(Guid id) → Task<Result<ConsultationDetailDto>>
GetPrescriptionsAsync(Guid id) → Task<Result<PrescriptionDetailDto>>
SetPrescriptionFlagAsync(Guid id, bool needsPrescription) → Task<Result<bool>>
BatchDeleteAsync(List<Guid> ids) → Task<Result<BatchOperationResultDto>>
GetPendingCasesAsync(Guid doctorId, Guid? patientId) → Task<Result<List<PendingMedicalCaseDto>>>
```

### IMedicalCaseCommandService
```
CreateMedicalCaseAsync(MedicalCaseInputDto, Guid doctorId, string doctorName) → Task<Result<MedicalCase>>
UpdateMedicalCaseAsync(Guid id, MedicalCaseInputDto) → Task<Result<MedicalCase>>
DeleteMedicalCaseAsync(Guid id) → Task<Result<bool>>
```

### IMedicalCaseQueryService
```
GetMedicalCaseByIdAsync(Guid id) → Task<Result<MedicalCase>>
GetMedicalCaseListAsync(int page, int size, ...) → Task<Result<PagedResult<MedicalCaseListDto>>>
QueryMedicalCasesAsync(MedicalCaseQueryDto) → Task<Result<List<MedicalCaseListDto>>>
SearchMedicalCasesAsync(...) → Task<Result<List<MedicalCaseListDto>>>
GetPendingCasesAsync(Guid doctorId, Guid? patientId) → Task<Result<List<PendingMedicalCaseDto>>>
```

### IMedicalCaseStateService
```
CompleteAsync(Guid id) → Task<Result<bool>>
SuspendAsync(Guid id, string? presentIllness, ...) → Task<Result<bool>>
CancelAsync(Guid id) → Task<Result<bool>>
CloseAsync(Guid id) → Task<Result<bool>>
SetPrescriptionFlagAsync(Guid id, bool needsPrescription) → Task<Result<bool>>
```

### IRegistrationService (estimated ~8 methods)
**Path**: `src/Server/Modules/LYBT.Module.Registration/Interfaces/IRegistrationService.cs`

```
CreateAsync(RegistrationInputDto input) → Task<Result<RegistrationDetailDto>>
QuickVisitAsync(QuickVisitInputDto input, Guid doctorId, string doctorName) → Task<Result<QuickVisitResultDto>>
GetByIdAsync(Guid id) → Task<Result<RegistrationDetailDto>>
GetListAsync(int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, Guid? patientId, Guid? doctorId) → Task<Result<PagedResult<RegistrationListDto>>>
GetWaitingQueueAsync(Guid? doctorId) → Task<Result<List<RegistrationListDto>>>
StartVisitAsync(Guid id) → Task<Result<RegistrationDetailDto>>
CancelAsync(Guid id) → Task<Result<bool>>
```

### IReportService (estimated ~4 methods)
**Path**: `src/Server/Modules/LYBT.Module.Reports/Interfaces/IReportService.cs`

```
GetDailyIncomeAsync() → Task<Result<DailyIncomeDto>>
GetDailyConsultationsAsync() → Task<Result<DailyConsultationDto>>
GetDailyHerbUsageAsync() → Task<Result<DailyHerbUsageDto>>
```

---

## [S6] Controllers — Every Action Method

### BaseApiController
**Path**: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`

```
GetOperator() → (Guid OperatorId, string OperatorName, UserRole OperatorRole)
LogOperation(string operation, object? data = null, Guid? targetId = null)
GetModelErrors() → List<string>
IsValidGuid(Guid id) → bool
GetRequestId() → string
IsModelValid → bool (property)
GetValidationErrorMessage() → string
Success(string message) → IActionResult
Success<T>(T data, string message) → IActionResult
SuccessPaged<T>(PagedResult<T>, string message) → IActionResult
Error(string message) → IActionResult
NotFound(string message) → IActionResult
BusinessFail(string message, string? errorCode) → IActionResult
ValidationFail(string message) → IActionResult
HandleResult<T>(Result<T>, string, bool useAuthMapping) → IActionResult
HandleResult(Result, string) → IActionResult
HandleServiceResult<T>(ServiceResult<T>, string) → IActionResult
HandlePagedResult<T>(Result<PagedResult<T>>, string) → IActionResult
HandleBoolResult(Result<bool>, string) → IActionResult
IsAdminOrOwner(Guid? createdBy) → bool
ValidateOwnership<TDto>(TDto, Func<TDto,Guid?>) → IActionResult?
ValidateModel() → IActionResult?
ValidatePagination(int page, int size) → IActionResult?
GetEntityWithOwnershipCheckAsync<TDto>(Guid id, Func<Guid,Task<TDto?>>, Func<TDto,Guid?>) → (TDto?, IActionResult?)
```

### AuthController — `api/v1/auth`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| POST | `login` | `LoginAsync(LoginRequest)` | AllowAnonymous | JWT token generation |
| POST | `logout` | `LogoutAsync(LogoutRequest)` | AllowAnonymous | Stateless logout |
| POST | `refresh` | `RefreshToken(RefreshTokenRequest)` | AllowAnonymous | Refresh token |
| POST | `auto-login` | `AutoLoginAsync(AutoLoginRequest)` | AllowAnonymous | Desktop auto-login |
| GET | `validate` | `ValidateToken()` | Authorize | Token introspection |

### PatientsController — `api/v1/patients`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `` | `GetPatients(page,pageSize,keyword)` | DoctorOrAdmin | Paged list with OutputCache |
| GET | `{id}` | `GetPatientById(id)` | DoctorOrAdmin | Single patient |
| POST | `` | `CreatePatient(PatientInputDto)` | DoctorOrAdmin | Returns 201 |
| PUT | `{id}` | `UpdatePatient(id,PatientInputDto)` | DoctorOrAdmin | Full update |
| DELETE | `{id}` | `DeletePatient(id)` | DoctorOrAdmin | Soft delete |
| POST | `toggle-status` | `ToggleStatus(id)` | DoctorOrAdmin | Enable/disable |
| POST | `batch-delete` | `BatchDelete(BatchDeleteInputDto)` | DoctorOrAdmin | Bulk soft delete |

### HerbsController — `api/v1/herbs`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `` | `GetHerbs(page,pageSize,keyword,category)` | DoctorOrAdmin | Paged list with OutputCache |
| GET | `{id}` | `GetHerbById(id)` | DoctorOrAdmin | Single herb |
| POST | `` | `CreateHerb(HerbInputDto)` | DoctorOrAdmin | Returns 201 |
| PUT | `{id}` | `UpdateHerb(id,HerbInputDto)` | DoctorOrAdmin | Full update |
| DELETE | `{id}` | `DeleteHerb(id)` | DoctorOrAdmin | Soft delete |
| POST | `toggle-status` | `ToggleStatus(id)` | DoctorOrAdmin | Enable/disable |
| POST | `batch-delete` | `BatchDelete(BatchDeleteInputDto)` | DoctorOrAdmin | Bulk soft delete |
| POST | `batch-import` | `BatchImport(HerbBatchImportInputDto)` | DoctorOrAdmin | Excel import |

### FormulasController — `api/v1/formulas`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `` | `GetFormulas(page,pageSize,keyword,category)` | DoctorOrAdmin | Paged with role-based visibility |
| GET | `{id}` | `GetFormulaById(id)` | DoctorOrAdmin | Single formula |
| POST | `` | `CreateFormula(FormulaInputDto)` | DoctorOrAdmin | Returns 201 |
| PUT | `{id}` | `UpdateFormula(id,FormulaInputDto)` | DoctorOrAdmin | Full update |
| DELETE | `{id}` | `DeleteFormula(id)` | DoctorOrAdmin | Soft delete |
| POST | `toggle-status` | `ToggleStatus(id)` | DoctorOrAdmin | Enable/disable |
| POST | `batch-delete` | `BatchDelete(BatchDeleteInputDto)` | DoctorOrAdmin | Bulk soft delete |
| POST | `batch-import` | `BatchImport(FormulaBatchImportInputDto)` | DoctorOrAdmin | Excel import |
| GET | `pending-validation` | `GetPendingValidation()` | DoctorOrAdmin | Unvalidated formulas |
| POST | `{formulaId}/herbs/{herbItemId}/validate` | `ValidateHerb(formulaId,herbItemId,ValidateFormulaHerbInputDto)` | DoctorOrAdmin | Validate herb item |

### MedicalCasesController — `api/v1/medicalcases`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| POST | `` | `CreateMedicalCase(MedicalCaseInputDto)` | DoctorOrAdmin | Create aggregate |
| PUT | `{id}` | `UpdateMedicalCase(id,MedicalCaseInputDto)` | DoctorOrAdmin | Save aggregate |
| DELETE | `{id}` | `DeleteMedicalCase(id)` | DoctorOrAdmin | Soft delete |
| GET | `{id}` | `GetMedicalCaseById(id)` | DoctorOrAdmin | Full detail |
| GET | `` | `GetMedicalCases(page,pageSize,status,patientId,keyword)` | DoctorOrAdmin | Paged list |
| GET | `query` | `QueryMedicalCases(MedicalCaseQueryDto)` | DoctorOrAdmin | Unified multi-type query |
| GET | `search` | `SearchMedicalCases(patientName,startDate,endDate,diagnosisKeyword)` | DoctorOrAdmin | Cross-case search |
| GET | `{id}/consultations` | `GetConsultations(id)` | DoctorOrAdmin | Consultation detail |
| GET | `{id}/prescriptions` | `GetPrescriptions(id)` | DoctorOrAdmin | Prescription detail |
| PUT | `{id}/prescription-flag` | `SetPrescriptionFlag(id,SetPrescriptionFlagRequest)` | DoctorOrAdmin | 3-step workflow |
| POST | `batch-delete` | `BatchDelete(BatchDeleteInputDto)` | DoctorOrAdmin | Bulk soft delete |

### MedicalCaseProcessingController — `api/v1/medicalcases`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| PUT | `{id}/status` | `UpdateStatus(id,MedicalCaseStatusInputDto)` | DoctorOrAdmin | State transitions |
| PUT | `{id}/close` | `CloseCase(id)` | DoctorOrAdmin | Skip workflow validation |
| PUT | `{id}/suspend` | `SuspendCase(id,MedicalCaseStatusInputDto)` | DoctorOrAdmin | With consultation data |
| PUT | `{id}/cancel` | `CancelCase(id)` | DoctorOrAdmin | Soft delete + audit |

### RegistrationsController — `api/v1/registrations`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| POST | `` | `CreateRegistration(RegistrationInputDto)` | DoctorOrAdmin | Manual registration |
| POST | `quick-visit` | `QuickVisit(QuickVisitInputDto)` | DoctorOrAdmin | Silent Registration+MedicalCase |
| GET | `{id}` | `GetRegistrationById(id)` | DoctorOrAdmin | Single registration |
| GET | `` | `GetRegistrations(page,pageSize,keyword,startDate,endDate,patientId,doctorId)` | DoctorOrAdmin | Paged list |
| GET | `queue` | `GetWaitingQueue(doctorId?)` | DoctorOrAdmin | Waiting queue |
| PUT | `{id}/start-visit` | `StartVisit(id)` | DoctorOrAdmin | Mark InProgress |
| PUT | `{id}/cancel` | `CancelRegistration(id)` | DoctorOrAdmin | Cancel |

### ReportsController — `api/v1/reports`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `daily/income` | `GetDailyIncome()` | DoctorOrAdmin | Today's income |
| GET | `daily/consultations` | `GetDailyConsultations()` | DoctorOrAdmin | Today's consultation count |
| GET | `daily/herbs` | `GetDailyHerbUsage()` | DoctorOrAdmin | Today's herb usage |

### ConfigurationController — `api/v1/configuration`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `` | `GetAllConfiguration()` | AdminOrSuperAdmin | All config |
| GET | `{key}` | `GetConfigurationByKey(key)` | AdminOrSuperAdmin | Single value |
| POST | `validate` | `ValidateConfiguration()` | AdminOrSuperAdmin | Production check |

### DiagnosticsController — `api/v1/diagnostics`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `logging/status` | `GetLoggingStatus()` | AdminOrSuperAdmin | Current log level |
| POST | `logging/debug/enable` | `EnableDebugMode()` | AdminOrSuperAdmin | Enable debug logging |
| POST | `logging/debug/disable` | `DisableDebugMode()` | AdminOrSuperAdmin | Disable debug logging |
| POST | `logging/level` | `SetLoggingLevel(SetLoggingLevelRequest)` | AdminOrSuperAdmin | Set log level |

### HealthController — `api/v1/health`
| HTTP | Route | Method | Auth | Description |
|------|-------|--------|------|-------------|
| GET | `` | `Get()` | AllowAnonymous | Liveness |
| GET | `ping` | `Ping()` | AllowAnonymous | Simple ping |
| GET | `details` | `GetHealthDetails()` | Authorize | DB connectivity |

---

## [S7] DTOs — Every Property

### Common

**ApiResponse\<T\>**: `bool Success` | `string Message` | `T? Data` | `object? Errors` | `long Timestamp` | `string RequestId`

**PagedResult\<T\>**: `List<T> Items` | `int TotalCount` | `int CurrentPage` | `int PageSize` | `int TotalPages` (computed) | `bool HasPreviousPage` (computed) | `bool HasNextPage` (computed) | `string? ErrorMessage` | `List<T> Data` (alias for Items)

**PagedQueryBaseDto**: `string? Keyword` | `int PageIndex = 1` | `int PageSize = 20` | `string? SortField` | `bool IsDescending` | `int Skip` (computed) | `Dictionary<string,object> Extensions`

**ServiceResult\<T\>**: `bool IsSuccess` | `T? Data` | `string? ErrorMessage` | `Exception? Exception` | `string? Message` (alias)

**ServiceResult**: `bool IsSuccess` | `string? ErrorMessage` | `Exception? Exception` | `string? Message`

**ValidationResult**: `bool IsValid` | `string? ErrorMessage` | `string? RuleName` | `List<string> Details`

**OperationResultDto**: `bool IsSuccess = true` | `string Message = ""` | `string? ErrorCode` | `DateTime OperationTime`

**BatchOperationResultDto**: + `int TotalCount` | `int SuccessCount` | `int FailureCount` | `int SkippedCount` | `List<Guid> SuccessfulIds` | `List<Guid> FailedIds` | `List<ErrorDetail> Errors` | `List<BatchOperationFailureItem> FailedItems` | `decimal SuccessRate` (computed)

**ImportResultDto**: + `int DuplicateCount` | `string ImportBatchId` | `string? FileName` | `List<string> DuplicateRecords` | `List<string> FailedRecords` | `DateTime ImportTime`

**HandledError**: `ErrorCategory Category` | `ErrorSeverity Severity` | `string UserMessage` | `string TechnicalMessage` | `string? ErrorCode` | `List<string>? SuggestedActions` | `DateTime Timestamp`

### Auth

**LoginRequest**: `string UserName [Required]` | `string Password [Required]` | `string? ClientIp` | `string? UserAgent` | `string? LoginType = "Password"` | `bool RememberMe = false` | `string? DeviceId` | `string? DeviceName`

**SuperAdminLoginRequest**: `string Password [Required]` | `string? IpAddress` | `DateTime? Timestamp`

**LoginResponse**: `string Token` | `UserDetailDto User` | `string RefreshToken` | `DateTime ExpiresAt` | `string? AutoLoginToken` | `bool MustChangePassword`

**TokenPair**: `string AccessToken` | `string RefreshToken` | `DateTime AccessTokenExpires` | `DateTime RefreshTokenExpires` | `string TokenType = "Bearer"` | `int ExpiresIn` (computed) | `Guid? UserId` | `string? UserName` | `string? UserRole` | `string? DeviceId`

**ValidateTokenRequest**: `string Token [Required]`

**ValidateTokenResponse**: `bool IsValid` | `int? UserId` | `string? Username` | `string? Role` | `DateTime? ExpiresAt` | `string? ErrorMessage`

**RefreshTokenRequest**: `string RefreshToken` | `string? DeviceId`

**ChangePasswordRequest**: `string OldPassword [Required]` | `string NewPassword [Required, StringLength(50,MinLength=8)]`

**ChangeSysAdminPassword**: `string OldPassword` | `string NewPassword`

**LogoutRequest**: `string? UserName` | `string? RefreshToken` | `string? DeviceId`

**AutoLoginRequest**: `string UserName [Required]` | `string AutoLoginToken [Required]` | `string? ClientIp` | `string? UserAgent` | `string? DeviceId` | `string? DeviceName`

### MedicalCase

**MedicalCaseInputDto**: `Guid? Id` | `Guid PatientId [Required]` | `string PatientName [Required]` | `Guid UserId` | `string DoctorName` | `string? CaseNumber` | `MedicalCaseStatus CaseStatus` | `bool? NeedsPrescription` | `ConsultationInputDto? Consultation` | `PrescriptionInputDto? Prescription`

**MedicalCaseListDto**: `Guid Id` | `Guid PatientId` | `string PatientName` | `Guid DoctorId` | `string DoctorName` | `string? CaseNumber` | `MedicalCaseStatus CaseStatus` | `bool? NeedsPrescription` | `DateTime CreatedAt` | `DateTime? CompletedAt` | `string? Diagnosis`

**MedicalCaseDetailDto**: All of ListDto + `ConsultationDetailDto? Consultation` | `PrescriptionDetailDto? Prescription` | `DateTime? UpdatedAt`

**MedicalCaseQueryDto**: `MedicalCaseQueryType QueryType` | `Guid? PatientId` | `Guid? DoctorId` | `MedicalCaseStatus? Status` | `string? Keyword` | `DateTime? StartDate` | `DateTime? EndDate` | `int PageIndex = 1` | `int PageSize = 20`

**MedicalCaseStatusInputDto**: `MedicalCaseStatus Status` | `string? PresentIllness` | `string? TongueDiagnosis` | `string? PulseDiagnosis` | `string? TcmDiagnosis`

**PendingMedicalCaseDto**: `Guid PatientId` | `string PatientName` | `string PhoneNumber` | `string PhoneMasked` | `MedicalCaseStatus CaseStatus` | `Guid? MedicalCaseId` | `DateTime CreatedAt` | `int QueueNumber`

**SetPrescriptionFlagRequest**: `bool NeedsPrescription [Required]`

**BatchDetailQueryDto**: `List<Guid> Ids [Required, MinLength(1), MaxLength(50)]`

### Consultation

**ConsultationInputDto**: `string? PresentIllness [StringLength(2000)]` | `string? TongueDiagnosis [StringLength(500)]` | `string? PulseDiagnosis [StringLength(500)]` | `string? TcmDiagnosis [StringLength(500)]`

**ConsultationDetailDto**: `Guid MedicalCaseId` | `string? PresentIllness` | `string? TongueDiagnosis` | `string? PulseDiagnosis` | `string? TcmDiagnosis`

### Prescription

**PrescriptionInputDto**: `Guid? Id` | `int DosageCount = 1` | `decimal Discount = 1.00` | `string? Usage` | `string? Advice` | `string? ReferencedFormulas` | `string? Remark` | `List<PrescriptionItemInputDto> Items`

**PrescriptionDetailDto**: `Guid Id` | `Guid MedicalCaseId` | `string? PrescriptionNumber` | `int DosageCount` | `decimal Discount` | `string? Usage` | `string? Advice` | `string? ReferencedFormulas` | `string? Remark` | `List<PrescriptionItemDto> Items` | `decimal TotalPrice` (computed)

**PrescriptionItemInputDto**: `Guid? Id` | `Guid HerbId [Required]` | `string HerbName [Required]` | `int Dosage [Required, Range(1,1000)]` | `string Unit [Required]` | `DecocteMethod DecocteMethod` | `decimal UnitPrice` | `string? Usage` | `string? Remark`

**PrescriptionItemDto**: `Guid Id` | `Guid HerbId` | `string HerbName` | `int Dosage` | `string Unit` | `DecocteMethod DecocteMethod` | `decimal UnitPrice` | `decimal Amount` | `string? Usage` | `string? Remark`

### Formula

**FormulaInputDto**: `Guid? Id` | `string Name [Required]` | `string? Effect` | `string? Description` | `string? Usage` | `string? Property` | `string? Category` | `bool IsShared` | `string? Instructions` | `string? Indications` | `string? Contraindications` | `string? Preparation` | `string? Remark` | `List<FormulaHerbItemInputDto> Herbs [Required]`

**FormulaListDto**: `Guid Id` | `string Name` | `string? Effect` | `string? Indications` | `string? Category` | `bool IsShared` | `FormulaValidationStatus ValidationStatus` | `CommonStatus Status` | `int HerbCount` | `decimal TotalPrice` | `DateTime CreatedAt`

**FormulaDetailDto**: All of ListDto + `string? Description` | `string? Usage` | `string? Property` | `string? Instructions` | `string? Contraindications` | `string? Preparation` | `string? Source` | `List<FormulaHerbItemDto> Herbs` | `string HerbNames` (computed) | `Guid? CreatedBy`

**FormulaHerbItemDto**: `Guid Id` | `Guid? HerbId` | `string? OriginalHerbName` | `bool IsValidated` | `string HerbName` | `int Dosage` | `string Unit` | `string? Preparation` | `string? Processing` | `string? Usage` | `decimal Price` | `decimal UnitPrice` (computed) | `string? ProcessingMethod` | `string? SpecialInstructions` | `int SortOrder` | `DecocteMethod DecocteMethod` | `HerbDetailDto? Herb`

**FormulaHerbItemInputDto**: `Guid? Id` | `Guid? HerbId` | `string HerbName [Required]` | `int Dosage [Required, Range(1,500)]` | `string Unit [Required]` | `string? Preparation` | `string? ProcessingMethod` | `string? Usage` | `int SortOrder` | `DecocteMethod DecocteMethod`

**ValidateFormulaHerbInputDto**: `Guid SelectedHerbId`

**FormulaBatchImportInputDto**: `List<FormulaImportItemDto> Formulas [Required]` | `string? FileName`

**FormulaBatchImportResultDto**: inherits ImportResultDto + `DateTime StartTime` | `DateTime EndTime` | `int MatchedHerbsCount` | `int UnmatchedHerbsCount` | `List<FormulaDetailDto> SuccessfulFormulas` | `List<FormulaImportFailureDto> Failures`

**FormulaImportItemDto**: `string Name [Required]` | `string? Effect` | `string? Usage` | `string? Property` | `bool IsShared` | `string? Instructions` | `string? Indications` | `string? Contraindications` | `string? Preparation` | `string? Remark` | `string? Source` | `List<FormulaHerbImportItemDto> Herbs [Required]`

**FormulaImportFailureDto**: `int RowIndex` | `string FormulaName` | `string ErrorMessage` | `string? ErrorDetails` | `string? OriginalData`

**FormulaHerbImportItemDto**: `string HerbName [Required]` | `int Dosage [Required, Range(1,500)]` | `string? Unit` | `string? Preparation` | `string? Usage` | `int SortOrder` | `string? OriginalHerbId`

**FormulaHerbExportItemDto**: `Guid HerbId` | `string HerbName` | `int Dosage` | `string Unit` | `string? Preparation` | `string? Usage` | `decimal Price` | `decimal Subtotal` | `int SortOrder`

### Herbs

**HerbInputDto**: `Guid? Id` | `string Name [Required]` | `string? PinYinCode` | `string? Category` | `string? Properties` | `string? Origin` | `string? Spec` | `string Unit [Required]` | `decimal Price [Required, Range(0,999999.99)]` | `decimal? CostPrice` | `string? Effect` | `string? Usage` | `string? Remark`

**HerbListDto**: `Guid Id` | `string Name` | `string? PinYinCode` | `string? Category` | `string? Origin` | `string? Spec` | `string Unit` | `decimal Price` | `CommonStatus Status` | `DateTime CreatedAt`

**HerbDetailDto**: All of ListDto + `string? Properties` | `decimal? CostPrice` | `string? Effect` | `string? Usage` | `string? Remark` | `DateTime? UpdatedAt` | `Guid? CreatedBy`

**HerbReferenceCheckDto**: `Guid HerbId` | `string HerbName` | `bool HasReferences` | `int ReferenceCount` | `bool CanDelete = true` | `string? DeleteWarning` | `List<PrescriptionReferenceDto>? RecentReferences`

**PrescriptionReferenceDto**: `Guid PrescriptionId` | `string PrescriptionNumber` | `string PatientName` | `DateTime CreatedAt` | `string Status`

**HerbBatchImportInputDto**: `List<HerbInputDto> Herbs` | `DuplicateStrategy Strategy = DuplicateStrategy.Skip`

**HerbBatchImportResultDto**: inherits ImportResultDto + `List<HerbImportFailureDto> Failures`

**HerbImportFailureDto**: `int RowNumber` | `string HerbName` | `string Reason` | `List<string> ErrorDetails`

**HerbBatchCheckReferenceInputDto**: `List<Guid> HerbIds`

### Patients

**PatientInputDto**: `Guid? Id` | `string Name [Required]` | `Gender Gender` | `DateTime? BirthDate` | `string? IdNumber` | `string? PhoneNumber` | `string? PinYinCode` | `CommonStatus Status` | `string? Remark`

**PatientListDto**: `Guid Id` | `string Name` | `Gender Gender` | `int? Age` | `string? PhoneNumber` | `CommonStatus Status` | `DateTime CreatedAt`

**PatientDetailDto**: All of ListDto + `DateTime? BirthDate` | `string? IdNumber` | `string? PinYinCode` | `DateTime? UpdatedAt` | `Guid? CreatedBy`

**PatientReferenceCheckDto**: `Guid PatientId` | `string PatientName` | `bool HasReferences` | `int ReferenceCount` | `bool CanDelete = true` | `string? DeleteWarning` | `List<MedicalCaseReferenceDto>? RecentMedicalCases`

**MedicalCaseReferenceDto**: `Guid MedicalCaseId` | `string CaseNumber` | `DateTime CreatedAt` | `string Status`

**PatientBatchImportInputDto**: `List<PatientInputDto> Patients` | `DuplicateStrategy Strategy`

**PatientBatchImportResultDto**: inherits ImportResultDto + `List<PatientImportFailureDto> Failures`

**PatientImportFailureDto**: `int OriginalRowNumber` | `string FailureReason` | `string FieldName` | `string OriginalValue` | `string SuggestedFix` | `PatientInputDto DataSnapshot`

**PatientBatchCheckReferenceInputDto**: `List<Guid> PatientIds`

### Registration

**RegistrationInputDto**: `Guid PatientId [Required]` | `string PatientName [Required]` | `Guid DoctorId [Required]` | `string DoctorName [Required]` | `RegistrationSource Source` | `decimal RegistrationFee [Range(0,999999.99)]` | `string? Remark`

**RegistrationListDto**: `Guid Id` | `Guid PatientId` | `string PatientName` | `Guid DoctorId` | `string DoctorName` | `Guid? MedicalCaseId` | `int QueueNumber` | `decimal RegistrationFee` | `RegistrationSource Source` | `RegistrationStatus Status` | `DateTime CreatedAt` | `bool HasMedicalCase` (computed)

**RegistrationDetailDto**: All of ListDto + `string? Remark` | `DateTime? UpdatedAt` | `Guid? CreatedBy`

**QuickVisitInputDto**: `Guid PatientId [Required]` | `string PatientName [Required]` | `string? Remark`

**QuickVisitResultDto**: `Guid RegistrationId` | `Guid MedicalCaseId` | `Guid PatientId` | `string PatientName` | `Guid DoctorId` | `string DoctorName` | `DateTime CreatedAt`

### Users

**UserInputDto**: `Guid? Id` | `string? UserName [StringLength(32,MinLength=3)]` | `string? Password [StringLength(128,MinLength=6)]` | `string? ConfirmPassword [Compare("Password")]` | `string? RealName [StringLength(50)]` | `string? PinYinCode [StringLength(50)]` | `string? PhoneNumber [Phone]` | `string? Email [EmailAddress]` | `UserRole? Role = Doctor` | `string? Remark [StringLength(500)]`

**UserListDto**: `Guid Id` | `string UserName` | `string RealName` | `string? PhoneNumber` | `UserRole Role` | `CommonStatus Status` | `bool IsEnabled` (computed) | `DateTime? LastLoginTime` | `DateTime CreatedAt`

**UserDetailDto**: All of ListDto + `string? Email` | `string? PinYinCode` | `int FailedLoginCount` | `DateTime? UpdatedAt` | `string? Remark`

**ChangePasswordDto**: `Guid UserId [Required]` | `string OldPassword [Required]` | `string NewPassword [Required]` | `string ConfirmNewPassword [Compare("NewPassword")]`

**ChangeProfileDto**: `string RealName [Required]` | `string? PhoneNumber [Phone]` | `string? Email [EmailAddress]`

**ResetPasswordRequestDto**: `bool MustChangeOnNextLogin = true`

**ResetPasswordResponseDto**: `bool Success` | `string TemporaryPassword`

### Reports

**DailyIncomeDto**: `decimal TotalIncome` | `decimal RegistrationFeeTotal` | `decimal MedicineFeeTotal`

**DailyConsultationDto**: `int TotalCount` | `List<DoctorCountDto> ByDoctor`

**DailyHerbUsageDto**: `List<HerbUsageItemDto> Items`

**HerbUsageItemDto**: `string HerbName` | `int UsageCount` | `decimal TotalDosage`

**DoctorCountDto**: `string DoctorName` | `int Count`

### Diagnostics

**SetLoggingLevelRequest**: `string Level [Required]`

**EnableDebugModeRequest**: `bool Enable`

### Health

**DatabaseHealthCheckResult**: `bool IsHealthy` | `string Message` | `long ResponseTimeMs`

---

## [S8] Desktop Layer — API Clients, ViewModels, Navigation

### IApiClient Sub-Interfaces

**IApiClientAuth** (7 methods):
```
LoginAsync(LoginRequest) → ApiResponse<LoginResponse>
LoginWithAutoTokenAsync(AutoLoginRequest) → ApiResponse<LoginResponse>
LogoutAsync(LogoutRequest) → ApiResponse
RefreshTokenAsync(RefreshTokenRequest) → ApiResponse<LoginResponse>
ValidateTokenFromHeaderAsync() → ApiResponse<object>
ValidateTokenAsync(ValidateTokenRequest) → ApiResponse<ValidateTokenResponse>
HealthCheckAsync() → ApiResponse<HealthCheckResponse>
```

**IApiClientUsers** (11 methods):
```
GetUsersAsync(int page, int pageSize, string? keyword) → ApiResponse<PagedResult<UserListDto>>
GetUserByIdAsync(Guid id) → ApiResponse<UserDetailDto>
CreateUserAsync(UserInputDto) → ApiResponse<UserDetailDto>
UpdateUserAsync(Guid id, UserInputDto) → ApiResponse<UserDetailDto>
DeleteUserAsync(Guid id) → ApiResponse
ChangeProfileAsync(Guid id, ChangeProfileDto) → ApiResponse<UserDetailDto>
ChangePasswordAsync(Guid id, ChangePasswordRequest) → ApiResponse
ResetPasswordAsync(Guid id, ResetPasswordRequestDto) → ApiResponse<ResetPasswordResponseDto>
ToggleStatusAsync(Guid id) → ApiResponse<UserDetailDto>
BatchDeleteAsync(BatchDeleteInputDto) → ApiResponse<BatchOperationResultDto>
GetCurrentUserAsync() → UserDetailDto (local-only)
```

**IApiClientPatients** (10 methods):
```
GetPatientsAsync(int page, int pageSize, string? keyword) → ApiResponse<PagedResult<PatientListDto>>
GetPatientByIdAsync(Guid id) → ApiResponse<PatientDetailDto>
CreatePatientAsync(PatientInputDto) → ApiResponse<PatientDetailDto>
UpdatePatientAsync(Guid id, PatientInputDto) → ApiResponse<PatientDetailDto>
DeletePatientAsync(Guid id) → ApiResponse
BatchImportAsync(PatientBatchImportInputDto) → ApiResponse<PatientBatchImportResultDto>
ExportTemplateAsync() → HttpResponseMessage
ExportPatientsAsync(string? keyword) → HttpResponseMessage
BatchDeleteAsync(BatchDeleteInputDto) → ApiResponse<BatchOperationResultDto>
ToggleStatusAsync(Guid id) → ApiResponse<PatientDetailDto>
```

**IApiClientHerbs** (10 methods):
```
GetHerbsAsync(int page, int pageSize, string? keyword, string? category) → ApiResponse<PagedResult<HerbListDto>>
GetHerbByIdAsync(Guid id) → ApiResponse<HerbDetailDto>
CreateHerbAsync(HerbInputDto) → ApiResponse<HerbDetailDto>
UpdateHerbAsync(Guid id, HerbInputDto) → ApiResponse<HerbDetailDto>
DeleteHerbAsync(Guid id) → ApiResponse
BatchImportAsync(HerbBatchImportInputDto) → ApiResponse<HerbBatchImportResultDto>
ExportTemplateAsync() → HttpResponseMessage
ExportHerbsAsync(string? keyword) → HttpResponseMessage
BatchDeleteAsync(BatchDeleteInputDto) → ApiResponse<BatchOperationResultDto>
ToggleStatusAsync(Guid id) → ApiResponse<HerbDetailDto>
```

**IApiClientFormulas** (10 methods):
```
GetFormulasAsync(int page, int pageSize, string? keyword, string? category) → ApiResponse<PagedResult<FormulaListDto>>
GetFormulaByIdAsync(Guid id) → ApiResponse<FormulaDetailDto>
CreateFormulaAsync(FormulaInputDto) → ApiResponse<FormulaDetailDto>
UpdateFormulaAsync(Guid id, FormulaInputDto) → ApiResponse<FormulaDetailDto>
DeleteFormulaAsync(Guid id) → ApiResponse
BatchImportAsync(FormulaBatchImportInputDto) → ApiResponse<FormulaBatchImportResultDto>
BatchDeleteAsync(BatchDeleteInputDto) → ApiResponse<BatchOperationResultDto>
ToggleStatusAsync(Guid id) → ApiResponse<FormulaDetailDto>
GetPendingValidationAsync() → ApiResponse<List<FormulaListDto>>
ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto) → ApiResponse<bool>
```

**IApiClientMedicalCases** (12 methods):
```
CreateMedicalCaseAsync(MedicalCaseInputDto) → ApiResponse<MedicalCaseDetailDto>
UpdateMedicalCaseAsync(Guid id, MedicalCaseInputDto) → ApiResponse<MedicalCaseDetailDto>
DeleteMedicalCaseAsync(Guid id) → ApiResponse
GetMedicalCaseByIdAsync(Guid id) → ApiResponse<MedicalCaseDetailDto>
GetMedicalCasesAsync(int page, int pageSize, MedicalCaseStatus? status, Guid? patientId, string? keyword) → ApiResponse<PagedResult<MedicalCaseListDto>>
QueryMedicalCasesAsync(MedicalCaseQueryDto) → ApiResponse<List<MedicalCaseListDto>>
SearchMedicalCasesAsync(string? patientName, DateTime? startDate, DateTime? endDate, string? diagnosisKeyword) → ApiResponse<List<MedicalCaseListDto>>
GetConsultationsAsync(Guid id) → ApiResponse<ConsultationDetailDto>
GetPrescriptionsAsync(Guid id) → ApiResponse<PrescriptionDetailDto>
SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest) → ApiResponse<bool>
BatchDeleteAsync(BatchDeleteInputDto) → ApiResponse<BatchOperationResultDto>
UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto) → ApiResponse<bool>
```

**IApiClientRegistrations** (7 methods):
```
CreateRegistrationAsync(RegistrationInputDto) → ApiResponse<RegistrationDetailDto>
QuickVisitAsync(QuickVisitInputDto) → ApiResponse<QuickVisitResultDto>
GetRegistrationByIdAsync(Guid id) → ApiResponse<RegistrationDetailDto>
GetRegistrationsAsync(int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, Guid? patientId, Guid? doctorId) → ApiResponse<PagedResult<RegistrationListDto>>
GetWaitingQueueAsync(Guid? doctorId) → ApiResponse<List<RegistrationListDto>>
StartVisitAsync(Guid id) → ApiResponse<RegistrationDetailDto>
CancelRegistrationAsync(Guid id) → ApiResponse<bool>
```

**IApiClientReports** (3 methods):
```
GetDailyIncomeAsync() → ApiResponse<DailyIncomeDto>
GetDailyConsultationsAsync() → ApiResponse<DailyConsultationDto>
GetDailyHerbUsageAsync() → ApiResponse<DailyHerbUsageDto>
```

### SwitchingApiClient Routing Logic
```
Current property:
  if _connectionSettings.CurrentUrl is localhost/127.0.0.1 → HttpClientApiClient (port 5300, LocalDB)
  else → RefitApiClient (remote WebAPI, SQL Server)
  Thread-safe via lock, lazy switching on URL change
```

### NavigationCoordinator
- `ViewToModuleMap`: Dictionary mapping view names → module names for lazy loading
- `_navigationHistory`: Stack<string> (max 20) for back navigation
- `_forwardStack`: Stack<string> for forward navigation
- `_breadcrumbs`: List<string> for UI display

### Role Definitions

**AdminRoleDefinition**: Modules = `[Auth, Users, Patients, Herbs, Formula, MedicalCase]` | HomeView = `AdminHomeView`

**DoctorRoleDefinition**: Modules = `[Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration]` | HomeView = `ClinicalWorkspaceView`

**ReceptionistRoleDefinition**: Modules = `[Auth, Users, Patients, Registration]` | HomeView = `ReceptionistHomeView`

**SuperAdminRoleDefinition**: Modules = `[Auth, Users, Sysadmin]` | HomeView = `SysadminHomeView`

### ViewModel Base Classes

**CoreViewModelBase**: `IsBusy` | `StatusMessage` | `ErrorMessage` | `ExecuteWithErrorHandlingAsync()` | `EventAggregator` | `IDisposable`

**NavigableViewModelBase**: + `INavigationAware` | `IRegionMemberLifetime` | `IConfirmNavigationRequest` | `IEditable` | `HasUnsavedChanges` | region navigation

**MasterDetailViewModelBase\<TListItem, TDetail\>**: + `IMasterDetailServices<T,T>` | `ObservableCollection<TListItem> Items` | `CurrentPage` | `PageSize` | `TotalCount` | `IsLoading` | CRUD commands | pagination | search

---

## [S9] AppDbContext — DbSets & Configuration

### DbSets (12)
```csharp
DbSet<AuthSession> AuthSessions
DbSet<Patient> Patients
DbSet<MedicalCase> MedicalCases
DbSet<Consultation> Consultations
DbSet<Prescription> Prescriptions
DbSet<PrescriptionItem> PrescriptionItems
DbSet<Herb> Herbs
DbSet<Formula> Formulas
DbSet<Registration> Registrations
DbSet<SystemLog> SystemLogs
// + Identity tables from IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

### OnModelCreating
```csharp
modelBuilder.ApplyOptimizations()  // global query filters + indexes
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)  // auto-discover IEntityTypeConfiguration
```

### SaveChangesAsync Override
```csharp
SetAuditFields()  // auto-fill CreatedAt, UpdatedAt, CreatedBy, UpdatedBy on IAuditableEntity
```

### Entity Configurations

**MedicalCaseConfiguration**: `UX_MedicalCases_Patient_ActiveOnly` unique filtered index (single active case per patient)

**ConsultationConfiguration**: 1:1 with MedicalCase via shared PK, Cascade delete

**PrescriptionConfiguration**: 1:1 with MedicalCase via MedicalCaseId, unique index

**PrescriptionItemConfiguration**: FK to Prescription (Cascade), UnitPrice precision(18,2)

**FormulaConfiguration**: 1:N with FormulaHerbItem (Cascade)

**FormulaHerbItemConfiguration**: FK to Herb (Restrict), Dosage default 1, Unit default "g"

**RegistrationConfiguration**: Indexes on PatientId, DoctorId, Status, MedicalCaseId

**SystemLogConfiguration**: Indexes on Timestamp, Level, CorrelationId, UserId

**EntityOptimizationExtensions**: Auto-discovers all `ISoftDeletable` types via reflection and applies `HasQueryFilter(e => !e.IsDeleted)`

---

## [S10] Key Patterns Summary

| Pattern | Scope | Detail |
|---------|-------|--------|
| Three-Layer | Server | Controller → Service → Repository → DbContext |
| MVVM | Desktop | View(XAML) ← ViewModel → Service → API |
| DDD Aggregate Root | MedicalCase | Only aggregate with Consultation + Prescription as internal entities |
| CQRS | MedicalCase | CommandService + QueryService + StateService |
| Module Isolation | All | Architecture tests enforce no cross-module references |
| Dual-Mode | System | SwitchingApiClient routes localhost→LocalWebAPI, else→Remote |
| Soft Delete | Entities | Global query filter on IsDeleted, IgnoreQueryFilters() for restore |
| ApiResponse Envelope | API | All controllers return ApiResponse\<T\> |
| Mapperly | Mapping | Compile-time [Mapper] partial classes (NOT AutoMapper) |
| CommunityToolkit.Mvvm | Desktop | [ObservableProperty]/[RelayCommand] (NOT Prism BindableBase) |
| Master-Detail Composite | Desktop | MasterDetailViewModelBase\<T,T\> for all entity views |
| Lazy Module Loading | Desktop | ViewToModuleMap triggers IModuleLoadingService on first navigation |
| Role-Based Loading | Desktop | IRoleDefinition → ApplicationBootstrapper.LoadModulesForRoleAsync() |
| Two-Phase Serilog | System | Bootstrap logger → Final logger |
| Central Package Mgmt | Build | All NuGet versions in Directory.Packages.props |

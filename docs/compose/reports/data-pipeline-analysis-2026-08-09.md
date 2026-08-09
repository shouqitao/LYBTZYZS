# 数据定义与传输链路深度分析报告

> **日期**: 2026-08-09
> **范围**: 6 个业务域（Herb / Formula / Patient / User / Registration / MedicalCase）
> **分析维度**: DB → Entity → DTO → API → Desktop Model → ViewModel → XAML 全链路
> **约束**: 只读分析，只给事实，不下建议

---

## 一、全景图

### 1.1 架构分层总览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  DB Layer (SQL Server)                                                      │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ AppDbContext (统一迁移链, ADR-0017)                                  │    │
│  │  + 5 模块 DbContext (Catalog/Identity/MedicalCase/Patients/Reg)     │    │
│  │  → 同库不同 DbContext, 逻辑隔离, 物理表由 AppDbContext 管理         │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ EF Core (Fluent API + IEntityTypeConfiguration)                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  Entity Layer (LYBT.Entities)                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ BaseEntity: Id/CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/             │    │
│  │             RowVersion/IsDeleted                                    │    │
│  │ Herb, Formula, FormulaHerbItem*, Patient, Registration,            │    │
│  │ MedicalCase(+Consultation+Prescription), ApplicationUser**         │    │
│  │ * FormulaHerbItem 不继承 BaseEntity                                 │    │
│  │ ** ApplicationUser 继承 IdentityUser<Guid>, 独立实现 IAuditableEntity│    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ Mapper (Server 端)                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  DTO Layer (LYBT.Shared.Models/Contracts)                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ 每域 3 个 DTO: XxxListDto / XxxDetailDto / XxxInputDto             │    │
│  │ Formula 额外: FormulaHerbItemDto / FormulaHerbItemInputDto         │    │
│  │ MedicalCase 额外: ConsultationDetailDto / PrescriptionDetailDto    │    │
│  │ 共享: ApiResponse<T>, PagedResult<T>, Result<T>                    │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ HTTP (Refit / SwitchingApiClient)                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  API Layer (双轨路由)                                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ IHerbApi / IFormulaApi / IPatientApi / IUserApi + IAuthApi /       │    │
│  │ IRegistrationApi / IMedicalCaseApi  (Refit 接口, remote 模式)      │    │
│  │ IApiClientXxx (统一接口, 无 Refit 属性, remote+local 共用)         │    │
│  │ SwitchingApiClient: localhost→HttpClientApiClient / 其他→RefitApiClient│ │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ Desktop 端                                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Desktop Model Layer (Modules/*/Models)                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ HerbDetailModel, FormulaDetailModel, PatientDetailModel,           │    │
│  │ UserDetailModel, MedicalCaseDetailModel                            │    │
│  │ + EditContext (Herb/Patient/User 有独立编辑上下文)                  │    │
│  │ *** Registration 无 Model, ViewModel 直接持有 DTO ***              │    │
│  │ 统一继承 ValidatableModelBase (INotifyDataErrorInfo)               │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ ViewModel                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  ViewModel Layer                                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ MasterDetailViewModelBase<TListDto, TDetailModel> (基类)           │    │
│  │ → XxxMasterDetailViewModel + XxxEditorViewModel (子 VM)            │    │
│  │ MedicalCase 额外: ConsultationEditorVM + PrescriptionEditorVM      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│       ↓ XAML Binding                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  UI Layer (XAML)                                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │ XxxMasterDetailControl → XxxViewControl (只读) + XxxEditControl (编辑)│  │
│  │ MasterDetail 模式: DataGrid(Items) + Transitioner 切换 View/Edit   │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 各域映射方式速查

| 域 | Server Mapper | Desktop Mapper | Desktop 有 Model? | EditContext? |
|---|---|---|---|---|
| Herb | CatalogDtoMapper (Mapperly+手写) | 无 (VM 内联手写) | ✅ HerbDetailModel | ✅ HerbEditContext |
| Formula | CatalogDtoMapper (Mapperly+手写) | FormulaDetailModelMapper (Mapperly+手写) | ✅ FormulaDetailModel | ✅ FormulaEditContext |
| Patient | PatientMapper (Mapperly) | 无 (VM 内联手写) | ✅ PatientDetailModel | ✅ PatientEditContext |
| User | IdentityMapper (Mapperly+手写) | 无 (VM 内联手写) | ✅ UserDetailModel | ✅ UserEditContext |
| Registration | RegistrationMapper (Mapperly) | 无 | ❌ 无 Model | ❌ 无 |
| MedicalCase | MedicalCaseMapper (Mapperly+手写丰富) | 4 个 Mapper (Mapperly+手写) | ✅ MedicalCaseDetailModel | ❌ 无 |

---

## 二、逐域链路分析

### 2.1 Herb（中药）域

#### 2.1.1 数据形状

**Entity `Herb`** (继承 BaseEntity, 表 `Herbs`):
- Id (Guid), Name (Required, 100), PinYinCode (50), Category (50), Properties (100),
  Origin (100), Spec (100), Unit (Required, 10), Price (decimal(18,2)),
  CostPrice (decimal(18,2)?), Effect (500), Usage (500), Remark (500),
  Status (CommonStatus→int), + BaseEntity 字段 (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/RowVersion/IsDeleted)
- 领域方法: Create(), UpdateProfile(), ChangeStatus(), SoftDelete(), Restore()

**HerbListDto** (10 字段):
- Id, Name, PinYinCode, Category, Origin, Spec, Unit, Price, Status, CreatedAt
- **缺失**: Properties, CostPrice, Effect, Usage, Remark, UpdatedAt, CreatedBy, IsDeleted

**HerbDetailDto** (实现 IAuditable, 16 字段):
- Id, Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price, CostPrice,
  Effect, Usage, Remark, Status, CreatedAt, UpdatedAt, CreatedBy
- **缺失**: RowVersion, IsDeleted, UpdatedBy
- **与 Entity 差异**: 无计算属性（Entity 无计算属性）

**HerbInputDto** (实现 IEntityInputDto, 14 字段):
- Id (Guid?), Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price,
  CostPrice, Effect, Usage, Remark
- **缺失**: Status (InputDto 不含状态，状态变更通过专用 API)

**HerbDetailModel** (继承 ValidatableModelBase, 16 字段):
- Id, Name, PinYinCode, Category, Properties, Origin, Spec, Unit, Price, CostPrice,
  Effect, Usage, Remark, Status, CreatedAt, UpdatedAt
- **UI 专有**: IsNew (Id==Guid.Empty 计算属性), Clone() 方法
- **与 DTO 差异**: 无 CreatedBy

#### 2.1.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→DTO | Mapperly 编译时生成 | CatalogDtoMapper.cs:41,46 | `ToHerbListDto`, `ToHerbDetailDto`，纯属性同名映射 |
| InputDto→Entity | **手写** | CatalogDtoMapper.cs:23-36 | 调用领域工厂 `Herb.Create()`，含校验+Trim |
| DTO→Model | **手写内联** | HerbMasterDetailViewModel.cs:114-130 | `new HerbDetailModel { ... }` 逐字段赋值 |
| Model→EditContext | **手写内联** | HerbEditorViewModel.cs:37-53 | `new HerbEditContext { ... }` 逐字段赋值 |
| EditContext→InputDto | **手写内联** | HerbEditorViewModel.cs:65-80 | `new HerbInputDto { ... }` + Trim |

#### 2.1.3 API 合约

- **Refit 接口**: IHerbApi.cs, 13 个端点, 全部 `ApiResponse<T>` 包装
- **统一接口**: IApiClientHerbs : IEntityApiSegment<HerbListDto, HerbDetailDto, HerbInputDto>
- **端点示例**: `GET /api/v1/herbs` → `PagedResult<HerbListDto>`, `POST /api/v1/herbs` → `HerbDetailDto`

#### 2.1.4 DB 配置

- **DbContext**: CatalogDbContext (模块) + AppDbContext (全局)
- **表名**: `Herbs` (PascalCase, 非 snake_case)
- **Fluent API**: HerbConfiguration 继承 BaseEntityConfiguration\<Herb\>, Status→int 枚举转换
- **列名**: EF Core 默认 PascalCase, 无显式列名映射

---

### 2.2 Formula（方剂）域

#### 2.2.1 数据形状

**Entity `Formula`** (继承 BaseEntity, 表 `Formulas`):
- Id, Name (200), Effect (500), Indication (1000), Usage (500), Remark (500),
  Property (300), Status (CommonStatus→int), IsShared (bool), ValidationStatus (FormulaValidationStatus),
  Category (50), FormulaType (FormulaType), UserId (Guid?), Herbs (ICollection\<FormulaHerbItem\>),
  HerbCount (计算属性, getter-only)
- **子表 FormulaHerbItem** (不继承 BaseEntity, 表 `FormulaHerbItems`):
  - Id (Guid PK), FormulaId (FK), HerbId (Guid? 延迟绑定), OriginalHerbName (100),
    IsValidated (bool), HerbName (Required, 100), Dosage (int, 默认 1), Unit (16, 默认 "g"),
    Usage (200), Remark (200), ProcessingMethod (100), DecocteMethod (DecocteMethod 枚举)

**FormulaListDto** (11 字段):
- Id, Name, Effect, Indications, Category, IsShared, ValidationStatus, Status,
  HerbCount, TotalPrice, CreatedAt
- **命名差异**: Entity `Indication` → DTO `Indications` (复数)
- **计算字段**: HerbCount (Service 计算), TotalPrice (Service 计算)

**FormulaDetailDto** (实现 IAuditable, 20+ 字段):
- Id, CreatedAt, UpdatedAt, CreatedBy, Status, IsEnabled (计算), Name, Effect, Indications,
  Description (DTO 独有), Usage, Property, IsShared, ValidationStatus, Source (DTO 独有),
  Remark, Contraindications (DTO 独有), Herbs (List\<FormulaHerbItemDto\>), HerbCount, TotalPrice,
  HerbNames (计算), Category (getter 有默认值 "验方")
- **DTO 独有字段**: Description, Source, Contraindications, IsEnabled, HerbNames

**FormulaInputDto** (实现 IEntityInputDto, 14 字段):
- Id (Guid?), Name, Effect, Description, Usage, Property, Category, IsShared,
  Instructions (DTO 独有), Indications, Contraindications, Preparation (DTO 独有),
  Remark, Herbs (List\<FormulaHerbItemInputDto\>)

**FormulaHerbItemDto** (15 字段):
- Id, HerbId, OriginalHerbName, IsValidated, HerbName, Dosage, Unit, Preparation,
  Processing (别名→ProcessingMethod), Usage, Price, UnitPrice (计算→Price),
  ProcessingMethod, SpecialInstructions (DTO 独有), SortOrder (DTO 独有),
  DecocteMethod, Herb (HerbDetailDto? 导航属性)

**关键映射断裂点**: Entity `ProcessingMethod` → DTO 双字段 `Preparation` + `ProcessingMethod`（CatalogDtoMapper.cs:114-115）

**FormulaDetailModel** (继承 ValidatableModelBase, 20+ 字段):
- Herbs 直接持有 `ObservableCollection<FormulaHerbItemDto>` (DTO, 非 Model)
- UI 专有: CreateNew(), Clone()

#### 2.2.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→DTO | Mapperly+手写混合 | CatalogDtoMapper.cs:71-99 | ListDto Mapperly; DetailDto **手写** (Category 默认值, Herbs 嵌套映射) |
| FormulaHerbItem→Dto | **手写** | CatalogDtoMapper.cs:106-119 | ProcessingMethod→双字段 Preparation+ProcessingMethod |
| DTO→Model | FormulaDetailModelMapper (Mapperly+手写) | FormulaDetailModelMapper.cs:56-76 | ToItemCore(Mapperly) + 手写 Herbs 映射 |
| Model→DTO | 手写 | FormulaDetailModelMapper.cs:105-113 | Herbs 直接 List 赋值 |
| Model→InputDto | 手写 | FormulaDetailModelMapper.cs:145-163 | Herbs 转 FormulaHerbItemInputDto |
| EditContext→InputDto | 手写 (FormulaHerbItemViewModel) | FormulaHerbItemViewModel.cs:41-52 | Remark→ProcessingMethod 映射 |

#### 2.2.3 API 合约

- **Refit 接口**: IFormulaApi.cs, 14 个端点, `ApiResponse<T>` 包装
- **统一接口**: IApiClientFormulas : IEntityApiSegment\<FormulaListDto, FormulaDetailDto, FormulaInputDto\>

#### 2.2.4 DB 配置

- **DbContext**: CatalogDbContext + AppDbContext
- **表名**: `Formulas`, `FormulaHerbItems` (PascalCase)
- **外键**: FormulaHerbItem→Formula (Cascade), FormulaHerbItem→Herb (**Restrict**)
- **级联**: Formula 删除 → 级联删除 FormulaHerbItem; Herb 被引用时阻止删除

---

### 2.3 Patient（患者）域

#### 2.3.1 数据形状

**Entity `Patient`** (继承 BaseEntity, 表 `Patients`):
- Id, Name (Required, 100), PinYinCode (50), Gender (Gender 枚举), BirthDate (DateTime?),
  IdNumber (50, [SensitiveData(IdentityInfo)]), PhoneNumber (20, [SensitiveData(ContactInfo)]),
  Status (CommonStatus→int), Age ([NotMapped] 计算属性)

**PatientListDto** (8 字段):
- Id, Name, Gender, Age (Service 计算), PhoneNumber, PinYinCode, Status, CreatedAt

**PatientDetailDto** (实现 IAuditable, 12 字段):
- Id, Name, Gender, BirthDate, Age, IdNumber, PhoneNumber, PinYinCode, Status,
  CreatedAt, UpdatedAt, CreatedBy

**PatientInputDto** (实现 IEntityInputDto, 7 字段):
- Id (Guid?), Name, PinYinCode, Gender, BirthDate, IdNumber, PhoneNumber
- **缺失**: Status, Age (InputDto 不含)

**PatientDetailModel** (继承 ValidatableModelBase, 12 字段):
- 与 PatientDetailDto 对齐, Age 自身计算 (与 Entity 公式一致)
- PinYinCode 由 Name setter 自动触发

#### 2.3.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→DTO | Mapperly 编译时生成 | PatientMapper.cs:31,36 | ToListDto/ToDetailDto, 纯属性复制 |
| InputDto→Entity | **手写** | PatientMapper.cs:19-26 | 调用 Patient.Create() 领域工厂 |
| DTO→EditContext | **手写内联** | PatientEditorViewModel.cs:40-57 | 逐字段赋值 |
| EditContext→InputDto | **手写内联** | PatientEditorViewModel.cs:62-74 | 含 Trim 清洗 |
| 保存回写 Model | **手写内联** | PatientMasterDetailViewModel.cs:182-189 | 逐字段同步 |

**Age 计算**: Entity 用 `[NotMapped]` getter, Desktop Model 用相同公式, DTO 由 Entity getter 自动映射

**敏感字段**: `[SensitiveData]` 标记仅元数据, **无运行时脱敏过滤**, 原值全链路传递

#### 2.3.3 API 合约

- **Refit 接口**: IPatientApi.cs, 8 个端点, `ApiResponse<T>` 包装
- **统一接口**: IApiClientPatients : IEntityApiSegment\<PatientListDto, PatientDetailDto, PatientInputDto\>

#### 2.3.4 DB 配置

- **DbContext**: PatientsDbContext + AppDbContext
- **表名**: `Patients` (PascalCase)
- **Fluent API**: PatientConfiguration 继承 BaseEntityConfiguration\<Patient\>, Status→int
- **软删除**: HasQueryFilter(e => !e.IsDeleted)

---

### 2.4 User（用户/账号）域

#### 2.4.1 数据形状

**Entity `ApplicationUser`** (继承 IdentityUser\<Guid\>, 实现 IAuditableEntity+ISoftDeletable, 表 `Users`):
- Identity 继承字段: Id, UserName, Email, PhoneNumber, PasswordHash, AccessFailedCount, ...
- 业务字段: RealName (100), PinYinCode (50), Role (UserRole→int), IsSysAdmin (bool),
  Status (CommonStatus→int), MustChangeOnNextLogin, LastLoginAt, RegistrationFee (decimal(10,2)),
  Remark (500)
- 审计字段: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy (自行实现, 非 BaseEntity 继承)
- 并发: RowVersion, IsDeleted

**UserListDto** (10 字段):
- Id, UserName, RealName, PhoneNumber, Role, Status, IsEnabled (计算), LastLoginTime,
  RegistrationFee, CreatedAt
- **命名差异**: Entity `LastLoginAt` → DTO `LastLoginTime`

**UserDetailDto** (13 字段):
- Id, UserName, RealName, Role, Status, IsEnabled (计算), PhoneNumber, Email, PinYinCode,
  LastLoginTime, FailedLoginCount, CreatedAt, UpdatedAt, RegistrationFee, Remark
- **命名差异**: Entity `AccessFailedCount` → DTO `FailedLoginCount`

**UserInputDto** (实现 IEntityInputDto, 10 字段):
- Id (Guid?), UserName, Password, ConfirmPassword, RealName, PinYinCode, PhoneNumber,
  Email, Role, RegistrationFee, Remark
- **InputDto 独有**: Password, ConfirmPassword (创建时可选, 更新时禁止)

**UserDetailModel** (继承 ValidatableModelBase, 13 字段):
- Id, UserName, RealName, PinYinCode, PhoneNumber, Email, Role, Status, LastLoginTime,
  CreatedAt, UpdatedAt, Remark, RegistrationFee
- **无 PasswordHash/Password 字段**
- PinYinCode 由 RealName setter 自动触发

#### 2.4.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→ListDto | **手写** | IdentityMapper.cs:27-38 | 9 字段手动映射, UserName ?? string.Empty 防空 |
| Entity→DetailDto | **手写** | IdentityMapper.cs:44-59 | 12 字段手动映射 |
| Entity→BasicDto | Mapperly | IdentityMapper.cs:68 | MapProperty: LastLoginAt→LastLoginTime, AccessFailedCount→FailedLoginCount |
| Entity→CredentialDto | Mapperly | IdentityMapper.cs:75 | 含 PasswordHash, 仅供登录验证 |
| DTO→Model | **手写内联** | UserMasterDetailViewModel.cs:211-226 | 逐字段赋值 |
| DTO→EditContext | **手写内联** | UserMasterDetailViewModel.cs:194-209 | 通过 InitializeFromDto |
| EditContext→InputDto | **手写内联** | UserEditorViewModel.cs:68-82 | GetUserInput() |
| 保存回写 | **手写内联** | UserMasterDetailViewModel.cs:267-278 | 逐字段同步 |

**密码处理**: PasswordHash 仅存在于 UserCredentialDto (内部登录验证), DTO/Model 全链路不暴露

#### 2.4.3 API 合约

- **Refit 接口**: IUserApi.cs (12 端点) + IAuthApi.cs (6 端点), `ApiResponse<T>` 包装
- **统一接口**: IApiClientIdentity : IEntityApiSegment\<UserListDto, UserDetailDto, UserInputDto\>
- **认证特殊**: Login 返回 LoginResponse (含 JWT + 用户信息)

#### 2.4.4 DB 配置

- **DbContext**: IdentityDbContext (继承 IdentityDbContext\<ApplicationUser\>) + AppDbContext
- **表名**: `Users` (非标准 AspNetUsers, 通过 ToTable("Users") 覆盖)
- **Fluent API**: Status/Role→int, RegistrationFee→decimal(10,2), 软删除查询过滤器
- **索引**: UserName (Unique), PinYinCode, Role, Status

---

### 2.5 Registration（挂号）域

#### 2.5.1 数据形状

**Entity `Registration`** (继承 BaseEntity, 表 `Registrations`):
- Id, PatientId (Required), PatientName (100, 冗余), DoctorId (Required), DoctorName (100, 冗余),
  MedicalCaseId (Guid?, 接诊后填入), Source (RegistrationSource 枚举), Status (RegistrationStatus 枚举),
  QueueNumber (int, 当日序号), RegistrationFee (decimal(10,2)), Remark (500)
- 领域方法: StartVisit(), Complete(), Cancel(), AssignMedicalCase(), RevertToWaiting(), SoftDelete()

**RegistrationListDto** (11 字段):
- Id, PatientId, PatientName, DoctorId, DoctorName, MedicalCaseId, QueueNumber,
  RegistrationFee, Source, Status, CreatedAt, HasMedicalCase (计算)

**RegistrationDetailDto** (实现 IAuditable, 13 字段):
- Id, PatientId, PatientName, DoctorId, DoctorName, MedicalCaseId, Source, QueueNumber,
  RegistrationFee, Status, Remark, CreatedAt, UpdatedAt, CreatedBy

**RegistrationInputDto** (7 字段):
- PatientId, PatientName, DoctorId, DoctorName, Source, RegistrationFee, Remark

#### 2.5.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→DTO | **Mapperly 编译时生成** | RegistrationMapper.cs:16,21 | ToListDto/ToDetailDto, 纯 partial 方法 |
| DTO→Model | **无 (无 Model 层)** | — | ViewModel 直接持有 Shared DTO |
| DTO→InputDto | **无转换** | RegistrationCreateDialogViewModel.cs:96-105 | 直接构造 RegistrationInputDto |

**特殊**: Server 端无传统 Service 层, 采用 **MediatR CQRS** (Command/Query Handler 直接注入 Mapper+Repository)

#### 2.5.3 API 合约

- **Refit 接口**: IRegistrationApi.cs, 6 个端点 (Create/GetById/GetList/GetQueue/StartVisit/Cancel)
- **统一接口**: IApiClientRegistrations (无 IEntityApiSegment 继承, 自定义方法)

#### 2.5.4 DB 配置

- **DbContext**: RegistrationDbContext + AppDbContext
- **表名**: `Registrations` (PascalCase)
- **索引**: PatientId, DoctorId, Status, MedicalCaseId
- **枚举**: Source/Status → int

---

### 2.6 MedicalCase（病历）域

#### 2.6.1 数据形状

**Entity `MedicalCase`** (继承 BaseEntity, 聚合根, 表 `MedicalCases`):
- Id, PatientId (Required), PatientName (50, 冗余), UserId (Required, 医生ID), DoctorName (50, 冗余),
  CaseNumber (50), CaseStatus (MedicalCaseStatus→int), NeedsPrescription (bool?), CompletedAt,
  IsPrinted, PrintCount, LastPrintedAt, PrintVersion
- 导航属性: PrintLogs (ICollection), Consultation (1:1), Prescription (1:0..1)
- 计算属性: IsLocked, IsActive, IsCompleted

**MedicalCaseListDto** (14 字段):
- Id, CaseNumber, PatientId, PatientName, PatientGender, PatientAge, UserId, DoctorName,
  CompletedAt, CaseStatus, Diagnosis, NeedsPrescription, HasConsultation, HasPrescription, CreatedAt

**MedicalCaseDetailDto** (20+ 字段):
- Id, CreatedAt, UpdatedAt, CreatedBy, CaseNumber, PatientId, PatientName, PatientGender,
  PatientAge, UserId, DoctorName, ConsultationId, PrescriptionId, CompletedAt, NeedsPrescription,
  CaseStatus, Diagnosis, HasConsultation (计算), HasPrescription (计算), IsLocked (计算),
  PresentIllness, Consultation (嵌套 ConsultationDetailDto), Prescription (嵌套 PrescriptionDetailDto)

**MedicalCaseInputDto** (聚合保存, 7 字段):
- Id (Guid?), PatientId, RegistrationId (可选), UserId, EditReason,
  Consultation (嵌套 ConsultationInputDto?), Prescription (嵌套 PrescriptionInputDto?), NeedsPrescription

#### 2.6.2 转换方式

| 环节 | 方式 | 文件:行号 | 详情 |
|------|------|-----------|------|
| Entity→ListDto | Mapperly + Service 手动补字段 | MedicalCaseMapper.cs:35 + MedicalCaseQueryService.cs:74-79 | Mapperly 忽略 6 个计算属性, Service 手动补充 HasConsultation/HasPrescription |
| Entity→DetailDto | **Mapperly+手写丰富** | MedicalCaseMapper.cs:111-169 | MapToMedicalCaseDetailDto: 先 Mapperly 基础映射, 再手动补嵌套 Consultation/Prescription + 计算字段 |
| Consultation→Dto | Mapperly | MedicalCaseMapper.cs:75 | MapProperty: Id→MedicalCaseId |
| Prescription→Dto | Mapperly + 手动丰富 | MedicalCaseMapper.cs:89,153-169 | 补 Items + 计算字段 (SingleDosePrice/TotalPrice/TotalWeight) |
| DTO→Model | MedicalCaseDetailModelMapper (Mapperly+手写) | MedicalCaseDetailModelMapper.cs:81 | ToItemCore(Mapperly) + 手动提取嵌套字段 |
| Model→InputDto | 手写 | MedicalCaseDetailModelMapper.cs:157 | ToInputDto |
| DTO→InputDto | 手写 | MedicalCaseDetailModelMapper.cs:175 | DTO→InputDto 直接转换 |

#### 2.6.3 API 合约

- **Refit 接口**: IMedicalCaseApi.cs, 19+ 端点 (CRUD + 状态流转 + 聚合保存 + 审计日志 + 打印)
- **统一接口**: IApiClientMedicalCases (187 行, 方法签名与 IMedicalCaseApi 一一对应)

#### 2.6.4 DB 配置

- **DbContext**: MedicalCaseDbContext + AppDbContext
- **表名**: MedicalCases, Consultations, Prescriptions, PrescriptionItems, MedicalCasePrintLogs, MedicalCaseAuditLogs
- **关键配置**:
  - Consultation: 共享主键 1:1 with MedicalCase (Cascade 删除)
  - Prescription: 唯一索引 UX_Prescriptions_MedicalCaseId, FK→MedicalCase (Cascade)
  - MedicalCase: 唯一索引 UX_MedicalCases_Patient_ActiveOnly (CaseStatus=1 AND IsDeleted=0)
  - FK→Patient (Restrict), FK→ApplicationUser (Restrict)

---

## 三、发现的问题清单

### P0 (架构层面事实偏差)

| # | 域 | 问题 | 证据 |
|---|---|---|---|
| P0-1 | 全局 | **模块 AGENTS.md 与实际架构矛盾**: 模块文档声称"Each module has its own DbContext (per-module data isolation) — never use the shared AppDbContext for module data"，但 ADR-0017 方案 A 明确"同库不同 DbContext, 物理表由 AppDbContext 单一迁移链管理"，模块 DbContext 只做逻辑隔离 | CatalogDbContext.cs 注释:13, MedicalCaseDbContext.cs 注释:12, PatientsDbContext.cs 注释:9, RegistrationDbContext.cs 注释:9, IdentityDbContext.cs 注释:14 |
| P0-2 | Registration | **Desktop 端无 Model 层**: ViewModel 直接持有 Shared DTO (RegistrationListDto/RegistrationInputDto)，跳过了 Desktop Model 层，违反项目其他 5 域的 MasterDetailViewModelBase\<TListDto, TDetailModel\> 模式 | RegistrationListViewModel.cs:44-48, Desktop/Modules 下无 Registration Model 文件 |
| P0-3 | Formula | **FormulaHerbItem 不继承 BaseEntity**: 无 IsDeleted/审计字段/RowVersion，子表用级联删除而非软删除，与项目其他所有业务实体的 BaseEntity 模式不一致 | FormulaHerbItem.cs:15-16 (无 BaseEntity 继承) |
| P0-4 | User | **ApplicationUser 独立实现审计接口**: 不继承 BaseEntity (从 IdentityUser\<Guid\> 来)，自行实现 IAuditableEntity+ISoftDeletable，审计字段代码与 BaseEntity 重复但不共享 | ApplicationUser.cs:14, BaseEntity.cs:10 |

### P1 (映射一致性/数据完整性)

| # | 域 | 问题 | 证据 |
|---|---|---|---|
| P1-1 | Formula | **ProcessingMethod 映射断裂**: Entity `ProcessingMethod` → DTO 双字段 `Preparation` + `ProcessingMethod` (同一实体字段映射到两个 DTO 字段)；Desktop 层 `FormulaHerbItemViewModel.Remark` → DTO `ProcessingMethod`，语义链断裂 | CatalogDtoMapper.cs:114-115, FormulaHerbItemViewModel.cs:49 |
| P1-2 | User | **LastLoginAt/LastLoginTime 命名不一致**: Entity `LastLoginAt` → DTO `LastLoginTime`，跨层命名不统一，需 MapProperty 桥接 | IdentityMapper.cs:68 [MapProperty] |
| P1-3 | User | **AccessFailedCount/FailedLoginCount 命名不一致**: Entity `AccessFailedCount` (IdentityUser) → DTO `FailedLoginCount`，需 MapProperty 桥接 | IdentityMapper.cs:68 [MapProperty] |
| P1-4 | Formula | **Indication/Indications 命名不一致**: Entity `Indication` (单数) → DTO `Indications` (复数)，需 MapProperty | CatalogDtoMapper.cs:71 [MapProperty] |
| P1-5 | Patient | **敏感字段无运行时脱敏**: `[SensitiveData]` 标记仅元数据，IdNumber/PhoneNumber 原值全链路传递 (Entity→DTO→Model→ViewModel→XAML)，无任何层做脱敏 | PatientModel.cs:47,55, PatientMapper.cs 无过滤逻辑 |
| P1-6 | Formula | **HerbCount/TotalPrice 计算位置不统一**: Entity 有 `HerbCount` (getter 计算), DTO 的 `HerbCount`/`TotalPrice` 由 Service 计算赋值, Desktop Model 的 `HerbCount` 也是计算属性，三层各自计算 | FormulaModel.cs:85, CatalogDtoMapper.cs:97 |
| P1-7 | MedicalCase | **Mapperly 忽略计算属性后手动补充**: Mapper 忽略 6 个字段 (CaseNumber/PatientGender/PatientAge/Diagnosis/HasConsultation/HasPrescription)，Service 手动补充，导致 Mapper 和 Service 耦合 | MedicalCaseMapper.cs:35, MedicalCaseQueryService.cs:74-79 |

### P2 (桌面端映射风格不统一)

| # | 域 | 问题 | 证据 |
|---|---|---|---|
| P2-1 | Herb/Patient/User | **Desktop 端无独立 Mapper**: DTO↔Model 映射全在 ViewModel 中手写内联，分散在 MasterDetailViewModel 和 EditorViewModel 中，与 Formula/MedicalCase 有独立 Mapper 的模式不一致 | HerbMasterDetailViewModel.cs:114-130, PatientEditorViewModel.cs:40-57, UserMasterDetailViewModel.cs:211-226 |
| P2-2 | Formula | **Desktop Model 直接持有 DTO 作为子项**: FormulaDetailModel.Herbs 类型为 `ObservableCollection<FormulaHerbItemDto>` (DTO 直接暴露给 UI)，未封装为 Model | FormulaDetailModel.cs:134-138 |
| P2-3 | MedicalCase | **Desktop 有 4 个 Mapper 文件**: MedicalCaseDetailModelMapper + PrescriptionMapper + ConsultationMapper + MedicalCaseCloneMapper，是最复杂的映射结构，与其他域的简单模式差异大 | Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/ (4 文件) |

---

## 四、转换方式现状总结

### 4.1 Server 端 (Entity → DTO)

| 技术 | 使用域 | 文件 | 特点 |
|------|--------|------|------|
| **Mapperly 编译时生成** (纯属性复制) | Herb (ListDto/DetailDto), Patient (ListDto/DetailDto), Registration (ListDto/DetailDto) | CatalogDtoMapper.cs, PatientMapper.cs, RegistrationMapper.cs | 纯 partial 方法，属性同名自动映射 |
| **Mapperly + 手写混合** | Formula (DetailDto 手写), User (ListDto/DetailDto 手写), MedicalCase (DetailDto 手动丰富) | CatalogDtoMapper.cs, IdentityMapper.cs, MedicalCaseMapper.cs | Mapperly 处理简单映射，手写处理嵌套/计算/默认值 |
| **InputDto→Entity 全部手写** | 所有域 | 各 Mapper 的 ToEntity 方法 | 调用领域工厂 (Herb.Create/Patient.Create 等)，Mapperly 无法表达校验+Trim |

### 4.2 Desktop 端 (DTO → Model)

| 技术 | 使用域 | 特点 |
|------|--------|------|
| **独立 Mapper (Mapperly+手写)** | Formula (FormulaDetailModelMapper), MedicalCase (4 个 Mapper) | 有明确的映射类，可测试 |
| **ViewModel 内联手写** | Herb, Patient, User | 映射逻辑分散在 MasterDetailViewModel 和 EditorViewModel 中 |
| **无 Model 层 (直接持有 DTO)** | Registration | ViewModel 直接绑定 Shared DTO |

### 4.3 DB 配置一致性

| 维度 | 状态 | 详情 |
|------|------|------|
| 表名约定 | **PascalCase 统一** | Herbs/Formulas/FormulaHerbItems/Patients/Users/Registrations/MedicalCases/Consultations/Prescriptions/PrescriptionItems |
| 列名约定 | **PascalCase 统一** (EF Core 默认) | 无显式 snake_case 映射 |
| 枚举存储 | **int 统一** | Status/Role/Gender/Source/CaseStatus/FormulaType/ValidationStatus/DecocteMethod 全部 HasConversion\<int\>() |
| 软删除 | **统一** (除 FormulaHerbItem) | HasQueryFilter(e => !e.IsDeleted), FormulaHerbItem 用级联删除 |
| 审计字段 | **统一** (除 FormulaHerbItem) | BaseEntity: CreatedAt/UpdatedAt/CreatedBy/UpdatedBy/RowVersion/IsDeleted |
| 并发控制 | **统一** | RowVersion (Timestamp) + 并发令牌 |
| 迁移链 | **单一链** | AppDbContext, 14 个迁移文件 (20260405→20260806) |
| 配置复用 | **统一** | 模块 DbContext 复用 Infrastructure 的 IEntityTypeConfiguration 类 |

---

*报告结束。本报告仅记录事实，不包含改进建议。*

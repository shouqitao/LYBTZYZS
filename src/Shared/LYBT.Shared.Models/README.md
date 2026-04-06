# LYBT.Shared.Models

> 共享数据模型库 | 111个.cs文件 | DTO契约+枚举+扩展方法

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的数据传输对象(DTO)、枚举和扩展方法

## 目录结构

```
LYBT.Shared.Models/
├── Common/                     # 通用模型(1文件)
│   └── Result.cs               # Result<T>/Result 统一返回值
├── DTOs/                       # 跨模块DTO(1文件)
│   └── Users/UserBasicDto.cs   # 跨模块用户基本信息
├── Contracts/                  # DTO契约定义(10模块，95文件)
│   ├── Auth/                   # 认证(12文件)
│   ├── Common/                 # 通用契约(16文件)
│   ├── Consultation/           # 诊断(2文件)
│   ├── Formula/                # 验方(11文件)
│   ├── Herbs/                  # 药材(8文件)
│   ├── MedicalCase/            # 医案(13文件)
│   ├── Patients/               # 患者(10文件)
│   ├── Prescriptions/          # 处方(4文件)
│   ├── Sync/                   # 数据同步(10文件)
│   └── Users/                  # 用户(9文件)
├── Enums/                      # 枚举定义(12文件)
└── Extensions/                 # 扩展方法(2文件)
```

## 核心组件

| 组件 | 说明 |
|------|------|
| ApiResponse<T> | 统一API响应格式(Success/Message/Data/Timestamp) |
| ServiceResult<T> | 服务层结果包装(IsSuccess/Data/ErrorMessage) |
| Result<T> | Service层统一返回值(支持ErrorCode) |
| PagedResult<T> | 分页结果模型(Items/TotalCount/TotalPages) |
| PagedQueryBaseDto | 分页查询基类(Keyword/PageIndex/PageSize/Sort) |

## DTO基类体系

| 基类 | 继承关系 | 说明 |
|------|----------|------|
| BaseDto | IIdentifiable<Guid> | 包含Id字段 |
| TimestampDto | BaseDto + IAuditable | 包含CreatedAt/UpdatedAt |
| StatusDto | TimestampDto + IStatusManageable | 包含Status字段 |
| CreateDtoBase | - | 创建操作基类(不含Id) |
| UpdateDtoBase | StatusDto | 更新操作基类(含Id) |

## 核心枚举

| 枚举 | 文件 | 说明 |
|------|------|------|
| UserRole | AuthEnums.cs | Receptionist/Doctor/Admin/SuperAdmin |
| LoginType | AuthEnums.cs | Password 认证类型 |
| CaseStatus | CaseStatus.cs | Suspended/Active/Completed |
| MedicalCaseStatus | MedicalCaseEnums.cs | Suspended/Active/Completed |
| CommonStatus | SystemEnums.cs | Disabled/Enabled |
| Gender | Gender.cs | Unknown/Male/Female |
| DecocteMethod | DecocteMethod.cs | 7种煎法 |
| FormulaType | FormulaType.cs | Classic/Experience |
| DuplicateStrategy | DuplicateStrategy.cs | Skip/Update/Error |
| PrintType | PrintType.cs | Prescription/Formula |
| ErrorCategory | ErrorEnums.cs | 12种错误分类 |
| PasswordStrength | SecurityEnums.cs | Weak~VeryStrong 5级 |

## 设计依据

- DTO集中于Shared.Models而非各模块内，确保Server/Desktop共享同一API契约
- 枚举与DTO同层，避免Desktop直接引用Server端Entities层
- DTO基类体系通过接口组合(IIdentifiable/IAuditable/IStatusManageable)实现按需继承
- 大部分业务DTO已扁平化设计，不再继承基类；批量操作DTO使用继承链

## 依赖关系

### 依赖
- 无(基础设施层，零依赖)

### 被依赖
- LYBT.Infrastructure (引用Entity和结果类型)
- LYBT.Module.* (所有Server模块)
- LYBT.WebAPI (引用所有DTO和ApiResponse)
- LYBT.Desktop.Contracts (引用所有DTO)
- LYBT.Desktop.* (所有Desktop模块)
- 所有测试项目

### NuGet包
- System.ComponentModel.Annotations (8.0.x)
- System.Text.Json (8.0.x)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 根据实际目录结构重写，修正文件计数 |
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | DTO三阶段优化完成 |

## 开发笔记

# LYBT.Shared.Models 代码知识

共享模型层 -- DTO、枚举、API 契约模型，被 Server 和 Desktop 双端引用。113 个 .cs 文件（不含 obj/bin）。

## 代码文件结构

```
Common/
└── Result.cs                # 统一返回值 Result<T>/Result -- Service 层成功/失败封装，含 ErrorCode 支持

DTOs/
└── Users/
    └── UserBasicDto.cs      # record UserBasicDto -- 跨模块用户基本信息 (供 IUserCrossModuleService)
                             # record UserCredentialDto : UserBasicDto -- 含 PasswordHash，仅密码验证用

Contracts/
├── Auth/
│   ├── AuthResult.cs        # AuthResult<T>/AuthResult -- 认证操作结果，含便捷工厂 (InvalidCredentials 等)
│   ├── AutoLoginRequest.cs  # 自动登录请求 -- UserName + AutoLoginToken + DeviceId
│   ├── ChangePasswordRequest.cs  # 修改密码请求 -- OldPassword + NewPassword
│   ├── ChangeSysAdminPassword.cs # sysadmin 密码修改
│   ├── LoginRequest.cs      # 用户登录请求 -- UserName/Password/RememberMe/DeviceId
│   ├── LoginResponse.cs     # 登录响应 -- Token/RefreshToken/UserDetailDto/AutoLoginToken/MustChangePassword
│   ├── LogoutRequest.cs     # 登出请求 -- UserName?/RefreshToken?/DeviceId?
│   ├── RefreshTokenRequest.cs # 刷新令牌请求
│   ├── SuperAdminLoginRequest.cs # 超管登录 -- 仅密码，用户名从配置读取（安全设计）
│   ├── TokenPair.cs         # JWT 令牌对 -- AccessToken/RefreshToken/过期时间/用户信息
│   ├── ValidateTokenRequest.cs  # Token 验证请求
│   └── ValidateTokenResponse.cs # Token 验证响应 -- IsValid/UserId/Username/Role/ExpiresAt
│
├── Common/
│   ├── ApiResponse.cs       # API 统一响应 ApiResponse<T> -- Success/Message/Data/Timestamp/RequestId
│   ├── BatchDeleteInputDto.cs # 批量删除输入 + BatchOperationResultDto (继承链基类) + BatchOperationFailureItem
│   ├── DtoBase.cs           # DTO 基类体系: IIdentifiable<T> / IAuditable / ICreatorTrackable / IStatusManageable
│   │                        #   BaseDto (Id) → TimestampDto (CreatedAt/UpdatedAt/CreatedBy) → StatusDto (Status)
│   ├── HandledError.cs      # 处理后错误信息 -- Category/Severity/UserMessage + 静态工厂方法
│   ├── HealthCheckResponse.cs # 健康检查响应 -- Status/Timestamp/Version/Environment
│   ├── HerbBasicDto.cs      # 药材基本信息 -- Id/Name/Pinyin/Category (跨模块查询用)
│   ├── ImportResultDto.cs   # 导入结果 : BatchOperationResultDto -- DuplicateCount/FileName/ImportTime
│   ├── ImportResultDtoT.cs  # 泛型导入结果 ImportResultDto<T> -- 含 ImportedData 列表
│   ├── ImportRowErrorDto.cs # 导入行错误 -- Row/Error/FieldName (Excel 行级错误)
│   ├── OperationResultDto.cs  # 操作结果基类 -- IsSuccess/Message/ErrorCode/OperationTime
│   ├── OperationResultDtoT.cs # 泛型操作结果 OperationResultDto<T> -- 含 Data
│   ├── PagedQueryBaseDto.cs # 分页查询基类 -- Keyword/PageIndex/PageSize/SortField/IsDescending + Skip 计算
│   ├── PagedResult.cs       # 分页结果 PagedResult<T> -- Items/TotalCount/CurrentPage/TotalPages + 导航属性
│   ├── PatientBasicDto.cs   # 患者基本信息 -- Id/Name/Gender/Phone/Status (跨模块查询用)
│   ├── ServiceResult.cs     # 服务层响应 ServiceResult<T>/ServiceResult -- IsSuccess/Data/ErrorMessage/Exception
│   └── ValidationResult.cs  # 业务验证结果 -- IsValid/ErrorMessage/RuleName/Details + 静态工厂方法
│
├── Consultation/
│   ├── ConsultationDetailDto.cs  # 诊疗详情 -- 基础标识 + 关联字段 + 四诊核心字段 (PresentIllness/Tongue/Pulse/TcmDiagnosis)
│   └── ConsultationInputDto.cs   # 诊疗输入 -- 4 个核心字段 (精简版)，排除展示字段
│
├── Formula/
│   ├── FormulaBatchImportInputDto.cs   # 验方批量导入请求 -- FormulaImportItemDto 列表 + FileName
│   ├── FormulaBatchImportResultDto.cs  # 验方导入结果 : ImportResultDto -- MatchedHerbsCount + 失败详情
│   ├── FormulaDetailDto.cs       # 验方详情 : ICreatorTrackable -- 全字段 + FormulaHerbItemDto 列表 + 验证状态
│   ├── FormulaHerbExportItemDto.cs # 验方药材导出项 -- HerbId/Name/Dosage/Unit/Preparation
│   ├── FormulaHerbImportItemDto.cs # 验方药材导入项 -- HerbName/Dosage/Unit
│   ├── FormulaHerbItemDto.cs     # 验方药材组成项 -- 支持延迟绑定 (HerbId 可空，OriginalHerbName)
│   ├── FormulaHerbItemInputDto.cs # 验方药材输入 -- HerbId?/HerbName/Dosage/Unit/DecocteMethod
│   ├── FormulaImportFailureDto.cs # 验方导入失败详情 -- RowIndex/FormulaName/ErrorMessage
│   ├── FormulaImportItemDto.cs   # 验方导入行项 -- Name/Effect/Usage + FormulaHerbImportItemDto 列表
│   ├── FormulaInputDto.cs        # 验方输入 -- Name/Effect/Description + FormulaHerbItemInputDto 列表
│   └── FormulaListDto.cs         # 验方列表 -- Id/Name/Effect/Type/ValidationStatus/Status/HerbCount
│
├── Herbs/
│   ├── HerbBatchCheckReferenceInputDto.cs  # 批量引用检查请求 -- HerbIds 列表
│   ├── HerbBatchImportInputDto.cs   # 药材批量导入请求 -- HerbInputDto 列表 + DuplicateStrategy
│   ├── HerbBatchImportResultDto.cs  # 药材导入结果 : ImportResultDto + HerbImportFailureDto 列表
│   ├── HerbDetailDto.cs            # 药材详情 : ICreatorTrackable -- 全字段 (Name/PinYinCode/Category/Properties/Price 等)
│   ├── HerbImportItemDto.cs        # 药材导入行项 -- Name/Origin/Spec/Unit/Price 等
│   ├── HerbInputDto.cs             # 药材输入 -- 统一创建/更新 (Id? 区分)
│   ├── HerbListDto.cs              # 药材列表 -- Id/Name/PinYinCode/Category/Origin/Price/Status
│   └── HerbReferenceCheckDto.cs    # 药材引用检查结果 -- HasReferences/ReferenceCount/CanDelete
│                                   # + PrescriptionReferenceDto 嵌套类 (处方引用详情)
│
├── MedicalCase/
│   ├── BatchDetailQueryDto.cs    # 批量详情查询 -- Ids 列表 (最多 50 个)
│   ├── MedicalCaseAuditLogDto.cs # 审计日志 -- OperatorId/OperationType/OldValue/NewValue
│   ├── MedicalCaseAuditLogPagedResultDto.cs # 审计日志分页结果
│   ├── MedicalCaseDetailDto.cs   # 医案详情 (聚合 DTO) -- 基础字段 + 打印管理 + ConsultationDetailDto + PrescriptionDetailDto
│   ├── MedicalCaseInputDto.cs    # 医案输入 -- Id?/PatientId/UserId/Remark + 嵌套 Consultation/Prescription
│   │                             # + CancelMedicalCaseRequestDto 嵌套类
│   ├── MedicalCaseListDto.cs     # 医案列表 -- CaseNumber/PatientName/PatientGender/DoctorName/CaseStatus/HasPrescription
│   ├── MedicalCasePermissionDto.cs # 权限详情 -- CanEdit/CanDelete/RequiresEditReason/DenialReason
│   ├── MedicalCaseQueryDto.cs    # 统一查询参数 -- QueryType/PatientId/DoctorId/Keyword/分页 + 排序/日期过滤
│   ├── MedicalCaseStatusInputDto.cs # 状态变更输入 -- Status + StatusChangeReason
│   ├── PendingMedicalCaseDto.cs  # 待看诊队列项 -- PatientId/PatientName/PhoneMasked/CaseStatus/MedicalCaseId?
│   ├── PrintCompletedRequest.cs  # 打印完成回写 -- PrintType/PrinterName
│   ├── PrintLogInputDto.cs       # 打印日志输入 -- PrintType/IsSuccess/PrinterName/ErrorMessage
│   └── SetPrescriptionFlagRequest.cs # 开处方标记 -- NeedsPrescription
│
├── Patients/
│   ├── BatchImportResultDto.cs   # 患者导入结果 : ImportResultDto + PatientImportFailureDto 列表
│   ├── ExportTemplateDto.cs      # 导出模板配置 -- IncludeSampleData/SampleRowCount
│   ├── PatientBatchCheckReferenceInputDto.cs # 批量引用检查请求
│   ├── PatientBatchImportInputDto.cs  # 患者批量导入请求 -- PatientInputDto 列表 + Strategy
│   ├── PatientDetailDto.cs       # 患者详情 : ICreatorTrackable -- Name/Gender/BirthDate/Age/IdNumber/Phone/Address 等
│   ├── PatientImportFailureDto.cs # 患者导入失败详情 -- OriginalRowNumber/FailureReason/SuggestedFix/DataSnapshot
│   ├── PatientImportItemDto.cs   # 患者导入行项 -- Name/GenderText/BirthDateText/IdCardNumber/Phone
│   ├── PatientInputDto.cs        # 患者输入 -- 统一创建/更新 (无 Age，由 Service 从 BirthDate 计算)
│   ├── PatientListDto.cs         # 患者列表 -- Id/Name/Gender/Age/PhoneNumber/Address/Status/CreatedAt
│   └── PatientReferenceCheckDto.cs # 患者引用检查结果 + MedicalCaseReferenceDto 嵌套类
│
├── Prescriptions/
│   ├── PrescriptionDetailDto.cs  # 处方详情 -- 全字段 + Items 列表 + 运行时警告 (DuplicateWarning/MissingDrugWarning)
│   ├── PrescriptionInputDto.cs   # 处方输入 -- NeedsPrescription/DosageCount/Usage/Advice/Items 列表
│   ├── PrescriptionItemDto.cs    # 处方项目 -- HerbId/HerbName/Dosage/UnitPrice/TotalPrice/DecocteMethod
│   └── PrescriptionItemInputDto.cs # 处方项输入 -- HerbId/HerbName/Unit/Dosage/UnitPrice/DecocteMethod
│
├── Sync/
│   ├── SyncCompareInputDto.cs    # 同步比对请求 -- EntityType + LocalEntityMetadata 列表
│   ├── SyncCompareResultDto.cs   # 同步比对结果 -- SyncDiffDto 列表 + ServerTotalCount
│   ├── SyncDeleteInputDto.cs     # 同步删除请求 -- EntityType + EntityIds
│   ├── SyncDeleteResultDto.cs    # 同步删除结果 -- Success/Rejected 列表 + SyncDeleteRejectedItem
│   ├── SyncDiffDto.cs            # 同步差异 -- EntityId/DiffType (LocalOnly/ServerOnly/Modified/Identical) + Checksum
│   │                             # + enum SyncDiffType 定义
│   ├── SyncDownloadInputDto.cs   # 同步下载请求 -- EntityType + EntityIds
│   ├── SyncDownloadResultDto.cs  # 同步下载结果 -- JsonElement 实体列表
│   ├── SyncMetadataDto.cs        # 同步元数据 -- EntityId/Checksum(SHA256)/LastModifiedAt/IsDeleted
│   ├── SyncUploadInputDto.cs     # 同步上传请求 -- EntityType + JsonElement 列表 + OverwriteConflicts
│   └── SyncUploadResultDto.cs    # 同步上传结果 -- SyncUploadItemResult 列表 (Success/ErrorMessage/IsConflict)
│
└── Users/
    ├── ChangePasswordDto.cs      # 修改密码 -- UserId/OldPassword/NewPassword/ConfirmPassword
    ├── ChangeProfileDto.cs       # 修改个人资料 -- RealName/PhoneNumber (MVP 精简版)
    ├── ResetPasswordRequestDto.cs # 管理员重置密码请求 -- MustChangeOnNextLogin (密码使用配置默认值)
    ├── ResetPasswordResponseDto.cs # 重置密码响应 -- Success/TemporaryPassword
    ├── UserBatchImportInputDto.cs  # 用户批量导入请求 -- UserInputDto 列表 + Strategy
    ├── UserBatchImportResultDto.cs # 用户导入结果 : ImportResultDto + UserImportFailureDto 嵌套类
    ├── UserDetailDto.cs           # 用户详情 -- UserName/RealName/Role/Status/PhoneNumber/LastLoginTime 等
    ├── UserInputDto.cs            # 用户输入 -- 统一创建/更新 (Id? 区分)，密码可选
    └── UserListDto.cs             # 用户列表 -- Id/UserName/RealName/PhoneNumber/Role/Status/IsEnabled

Enums/
├── AuthEnums.cs             # LoginType (Password), AuthSessionStatus (Active/Expired/LoggedOut/Locked),
│                            # UserRole (Receptionist=0/Doctor=1/Admin=10/SuperAdmin=100)
├── CaseStatus.cs            # CaseStatus -- MedicalCaseStatus 的简化别名 (Suspended/Active/Completed)
├── DecocteMethod.cs         # 煎法: Default/PreDecoct/PostAdd/MeltIn/TakeWithWater/WrapDecoct/SeparateDecoct
├── DuplicateStrategy.cs     # 批量导入重复策略: Skip/Update/Error
├── ErrorEnums.cs            # ErrorCategory (General~Unknown 共 12 项), ErrorSeverity (Info~Fatal 共 5 级)
├── FormulaType.cs           # 方剂类型: Classic/Experience
├── FormulaValidationStatus.cs # 验方验证状态: Draft/Validated (支持延迟绑定工作流)
├── Gender.cs                # 性别: Unknown/Male/Female
├── MedicalCaseEnums.cs      # MedicalCaseStatus (Suspended/Active/Completed), AuditOperationType (Create~Cancel),
│                            # MedicalCaseQueryType (All/ByPatient/Pending/Unfinished/Recent)
├── PrintType.cs             # 打印类型: Prescription/Formula
├── SecurityEnums.cs         # PasswordStrength (Weak~VeryStrong 共 5 级)
└── SystemEnums.cs           # CommonStatus (Disabled/Enabled)

Extensions/
├── DtoConversionExtensions.cs # DetailDto → InputDto 转换: MedicalCase/Consultation/Prescription
└── EnumExtensions.cs          # 枚举扩展: GetDescription (带缓存)/GetAllDescriptions/GetEnumByDescription/
                               # IsValidEnumValue/GetAllValues/ToKeyValueList
```

## 含方法的核心类型

### Common/Result.cs
**Result<T>** / **Result** | Service 层统一返回值模式

| 方法 | 说明 |
|------|------|
| Success(T data) | 创建成功结果 |
| Failure(string) | 单个错误信息的失败结果 |
| Failure(List<string>) | 多个错误信息的失败结果 |
| Failure(ErrorCode, string?) | 带错误码的失败结果 |
| FromException(Exception, string?) | 从异常创建失败结果 |

### Contracts/Auth/AuthResult.cs
**AuthResult<T>** / **AuthResult** | 认证操作结果

| 方法 | 说明 |
|------|------|
| Success(T data) | 认证成功 |
| Failure(ErrorCode, string?) | 带错误码的认证失败 |
| InvalidCredentials() | 凭据无效 |
| UserNotFound() | 用户不存在 |
| UserDisabled() | 用户已禁用 |
| TokenRevoked() | Token 已撤销 |
| RefreshTokenExpired() | RefreshToken 已过期 |
| SessionExpired() | 会话已过期 |

### Contracts/Common/ApiResponse.cs
**ApiResponse<T>** / **ApiResponse** | 统一 API 响应格式

| 方法 | 说明 |
|------|------|
| CreateSuccess(T?, string) | 创建成功响应 |
| CreateFail(string, object?) | 创建失败响应 |

### Contracts/Common/ServiceResult.cs
**ServiceResult<T>** / **ServiceResult** | 服务层响应结果

| 方法 | 说明 |
|------|------|
| Success(T) | 成功 |
| Success(T, string) | 带消息的成功 |
| Failure(string, Exception?) | 失败 |

### Contracts/Common/ValidationResult.cs
**ValidationResult** | 业务规则验证结果

| 方法 | 说明 |
|------|------|
| Success() | 验证成功 |
| Success(string, params (string, object)[]) | 带详情的验证成功 |
| Failure(string, string?) | 验证失败 |
| Failure(string, string, params (string, object)[]) | 带详情的验证失败 |
| WithDetail(string, object) | 链式添加详情 |

### Contracts/Common/HandledError.cs
**HandledError** | 结构化错误信息

| 方法 | 说明 |
|------|------|
| NetworkError(string, Exception?) | 网络错误 (CanRetry=true) |
| BusinessError(string, Exception?) | 业务逻辑错误 |
| ValidationError(string, Exception?) | 验证错误 |
| FatalError(string, Exception?) | 致命错误 (RequiresUserAcknowledgment=true) |

### Extensions/EnumExtensions.cs
**EnumExtensions** | 枚举工具方法 (静态类)

| 方法 | 说明 |
|------|------|
| GetDescription(Enum) | 获取 [Description] 属性值 (ConcurrentDictionary 缓存) |
| GetAllDescriptions<T>() | 批量获取枚举值与描述的字典 |
| GetEnumByDescription<T>(string) | 根据描述反查枚举值 |
| IsValidEnumValue<T>(T) | 检查枚举值是否有效 |
| ToKeyValueList<T>() | 转为 int-string 键值对列表 (下拉框用) |

### Extensions/DtoConversionExtensions.cs
**DtoConversionExtensions** | DTO 转换 (静态类)

| 方法 | 说明 |
|------|------|
| MedicalCaseDetailDto.ToInputDto() | DetailDto 转 InputDto (含嵌套 Consultation/Prescription) |
| ConsultationDetailDto.ToInputDto() | 诊疗 DetailDto 转 InputDto |
| PrescriptionDetailDto.ToPrescriptionInputDto() | 处方 DetailDto 转 InputDto (含 Items 映射) |

## DTO 基类继承体系

```
IIdentifiable<Guid>
└── BaseDto (Id)
    └── TimestampDto : IAuditable, ICreatorTrackable (+ CreatedAt/UpdatedAt/CreatedBy)
        └── StatusDto : IStatusManageable (+ Status/IsEnabled 计算属性)
```

注意: 大部分业务 DTO (Detail/List/Input) 已扁平化设计，不再继承基类，直接声明所有字段。
仅批量操作结果 DTO 使用继承链: OperationResultDto → BatchOperationResultDto → ImportResultDto → 模块特定 ResultDto。

## DTO 命名约定

| 后缀 | 用途 | 示例 |
|------|------|------|
| DetailDto | 详情查询/展示 (全字段) | PatientDetailDto, HerbDetailDto |
| ListDto | 列表查询 (最小字段集) | PatientListDto, FormulaListDto |
| InputDto | 统一创建/更新输入 (Id?区分) | PatientInputDto, HerbInputDto |
| ImportItemDto | 批量导入单行数据 | PatientImportItemDto |
| ExportItemDto | 批量导出单行数据 | PatientExportItemDto |
| ImportFailureDto | 导入失败详情 | PatientImportFailureDto |
| BatchImportInputDto | 批量导入请求 | HerbBatchImportInputDto |
| BatchImportResultDto | 批量导入结果 | HerbBatchImportResultDto |
| ReferenceCheckDto | 删除前引用检查结果 | HerbReferenceCheckDto |
| BasicDto | 跨模块最小字段集 | HerbBasicDto, PatientBasicDto |

## 聚合 DTO 嵌套关系

```
MedicalCaseDetailDto (响应)
├── ConsultationDetailDto?     # 诊疗详情
└── PrescriptionDetailDto?     # 处方详情
    └── List<PrescriptionItemDto>  # 处方项目列表

MedicalCaseInputDto (请求)
├── ConsultationInputDto?      # 诊疗输入
└── PrescriptionInputDto?      # 处方输入
    └── List<PrescriptionItemInputDto>  # 处方项输入列表

FormulaDetailDto (响应)
└── List<FormulaHerbItemDto>   # 验方药材组成列表

FormulaInputDto (请求)
└── List<FormulaHerbItemInputDto>  # 验方药材输入列表
```

## 死代码清理记录

| 文件 | 类型 | 状态 | 说明 |
|------|------|------|------|
| Contracts/Common/SharedCommon.cs | 静态类 + 嵌套 HandledError | [已清理] 2026-03-01 | 文件已删除，与独立的 HandledError.cs 功能重复 |
| Contracts/Common/ErrorContext.cs | DTO | [已清理] 2026-03-01 | 文件已删除，仅被 SharedCommon.cs 引用 |
| Contracts/Common/ServiceResultT.cs | (空文件) | [已清理] 2026-03-01 | 文件已删除，ServiceResult<T> 已在 ServiceResult.cs 中定义 |
| Common/EnumItem.cs | 泛型类 | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Common/NullableEnumItem.cs | 泛型类 | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Contracts/Consultation/ConsultationListDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，无代码使用 |
| Contracts/Prescriptions/PrescriptionListDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Contracts/Prescriptions/PrescriptionSearchResultDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，0 代码引用 |
| Contracts/Patients/PatientExportItemDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Contracts/Formula/FormulaExportItemDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Contracts/Herbs/HerbExportItemDto.cs | DTO | [已清理] 2026-03-01 | 文件已删除，0 外部引用 |
| Core/BaseAuthSession.cs | 模型 | [已清理] 2026-03-01 | 文件已删除，Server 端未继承此类 |
| Enums/RecordEnums.cs (ConsultationStatus) | 枚举文件 | [已清理] 2026-03-01 | 文件已删除，Consultation 不使用独立状态管理 |
| Enums/ValidationEnums.cs (BusinessOperation) | 枚举文件 | [已清理] 2026-03-01 | 文件已删除，BusinessOperation 枚举随文件移除 |
| Enums/SystemEnums.cs (OperationResult) | 枚举成员 | [已清理] 2026-03-01 | OperationResult 枚举从 SystemEnums.cs 中移除，CommonStatus 保留 |
| Enums/MedicalCaseEnums.cs (MedicalCaseUpdateMode) | 枚举成员 | [已清理] 2026-03-01 | MedicalCaseUpdateMode 枚举从 MedicalCaseEnums.cs 中移除，其余枚举保留 |

## 设计分析

### 结果类型职责划分

项目存在多种结果封装类型，各有分工:

| 类型 | 层级 | 用途 |
|------|------|------|
| Result<T> / Result | Service 层 (内部) | 业务逻辑成功/失败，支持 ErrorCode |
| AuthResult<T> | Auth 模块 | 认证专用结果，含便捷工厂方法 |
| ServiceResult<T> | Service 层 (内部) | 带 Exception 的服务结果 |
| ApiResponse<T> | API 层 (外部) | HTTP 响应封装，含 Timestamp/RequestId |
| OperationResultDto | 批量操作 | 批量导入/删除的操作结果基类 |
| ValidationResult | 验证层 | 业务规则验证结果 |

Result<T> 与 ServiceResult<T> 存在功能重叠，前者侧重 ErrorCode 支持，后者侧重 Exception 传递。

### 批量操作 DTO 继承链

```
OperationResultDto (IsSuccess/Message/ErrorCode)
└── BatchOperationResultDto (TotalCount/SuccessCount/FailureCount/SuccessfulIds/FailedIds)
    └── ImportResultDto (DuplicateCount/ImportBatchId/FileName)
        ├── PatientBatchImportResultDto + PatientImportFailureDto
        ├── HerbBatchImportResultDto + HerbImportFailureDto
        ├── FormulaBatchImportResultDto + FormulaImportFailureDto
        └── UserBatchImportResultDto + UserImportFailureDto
```

### 同步模块设计

Sync 契约实现基于 Checksum(SHA256) 的差异比对模式:
1. Compare: 本地发送 LocalEntityMetadata 列表 → 服务器返回 SyncDiffDto 列表
2. Download: 根据差异下载 ServerOnly/Modified 实体 (JsonElement 格式)
3. Upload: 上传 LocalOnly 实体，支持冲突覆盖
4. Delete: 同步删除，被引用的实体返回 Rejected

### 已知陷阱

- MedicalCaseDetailDto.HasPrescription 是计算属性 (依赖 PrescriptionId.HasValue)，Mapper 必须显式设置 PrescriptionId
- PrescriptionInputDto.NeedsPrescription 默认为 true，MedicalCaseInputDto.NeedsPrescription 默认为 null (可空)，语义不同
- PatientInputDto 不含 Age 字段，Age 由 Service 从 BirthDate 计算，前端不应直接传递
- FormulaHerbItemDto.HerbId 可空，支持延迟绑定（老系统导入时先存 OriginalHerbName，稍后绑定药材库）
- PagedResult<T>.Data 是 Items 的 [JsonIgnore] 别名，序列化时只输出 items 字段

---

最后更新: 2026-03-01
文档版本: v1.0

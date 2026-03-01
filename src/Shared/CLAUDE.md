# Shared 层开发指南

## 技术栈

- .NET 8 + System.Text.Json + FluentValidation

## 核心类型

### 统一响应格式

```csharp
ApiResponse<T>  // Success, Message, Data, Timestamp, RequestId
  .Ok(data)     // 成功响应
  .Fail(message) // 失败响应

ServiceResult<T> // IsSuccess, Data, Message, Errors
  .Success(data)
  .Failure(error)

PagedResult<T>   // Items, TotalCount, PageIndex, PageSize, TotalPages
```

### DTO 基类层次

```csharp
BaseDto : IIdentifiable<Guid>     // Id
StatusDto : BaseDto                // + Status (CommonStatus)
PagedQueryBaseDto                  // PageIndex, PageSize, OrderBy, IsDescending, Keyword
```

### DTO 命名约定

| 后缀 | 用途 | 示例 |
|------|------|------|
| DetailDto | 完整详情 | MedicalCaseDetailDto, ConsultationDetailDto |
| ListDto | 列表展示 | MedicalCaseListDto, HerbListDto |
| InputDto | 创建/更新输入 | UserInputDto, PatientInputDto |
| InputBaseDto | 共享输入字段基类 | UserInputBaseDto |
| SearchDto | 高级搜索 (继承 PagedQueryBaseDto) | UserSearchDto, PatientSearchDto |
| ItemDto / ItemInputDto | 子实体 | PrescriptionItemDto, FormulaHerbItemInputDto |

### 枚举定义

| 枚举 | 用途 |
|------|------|
| UserRole | Doctor, Admin, Receptionist, SuperAdmin |
| CommonStatus | Enabled, Disabled |
| CaseStatus | Suspended, Active, Completed, Closed |
| PrescriptionStatus | Draft, Confirmed |
| LoginType | Password, AutoLogin, TokenRefresh |

## 业务模块 DTO 分布

```
Shared.Models/Contracts/
├── Auth/           # LoginDto, TokenDto, RefreshTokenDto, AuthResult
├── Users/          # UserDto, UserInputDto, UserSearchDto, UserBasicDto
├── Patients/       # PatientDto, PatientInputDto, PatientSearchDto
├── MedicalCase/    # MedicalCaseDetailDto, MedicalCaseListDto, MedicalCaseCreateDto
├── Consultation/   # ConsultationDetailDto, ConsultationInputDto
├── Prescriptions/  # PrescriptionDetailDto, PrescriptionInputDto, PrescriptionItemDto
├── Herbs/          # HerbDto, HerbInputDto, HerbSearchDto
├── Formula/        # FormulaDto, FormulaInputDto, FormulaSearchDto
└── Sync/           # SyncCompareInputDto, SyncUploadInputDto, SyncDownloadInputDto, SyncMetadataDto
```

## 开发注意事项

- 修改 DTO 必须确保前后端同步更新
- 所有查询 DTO 应继承 `PagedQueryBaseDto`
- 新增业务 DTO 需在 `Contracts/{Module}/` 目录下创建
- 枚举变更需同步更新前端缓存策略
- `IIdentifiable<T>` 接口用于统一实体标识

# Shared 层

> 前后端共享组件库，提供统一的 DTO 契约、枚举、验证器、工具类

## 架构概览

Shared 层是 Server 和 Desktop 的公共依赖，实现契约驱动设计。
所有 DTO 采用分层继承体系: BaseDto -> StatusDto，查询类继承 PagedQueryBaseDto。
统一响应格式 ApiResponse<T> 和 ServiceResult<T> 确保前后端数据契约一致。

DTO 已完成三阶段优化: 查询命名标准化、操作结果基类抽取、继承层次优化。

## 项目列表

| 项目 | 职责 | 状态 |
|------|------|------|
| LYBT.Shared.Models | DTO、响应模型、枚举、常量 | 稳定 |
| LYBT.Shared.Primitives | 核心原语（基类、接口定义） | 稳定 |
| LYBT.Shared.Validators | FluentValidation 验证器 | 稳定 |
| LYBT.Shared.Utilities | 扩展方法、帮助类 | 稳定 |
| LYBT.Shared.Components | 共享 UI 组件 | 稳定 |
| LYBT.Shared.Configuration | 配置管理共享逻辑 | 稳定 |
| LYBT.Shared.ExceptionHandling | 统一异常处理 | 稳定 |
| LYBT.Shared.Logging | 日志基础设施 | 稳定 |

## 目录结构

```
src/Shared/
├── LYBT.Shared.Models/
├── LYBT.Shared.Primitives/
├── LYBT.Shared.Validators/
├── LYBT.Shared.Utilities/
├── LYBT.Shared.Components/
├── LYBT.Shared.Configuration/
├── LYBT.Shared.ExceptionHandling/
└── LYBT.Shared.Logging/
```

## 依赖关系

```
Server.Modules  -> Shared.Models / Shared.Validators / Shared.Utilities
Desktop.Modules -> Shared.Models / Shared.Components / Shared.Utilities
```

- **被依赖**: Server 层和 Desktop 层均引用
- **自身无外部层依赖**: 仅依赖 .NET BCL 和第三方库 (FluentValidation, System.Text.Json)

## DTO 继承体系

```
BaseDto (Id)
  └── StatusDto (+ Status)
PagedQueryBaseDto (PageIndex, PageSize, OrderBy, Keyword)
UserInputBaseDto / PatientInputBaseDto ... (共享输入字段)
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简 README，详细内容迁移至 CLAUDE.md |
| 2025-09-20 | DTO 三阶段优化完成 |

## 开发笔记

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

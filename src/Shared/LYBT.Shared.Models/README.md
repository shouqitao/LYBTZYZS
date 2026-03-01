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

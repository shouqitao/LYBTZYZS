# LYBT.Shared.Models

> 共享数据模型库 | DTO契约定义 | 前后端类型一致

## 项目定位

- **层级**: Shared层
- **职责**: 提供Server/Client共享的数据传输对象(DTO)、枚举、异常类和扩展方法

## 目录结构

```
LYBT.Shared.Models/
├── Common/                 # 通用模型(3文件)
│   ├── BatchIdsDto.cs
│   ├── EnumItem.cs
│   └── NullableEnumItem.cs
├── Constants/              # 常量定义(2文件)
│   ├── ErrorMessageKeys.cs
│   └── ValidationConstants.cs
├── Contracts/              # DTO定义(8模块)
│   ├── Common/             # 通用契约(11文件)
│   ├── Auth/               # 认证DTO(8文件)
│   ├── Users/              # 用户DTO
│   ├── Patients/           # 患者DTO
│   ├── MedicalCase/        # 医案DTO
│   ├── Consultation/       # 诊断DTO
│   ├── Prescriptions/      # 处方DTO
│   ├── Herbs/              # 药材DTO
│   └── Formula/            # 验方DTO
├── Core/                   # 核心模型(1文件)
├── Enums/                  # 枚举定义(9文件)
├── Exceptions/             # 异常类(6文件)
└── Extensions/             # 扩展方法(8文件)
```

## 核心组件

| 组件 | 说明 |
|------|------|
| ApiResponse<T> | 统一API响应格式 |
| ServiceResult<T> | 服务层结果包装 |
| PagedResult<T> | 分页结果模型 |
| PagedQueryBaseDto | 分页查询基类 |

## DTO基类体系

| 基类 | 继承关系 | 说明 |
|------|----------|------|
| BaseDto | IIdentifiable<Guid> | 包含Id字段 |
| TimestampDto | BaseDto + IAuditable | 包含CreatedAt/UpdatedAt |
| StatusDto | TimestampDto + IStatusManageable | 包含Status字段 |
| CreateDtoBase | - | 创建操作基类(不含Id) |
| UpdateDtoBase | StatusDto | 更新操作基类(含Id) |

## 核心枚举

| 枚举 | 值 | 说明 |
|------|------|------|
| CommonStatus | Disabled, Enabled | 通用状态 |
| UserRole | Doctor, Admin | 用户角色 |
| Gender | Unknown, Male, Female | 性别 |
| CaseStatus | Registered, InProgress, Completed, Cancelled, Temporary | 医案状态 |
| PrescriptionStatus | Draft, Confirmed, Dispensed, Cancelled | 处方状态 |

## 异常类

| 异常类 | 说明 |
|--------|------|
| AppException | 应用异常基类 |
| BusinessException | 业务逻辑异常 |
| ValidationException | 验证失败异常 |
| NotFoundException | 资源未找到异常 |
| ApiException | API调用异常 |

## 依赖关系

### 依赖
- 无(基础设施层，零依赖)

### 被依赖
- LYBT.Infrastructure (引用Entity和Exceptions)
- LYBT.Module.*(所有Server模块)
- LYBT.WebAPI (引用所有DTO和ApiResponse)
- LYBT.Desktop.Contracts (引用所有DTO)
- LYBT.Desktop.*(所有Desktop模块)
- 所有测试项目

### NuGet包
- System.ComponentModel.Annotations (8.0.x)
- System.Text.Json (8.0.x)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | DTO三阶段优化完成 |
| 2025-09-20 | DTO基类体系重构 |

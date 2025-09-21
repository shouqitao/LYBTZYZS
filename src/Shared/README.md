# LYBT.Shared

> **前后端共享组件** - .NET 8核心共享库
> DTO优化完成 | 接口统一 | 类型安全 | 前后端一致性
> **模块状态**: ✅ **生产就绪** | 🎆 **DTO优化三阶段完成** | **零编译错误** | **2025-09-20更新**

## 🎯 项目概述

LYBT.Shared 是系统的核心共享组件库，提供前后端统一的数据传输对象(DTO)、服务接口定义和通用工具类。完成了DTO三阶段优化，实现了类型安全和前后端完全一致的数据契约。

**技术栈**: .NET 8 + System.Text.Json + FluentValidation
**架构模式**: 契约驱动设计 + 分层DTO模型 + 接口统一化
**最新优化**: DTO三阶段优化完成（查询标准化、操作结果基类、继承层次优化）

## 📚 规范文档

- **[Shared类型清单](../../docs/shared-inventory/shared-types.md)** - 268+类型完整清单和分类
- **[Shared依赖关系](../../docs/shared-inventory/shared-deps.md)** - 模块间依赖关系图
- **[枚举规范文档](../../docs/shared-inventory/shared-enums-spec.md)** - 枚举命名、i18n和前端缓存规范
- **[结构优化建议](../../docs/shared-inventory/shared-structure-proposal.md)** - 目录结构重构方案
- **[架构门禁规范](../../docs/shared-inventory/shared-arch-gates.md)** - 依赖边界和禁止项清单

## 📦 项目结构

### LYBT.Shared.Models
数据传输对象和响应模型 - DTO优化三阶段完成

```
LYBT.Shared.Models/
├── Common/                    # 通用模型
│   ├── ApiResponse.cs         # 统一API响应格式
│   ├── PagedResult.cs         # 分页数据模型
│   ├── BaseDto.cs            # DTO基类（包含Id）
│   ├── StatusDto.cs          # 状态DTO基类（BaseDto + Status字段）
│   ├── ServiceResult.cs      # 服务层结果模型
│   └── PagedQueryBaseDto.cs  # 分页查询基类
├── Contracts/                # 业务契约模型（DTO优化完成）
│   ├── Common/               # 通用契约
│   │   └── IIdentifiable.cs # ID接口定义
│   ├── Auth/                 # 认证相关DTO
│   │   └── AuthDtos.cs      # LoginDto, TokenDto, RefreshTokenDto
│   ├── Users/                # 用户相关DTO
│   │   └── UserDtos.cs      # UserDto, UserCreateDto, UserUpdateDto, UserSearchDto
│   ├── Patients/             # 患者相关DTO
│   │   └── PatientDtos.cs   # PatientDto, PatientCreateDto, PatientUpdateDto, PatientSearchDto
│   ├── MedicalCase/          # 医案相关DTO
│   │   └── MedicalCaseDtos.cs # MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseSearchDto
│   ├── Consultation/         # 诊疗相关DTO
│   │   └── ConsultationDtos.cs # ConsultationDetailDto, ConsultationCreateDto, ConsultationSearchDto
│   ├── Prescriptions/        # 处方相关DTO
│   │   ├── PrescriptionDtos.cs # PrescriptionDto, PrescriptionCreateDto, PrescriptionSearchDto
│   │   └── PrescriptionCalculationDto.cs # 处方计算DTO
│   ├── Herbs/                # 药材相关DTO
│   │   └── HerbDtos.cs      # HerbDto, HerbCreateDto, HerbUpdateDto, HerbSearchDto
│   └── Formula/              # 验方相关DTO
│       └── FormulaDtos.cs   # FormulaDto, FormulaCreateDto, FormulaUpdateDto, FormulaSearchDto
├── Core/                     # 核心定义
│   └── Interfaces/           # 核心接口
├── Enums/                    # 枚举定义
│   ├── UserRole.cs           # 用户角色枚举（Doctor, Admin）
│   ├── CommonStatus.cs       # 通用状态枚举（Enabled, Disabled）
│   ├── MedicalCaseStatus.cs  # 医案状态
│   └── PrescriptionStatus.cs # 处方状态
├── Constants/                # 常量定义
├── Exceptions/               # 异常定义
└── Extensions/               # 扩展方法
```

### LYBT.Shared.Interfaces  
业务服务接口定义

```
LYBT.Shared.Interfaces/
├── Services/                 # 服务接口
│   ├── IUserService.cs       # 用户服务接口
│   ├── IPatientService.cs    # 患者服务接口
│   ├── IConsultationService.cs # 看诊服务接口
│   ├── IPrescriptionService.cs # 处方服务接口
│   ├── IHerbService.cs       # 中药材服务接口
│   └── IFormulaService.cs    # 验方服务接口
└── Repositories/             # 仓储接口
    ├── IBaseRepository.cs    # 基础仓储接口
    └── [各业务仓储接口]
```

### LYBT.Shared.Utilities
通用工具类和扩展方法

```
LYBT.Shared.Utilities/
├── Extensions/               # 扩展方法
├── Helpers/                  # 帮助类
├── Validators/               # 数据验证器
└── Constants/                # 常量定义
```

## 🏗️ 技术特性

### 统一响应格式
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
    
    // 成功响应
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    
    // 失败响应  
    public static ApiResponse<T> Fail(string message, T? data = default)
}
```

### 分页数据模型
```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
```

### 服务结果模型
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
    
    public static ServiceResult<T> Success(T data, string message = "")
    public static ServiceResult<T> Failure(string error)
    public static ServiceResult<T> Failure(List<string> errors)
}
```

## 🎆 DTO优化三阶段成果

### 第一阶段：查询命名标准化
- ✅ 统一查询DTO命名：`QueryDto`（基础查询）、`SearchDto`（高级搜索）
- ✅ 继承`PagedQueryBaseDto`基类，统一分页参数

### 第二阶段：操作结果基类抽取
- ✅ 创建`StatusDto`基类（BaseDto + Status字段）
- ✅ 所有业务DTO继承自`StatusDto`
- ✅ 统一状态管理模式

### 第三阶段：继承层次优化
- ✅ 分离`CreateDto`和`UpdateDto`
- ✅ 提取`InputBaseDto`共享字段
- ✅ 实现`IIdentifiable<T>`接口

## 🎯 核心DTO模型

### 基类层次结构
```csharp
// 基础DTO - 包含Id
public abstract class BaseDto : IIdentifiable<Guid>
{
    public Guid Id { get; set; }
}

// 状态DTO - 添加状态字段
public abstract class StatusDto : BaseDto
{
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

// 分页查询基类
public abstract class PagedQueryBaseDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? OrderBy { get; set; }
    public bool IsDescending { get; set; }
    public string? Keyword { get; set; }
}
```

### 用户模块DTO示例（优化后）
```csharp
// 用户信息DTO - 继承StatusDto
public class UserDto : StatusDto
{
    public string Username { get; set; }
    public string RealName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string? PinYinCode { get; set; }

    // 兼容性属性
    public bool IsActive => Status == CommonStatus.Enabled;
    public string UserDisplayName => RealName ?? Username;
}

// 用户输入基础DTO - 共享字段
public abstract class UserInputBaseDto
{
    [Required]
    public string RealName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; } = UserRole.Doctor;
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

// 用户创建DTO - 继承输入基础
public class UserCreateDto : UserInputBaseDto
{
    [Required]
    public string Username { get; set; }
    [Required]
    public string Password { get; set; }
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}

// 用户更新DTO - 可选字段
public class UserUpdateDto : UserInputBaseDto, IIdentifiable<Guid>
{
    [Required]
    public Guid Id { get; set; }
    public new string? RealName { get; set; }  // 可选更新
    public new UserRole? Role { get; set; }     // 可选更新
}

// 用户搜索DTO - 高级搜索
public class UserSearchDto : PagedQueryBaseDto
{
    public string? Username { get; set; }
    public string? RealName { get; set; }
    public UserRole? Role { get; set; }
    public CommonStatus? Status { get; set; }
    public string? PinYinCode { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeInactive { get; set; } = false;
}
```

### 患者相关
```csharp
// 患者信息DTO
public class PatientDto : BaseDto
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string IdCardNumber { get; set; }
}
```

### 诊疗相关
```csharp
// 医疗案例DTO
public class MedicalCaseDto : BaseDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
}

// 看诊详情DTO
public class ConsultationDetailDto : BaseDto
{
    public Guid MedicalCaseId { get; set; }
    public string ChiefComplaint { get; set; }        // 主诉
    public string PresentIllness { get; set; }        // 现病史
    public string TCMObservation { get; set; }        // 望诊
    public string TCMAuscultation { get; set; }       // 闻诊
    public string TCMInquiry { get; set; }            // 问诊
    public string TCMPalpation { get; set; }          // 切诊
    public string Diagnosis { get; set; }             // 诊断
    public string Treatment { get; set; }             // 治疗方案
}
```

## 🔧 使用指南

### 在前端项目中使用
```csharp
// 注册服务接口实现
services.AddScoped<IUserService, UserModuleService>();

// 使用DTO进行数据传输
var createDto = new UserCreateDto 
{
    Username = "doctor01",
    RealName = "张医生",
    Role = UserRole.Doctor
};
```

### 在后端项目中使用
```csharp
// Controller返回统一响应格式
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([Body] UserCreateDto dto)
{
    var result = await _userService.CreateAsync(dto);
    return HandleServiceResult(result, "用户创建成功");
}
```

## 📊 设计原则

- **统一性**: 前后端使用相同的DTO模型，确保数据一致性
- **类型安全**: 强类型模型，编译时检查数据结构
- **版本控制**: 支持API版本管理和向后兼容
- **可扩展性**: 模块化设计，便于新增业务模型
- **验证友好**: 支持数据注解和FluentValidation

## 📈 性能优化

- **轻量级**: 仅包含数据传输所需属性
- **序列化优化**: JSON序列化性能优化
- **内存友好**: 避免循环引用和大对象
- **缓存友好**: 支持DTO级别的缓存策略

---

> 📌 **开发提醒**: 修改共享模型时请确保前后端同步更新，避免版本不一致问题
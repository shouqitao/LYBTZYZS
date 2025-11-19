# LYBT.Shared.Models

> **共享数据模型库** - .NET 8 DTO与契约定义
> DTO优化完成 | 类型安全 | 前后端一致 | 验证增强
> **模块状态**:  **生产就绪** | 🎆 **DTO三阶段优化完成** | **零编译错误** | **2025-10-29更新**

## 📦 项目定位

- **层级**: Shared层（跨端共享）
- **类型**: 核心数据模型库
- **职责**: 提供前后端共享的数据传输对象(DTO)、实体模型、枚举类型、异常类和扩展方法。完成了DTO三阶段系统性优化，实现了完全类型安全的数据契约。作为整个系统的数据契约基础，确保Client端、Server端、WebAPI之间的数据结构一致性。

## 📂 代码结构

```
LYBT.Shared.Models/ (16目录, 70文件)
├── Common/                           # 通用模型 (3文件)
│   ├── BatchIdsDto.cs                # 批量ID操作DTO
│   ├── EnumItem.cs                   # 枚举项DTO
│   └── NullableEnumItem.cs           # 可空枚举项DTO
├── Constants/                        # 常量定义 (2文件)
│   ├── ErrorMessageKeys.cs           # 错误消息键常量
│   └── ValidationConstants.cs        # 验证规则常量（最大长度、最小值等）
├── Contracts/                        # 数据传输对象(DTO)
│   ├── Common/                       # 通用契约 (11文件)
│   │   ├── ApiResponse.cs            # 统一API响应格式
│   │   ├── ServiceResult.cs          # 服务层结果包装
│   │   ├── PagedResult.cs            # 分页结果模型
│   │   ├── PagedQueryBaseDto.cs      # 分页查询基类
│   │   ├── DtoBase.cs                # DTO基类体系（5个接口 + 6个基类）
│   │   ├── OperationResultDtos.cs    # 操作结果DTO
│   │   ├── HealthCheckResponse.cs    # 健康检查响应
│   │   ├── HandledError.cs           # 已处理错误信息
│   │   ├── ErrorCategory.cs          # 错误分类枚举
│   │   ├── ErrorSeverity.cs          # 错误严重级别
│   │   ├── ErrorContext.cs           # 错误上下文信息
│   │   └── SharedCommon.cs           # 共享公共定义
│   ├── Auth/                         # 认证相关DTO (8文件)
│   │   ├── LoginRequest.cs           # 登录请求
│   │   ├── LoginResponse.cs          # 登录响应
│   │   ├── LogoutRequest.cs          # 登出请求
│   │   ├── ChangePasswordRequest.cs  # 修改密码请求
│   │   ├── ChangeSysAdminPassword.cs # 系统管理员密码修改
│   │   ├── SuperAdminLoginRequest.cs # 超级管理员登录
│   │   ├── TokenPair.cs              # Token对（AccessToken + RefreshToken）
│   │   └── ValidateTokenRequest.cs   # Token验证请求
│   ├── Users/                        # 用户相关DTO (1文件, 4个DTO)
│   │   └── UserDtos.cs               # UserDto, UserCreateDto, UserUpdateDto, UserSearchDto
│   ├── Patients/                     # 患者相关DTO (3文件)
│   │   ├── PatientDtos.cs            # PatientDto, PatientCreateDto, PatientUpdateDto, PatientSearchDto
│   │   ├── PatientOperationDtos.cs   # 患者操作相关DTO
│   │   └── PatientStatisticsDtos.cs  # 患者统计DTO
│   ├── MedicalCase/                  # 医案相关DTO (2文件)
│   │   ├── MedicalCaseDtos.cs        # MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseSearchDto
│   │   └── SimplifiedMedicalCaseDtos.cs # 简化医案DTO（用于列表显示）
│   ├── Consultation/                 # 诊疗相关DTO (1文件)
│   │   └── ConsultationDtos.cs       # ConsultationDetailDto, ConsultationCreateDto, ConsultationSearchDto
│   ├── Prescriptions/                # 处方相关DTO (5文件)
│   │   ├── PrescriptionDtos.cs       # PrescriptionDto, PrescriptionCreateDto, PrescriptionSearchDto
│   │   ├── PrescriptionCalculationDto.cs # 处方计算DTO（单帖价格、总价格）
│   │   ├── PrescriptionSearchResultDto.cs # 处方搜索结果DTO
│   │   ├── PrescriptionUpdateDto.cs  # 处方更新DTO
│   │   └── PrescriptionValidationResult.cs # 处方验证结果DTO
│   ├── Herbs/                        # 药材相关DTO (2文件)
│   │   ├── HerbDtos.cs               # HerbDto, HerbCreateDto, HerbUpdateDto, HerbSearchDto
│   │   └── HerbOperationDtos.cs      # 药材操作相关DTO
│   └── Formula/                      # 验方相关DTO (2文件)
│       ├── FormulaDtos.cs            # FormulaDto, FormulaCreateDto, FormulaSearchDto
│       └── FormulaAnalysisDtos.cs    # 验方分析DTO
├── Core/                             # 核心模型 (1文件)
│   └── BaseAuthSession.cs            # 认证会话基类
├── Enums/                            # 枚举定义 (9文件)
│   ├── AuthEnums.cs                  # 认证相关枚举（AuthType等）
│   ├── SystemEnums.cs                # 系统枚举（CommonStatus等）
│   ├── Gender.cs                     # 性别枚举
│   ├── RecordEnums.cs                # 记录相关枚举
│   ├── CaseStatus.cs                 # 医案状态枚举
│   ├── MedicalCaseEnums.cs           # 医案相关枚举
│   ├── PatientStatus.cs              # 患者状态枚举
│   ├── PrescriptionStatus.cs         # 处方状态枚举
│   └── FormulaValidationStatus.cs    # 验方验证状态枚举
├── Exceptions/                       # 异常定义 (6文件)
│   ├── AppException.cs               # 应用异常基类
│   ├── BusinessException.cs          # 业务异常
│   ├── ValidationException.cs        # 验证异常
│   ├── NotFoundException.cs          # 资源未找到异常
│   ├── ApiException.cs               # API异常
│   └── ExceptionFactory.cs           # 异常工厂（统一创建异常）
└── Extensions/                       # 扩展方法 (8文件)
    ├── EnumExtensions.cs             # 枚举扩展（GetDescription等）
    ├── UserDtoExtensions.cs          # 用户DTO扩展
    ├── PatientDtoExtensions.cs       # 患者DTO扩展
    ├── MedicalCaseDtoExtensions.cs   # 医案DTO扩展
    ├── ConsultationDtoExtensions.cs  # 诊疗DTO扩展
    ├── PrescriptionDtoExtensions.cs  # 处方DTO扩展
    ├── HerbDtoExtensions.cs          # 药材DTO扩展
    └── FormulaDtoExtensions.cs       # 验方DTO扩展
```

**说明**:
- **Common/**: 3个通用模型，支持批量操作和枚举项转换
- **Constants/**: 2个常量定义文件，统一验证规则和错误消息
- **Contracts/**: 8个模块的完整DTO定义（11个Common通用契约 + 8个Auth认证 + 7个业务模块DTO）
- **Core/**: 1个核心模型BaseAuthSession，支持认证会话管理
- **Enums/**: 9个枚举定义文件，覆盖所有业务状态和类型
- **Exceptions/**: 6个异常类，统一异常处理机制（包含ExceptionFactory工厂）
- **Extensions/**: 8个扩展方法文件，为DTO提供便捷操作（7个业务模块扩展 + 1个枚举扩展）

## 🔗 依赖关系

### 依赖的项目
**无依赖** - 作为基础设施层项目，不依赖任何其他项目

### 被依赖项目
1. **LYBT.Server.Interfaces** - Server端接口定义层（引用DTO和枚举）
2. **LYBT.Infrastructure** - 基础设施层（引用Entity和Exceptions）
3. **LYBT.Module.*（8个Server端模块）** - 引用对应的DTO和枚举
4. **LYBT.WebAPI** - WebAPI层（引用所有DTO和ApiResponse）
5. **LYBT.Desktop.Contracts** - Desktop端契约层（引用所有DTO）
6. **LYBT.Desktop.*（8个Desktop端模块）** - 引用对应的DTO
7. **LYBT.Desktop.Shell** - Desktop端Shell（引用通用DTO）
8. **测试项目（10+个）** - 所有测试项目都引用Shared.Models

### NuGet包
- **System.ComponentModel.Annotations** (8.0.x) - DataAnnotations验证特性
- **System.Text.Json** (8.0.x) - JSON序列化（JsonPropertyName等）

## 🛠 技术栈

- **.NET 8**: 目标框架
- **System.ComponentModel.Annotations**: DataAnnotations验证框架
- **System.Text.Json**: JSON序列化和反序列化
- **C# 12**: 最新语言特性（Record、Init-only属性、Primary Constructor等）

## 🎆 DTO优化三阶段成果

### 第一阶段：查询命名标准化 
- 统一查询DTO命名模式：`QueryDto`（基础）和`SearchDto`（高级）
- 所有查询DTO继承`PagedQueryBaseDto`基类
- 统一分页、排序、关键词搜索参数

### 第二阶段：操作结果基类抽取 
- 创建`StatusDto`基类（BaseDto + Status字段）
- 所有业务DTO继承自`StatusDto`
- 统一状态管理模式（CommonStatus枚举）

### 第三阶段：继承层次优化 
- 完全分离`CreateDto`和`UpdateDto`
- 提取`InputBaseDto`共享字段
- 实现`IIdentifiable<T>`接口
- 删除未使用的字段和过时代码

## 🏛️ DTO基类架构体系

### 核心接口定义（5个接口）

```csharp
// 1. 标识接口 - 提供唯一标识符
public interface IIdentifiable<T>
{
    T Id { get; set; }
}

// 2. 审计接口 - 提供创建和更新时间追踪
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

// 3. 状态管理接口 - 提供通用状态字段
public interface IStatusManageable
{
    CommonStatus Status { get; set; }
}

// 4. 备注接口 - 提供备注字段
public interface IRemarkable
{
    string? Remark { get; set; }
}

// 5. 编码接口 - 提供拼音码
public interface ICodeable
{
    string? PinYinCode { get; set; }
}
```

### 简化DTO基础类体系（UltraThink架构优化）

```csharp
// 1. BaseDto - 基础DTO（包含ID）
public abstract class BaseDto : IIdentifiable<Guid>
{
    [DisplayName("ID")]
    public Guid Id { get; set; }
}

// 2. TimestampDto - 时间戳DTO（BaseDto + 审计时间）
public abstract class TimestampDto : BaseDto, IAuditable
{
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}

// 3. StatusDto - 状态管理DTO（TimestampDto + 状态字段）
public abstract class StatusDto : TimestampDto, IStatusManageable
{
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    [DisplayName("是否启用")]
    public bool IsEnabled => Status == CommonStatus.Enabled;
}
```

### CRUD操作DTO基类

```csharp
// 1. CreateDtoBase - 创建操作DTO基类（不包含ID）
public abstract class CreateDtoBase : IStatusManageable, IRemarkable
{
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }
}

// 2. UpdateDtoBase - 更新操作DTO基类（包含ID）
public abstract class UpdateDtoBase : StatusDto, IRemarkable
{
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }
}
```

### 查询DTO基类

```csharp
// 1. PagedQueryBaseDto - 分页查询基类
public abstract class PagedQueryBaseDto
{
    [DisplayName("页码")]
    public int PageIndex { get; set; } = 1;

    [DisplayName("每页条数")]
    public int PageSize { get; set; } = 20;

    [DisplayName("排序字段")]
    public string? OrderBy { get; set; }

    [DisplayName("降序排序")]
    public bool IsDescending { get; set; }

    [DisplayName("搜索关键词")]
    public string? Keyword { get; set; }
}

// 2. ExtendedQueryDto - 扩展查询DTO基类
public abstract class ExtendedQueryDto : PagedQueryBaseDto
{
    [DisplayName("状态")]
    public CommonStatus? Status { get; set; }

    [DisplayName("开始日期")]
    public DateTime? StartDate { get; set; }

    [DisplayName("结束日期")]
    public DateTime? EndDate { get; set; }

    [DisplayName("包含已禁用")]
    public bool IncludeInactive { get; set; } = false;

    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }
}
```

### 统计DTO基类

```csharp
// StatisticsDto - 统计DTO基类
public abstract class StatisticsDto
{
    [DisplayName("总数")]
    public int TotalCount { get; set; }

    [DisplayName("统计时间")]
    public DateTime StatisticsTime { get; set; } = DateTime.Now;

    [DisplayName("启用数量")]
    public int EnabledCount { get; set; }

    [DisplayName("禁用数量")]
    public int DisabledCount { get; set; }

    [DisplayName("已删除数量")]
    public int DeletedCount { get; set; }
}
```

## 📊 DTO继承关系架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    接口定义（5个）                            │
├─────────────────────────────────────────────────────────────┤
│ IIdentifiable<T>  IAuditable  IStatusManageable             │
│ IRemarkable  ICodeable                                       │
└───────────────────┬─────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        │     BaseDto (抽象)     │ ← IIdentifiable<Guid>
        │   - Id: Guid           │
        └───────────┬───────────┘
                    │
        ┌───────────┴───────────┐
        │  TimestampDto (抽象)   │ ← IAuditable
        │   - CreatedAt          │
        │   - UpdatedAt          │
        └───────────┬───────────┘
                    │
        ┌───────────┴───────────┐
        │   StatusDto (抽象)     │ ← IStatusManageable
        │   - Status             │
        │   - IsEnabled (计算)   │
        └───────────┬───────────┘
                    │
    ┌───────────────┴───────────────┐
    │                               │
┌───┴──────────┐          ┌────────┴────────┐
│ 业务DTO继承   │          │ UpdateDtoBase   │ ← IRemarkable
│ (UserDto等)   │          │  - Remark       │
└──────────────┘          └─────────────────┘

        ┌─────────────────────────┐
        │  CreateDtoBase (抽象)    │ ← IStatusManageable + IRemarkable
        │   - Status               │
        │   - Remark               │
        └───────────┬─────────────┘
                    │
        ┌───────────┴──────────────┐
        │ 业务CreateDto继承          │
        │ (UserCreateDto等)         │
        └──────────────────────────┘

        ┌─────────────────────────┐
        │ PagedQueryBaseDto (抽象) │
        │   - PageIndex            │
        │   - PageSize             │
        │   - OrderBy              │
        │   - Keyword              │
        └───────────┬─────────────┘
                    │
        ┌───────────┴──────────────┐
        │  ExtendedQueryDto (抽象)  │
        │   - Status               │
        │   - StartDate/EndDate    │
        │   - IncludeInactive      │
        │   - PinYinCode           │
        └───────────┬──────────────┘
                    │
        ┌───────────┴──────────────┐
        │ 业务SearchDto继承          │
        │ (UserSearchDto等)         │
        └──────────────────────────┘
```

## 📚 模块DTO设计示例

### 1. 用户模块（完整优化示例）

```csharp
#region 基础DTO

/// <summary>
/// 用户信息DTO - 继承StatusDto
/// </summary>
public class UserDto : StatusDto
{
    [DisplayName("用户名")]
    public string Username { get; set; } = string.Empty;

    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    [DisplayName("用户角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }

    [DisplayName("邮箱地址")]
    public string? Email { get; set; }

    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    // 兼容性属性
    [DisplayName("账号启用状态")]
    public bool IsActive => Status == CommonStatus.Enabled;

    [DisplayName("用户显示名")]
    public string UserDisplayName => RealName ?? Username;
}

#endregion

#region 输入DTO

/// <summary>
/// 用户输入基础DTO - 共享字段
/// </summary>
public abstract class UserInputBaseDto
{
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "电话号码格式不正确")]
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [DisplayName("邮箱地址")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "用户角色不能为空")]
    [DisplayName("用户角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

/// <summary>
/// 用户创建DTO - 继承输入基础
/// </summary>
public class UserCreateDto : UserInputBaseDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$")]
    [DisplayName("用户名")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6)]
    [DisplayName("密码")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [DisplayName("确认密码")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 用户更新DTO - 可选更新
/// </summary>
public class UserUpdateDto : UserInputBaseDto, IIdentifiable<Guid>
{
    [Required(ErrorMessage = "用户ID不能为空")]
    [DisplayName("用户ID")]
    public Guid Id { get; set; }

    // 使用new修饰符实现可选更新
    [DisplayName("真实姓名")]
    public new string? RealName { get; set; }

    [DisplayName("用户角色")]
    public new UserRole? Role { get; set; }
}

#endregion

#region 查询DTO

/// <summary>
/// 用户查询DTO - 基础查询
/// </summary>
public class UserQueryDto : PagedQueryBaseDto
{
    [DisplayName("用户名")]
    public string? Username { get; set; }

    [DisplayName("真实姓名")]
    public string? RealName { get; set; }

    [DisplayName("用户角色")]
    public UserRole? Role { get; set; }

    [DisplayName("状态")]
    public CommonStatus? Status { get; set; }
}

/// <summary>
/// 用户搜索DTO - 高级搜索
/// </summary>
public class UserSearchDto : UserQueryDto
{
    [DisplayName("邮箱")]
    public string? Email { get; set; }

    [DisplayName("电话")]
    public string? PhoneNumber { get; set; }

    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    [DisplayName("开始日期")]
    public DateTime? StartDate { get; set; }

    [DisplayName("结束日期")]
    public DateTime? EndDate { get; set; }

    [DisplayName("包含已禁用")]
    public bool IncludeInactive { get; set; } = false;
}

#endregion
```

### 2. 处方模块（计算属性示例）

```csharp
/// <summary>
/// 处方信息DTO - 包含计算属性
/// </summary>
public class PrescriptionDto : StatusDto, IRemarkable
{
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    [DisplayName("患者ID")]
    public Guid PatientId { get; set; }

    [DisplayName("患者姓名")]
    public string? Name { get; set; }

    [DisplayName("诊断")]
    public string? Diagnosis { get; set; }

    [DisplayName("剂数")]
    public int DosageCount { get; set; } = 7;

    [DisplayName("折扣")]
    public decimal Discount { get; set; } = 1.0m;

    [DisplayName("处方项目")]
    public List<PrescriptionItemDto> Items { get; set; } = new();

    // 计算属性 - 单帖价格
    [DisplayName("单帖价格")]
    public decimal SingleDosePrice
    {
        get
        {
            if (Items == null || !Items.Any())
                return 0m;

            var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
            return subtotal * Discount;
        }
    }

    // 计算属性 - 总价格
    [DisplayName("总价格")]
    public decimal TotalPrice => SingleDosePrice * DosageCount;

    [DisplayName("备注")]
    public string? Remark { get; set; }
}
```

### 3. 患者模块（拼音码示例）

```csharp
/// <summary>
/// 患者信息DTO - 支持拼音码快速检索
/// </summary>
public class PatientDto : StatusDto, ICodeable, IRemarkable
{
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;

    [DisplayName("年龄")]
    public int? Age { get; set; }

    [DisplayName("出生日期")]
    public DateTime? DateOfBirth { get; set; }

    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    [DisplayName("地址")]
    public string? Address { get; set; }

    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    [DisplayName("备注")]
    public string? Remark { get; set; }

    // 计算属性 - 显示年龄
    [DisplayName("显示年龄")]
    public string DisplayAge
    {
        get
        {
            if (Age.HasValue)
                return $"{Age}岁";

            if (DateOfBirth.HasValue)
            {
                var age = DateTime.Now.Year - DateOfBirth.Value.Year;
                return $"{age}岁";
            }

            return "未知";
        }
    }
}
```

## 🔧 通用响应格式

### 1. ApiResponse<T> - API统一响应

```csharp
/// <summary>
/// API统一响应格式
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            RequestId = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResponse<T> CreateFailure(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            RequestId = Guid.NewGuid().ToString()
        };
    }
}
```

### 2. ServiceResult<T> - 服务层结果

```csharp
/// <summary>
/// 服务层结果包装
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    // 兼容性属性
    public string? Message => ErrorMessage;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ServiceResult<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
```

### 3. PagedResult<T> - 分页结果

```csharp
/// <summary>
/// 分页结果模型
/// </summary>
/// <typeparam name="T">数据项类型</typeparam>
public class PagedResult<T>
{
    [DisplayName("数据项")]
    public List<T> Items { get; set; } = new();

    [DisplayName("总记录数")]
    public int TotalCount { get; set; }

    [DisplayName("页码")]
    public int PageIndex { get; set; } = 1;

    [DisplayName("每页条数")]
    public int PageSize { get; set; } = 20;

    // 计算属性
    [DisplayName("总页数")]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [DisplayName("是否有上一页")]
    public bool HasPreviousPage => PageIndex > 1;

    [DisplayName("是否有下一页")]
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// 创建空结果
    /// </summary>
    public static PagedResult<T> Empty(int pageIndex = 1, int pageSize = 20)
    {
        return new PagedResult<T>
        {
            Items = new List<T>(),
            TotalCount = 0,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
```

## 📊 枚举定义

### 1. 核心业务枚举

```csharp
// 通用状态枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommonStatus
{
    [Description("禁用")]
    Disabled = 0,

    [Description("启用")]
    Enabled = 1
}

// 用户角色枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    [Description("医生")]
    Doctor = 0,

    [Description("管理员")]
    Admin = 1
}

// 性别枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    [Description("未知")]
    Unknown = 0,

    [Description("男")]
    Male = 1,

    [Description("女")]
    Female = 2
}

// 医案状态枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CaseStatus
{
    [Description("已登记")]
    Registered = 0,

    [Description("诊疗中")]
    InProgress = 1,

    [Description("已完成")]
    Completed = 2,

    [Description("已取消")]
    Cancelled = 3,

    [Description("暂存")]
    Temporary = 4
}

// 处方状态枚举
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrescriptionStatus
{
    [Description("草稿")]
    Draft = 0,

    [Description("已确认")]
    Confirmed = 1,

    [Description("已配药")]
    Dispensed = 2,

    [Description("已取消")]
    Cancelled = 3
}
```

### 2. 枚举扩展方法

```csharp
/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取枚举描述
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
            as DescriptionAttribute;

        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// 获取所有枚举项
    /// </summary>
    public static List<EnumItem> GetAllItems<TEnum>() where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(e => new EnumItem
            {
                Value = Convert.ToInt32(e),
                Name = e.ToString(),
                Description = e.GetDescription()
            })
            .ToList();
    }
}
```

##  使用示例

### 1. 控制器使用（WebAPI层）

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 分页查询用户
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] UserSearchDto searchDto)
    {
        var result = await _userService.GetPagedAsync(searchDto);

        if (result.IsSuccess)
            return Ok(ApiResponse<PagedResult<UserDto>>.CreateSuccess(result.Data));

        return BadRequest(ApiResponse<PagedResult<UserDto>>.CreateFailure(result.ErrorMessage));
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] UserCreateDto createDto)
    {
        var result = await _userService.CreateAsync(createDto);

        if (result.IsSuccess)
            return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data, "用户创建成功"));

        return BadRequest(ApiResponse<UserDto>.CreateFailure(result.ErrorMessage));
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid id,
        [FromBody] UserUpdateDto updateDto)
    {
        if (id != updateDto.Id)
            return BadRequest(ApiResponse<UserDto>.CreateFailure("ID不匹配"));

        var result = await _userService.UpdateAsync(updateDto);

        if (result.IsSuccess)
            return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data, "用户更新成功"));

        return BadRequest(ApiResponse<UserDto>.CreateFailure(result.ErrorMessage));
    }
}
```

### 2. 服务层使用（Server端）

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;

    public UserService(
        IUserRepository repository,
        IMapper mapper,
        AppDbContext context)
    {
        _repository = repository;
        _mapper = mapper;
        _context = context;
    }

    /// <summary>
    /// 分页查询用户
    /// </summary>
    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto searchDto)
    {
        try
        {
            var query = _context.Users.AsQueryable();

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(searchDto.Username))
                query = query.Where(u => u.Username.Contains(searchDto.Username));

            if (searchDto.Role.HasValue)
                query = query.Where(u => u.Role == searchDto.Role.Value);

            if (!searchDto.IncludeInactive)
                query = query.Where(u => u.Status == CommonStatus.Enabled);

            // 拼音码搜索
            if (!string.IsNullOrWhiteSpace(searchDto.PinYinCode))
                query = query.Where(u => u.PinYinCode != null &&
                                         u.PinYinCode.Contains(searchDto.PinYinCode));

            // 分页
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(searchDto.OrderBy ?? "Username", searchDto.IsDescending)
                .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var pagedResult = new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = searchDto.PageIndex,
                PageSize = searchDto.PageSize
            };

            return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ServiceResult<PagedResult<UserDto>>.Failure($"查询失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto createDto)
    {
        try
        {
            // 检查用户名是否已存在
            var existingUser = await _repository.GetByUsernameAsync(createDto.Username);
            if (existingUser != null)
                return ServiceResult<UserDto>.Failure("用户名已存在");

            // 创建用户实体
            var userEntity = _mapper.Map<UserModel>(createDto);

            // 生成拼音码（如果有Helper）
            userEntity.PinYinCode = PinYinHelper.GetInitials(createDto.RealName);

            // 保存到数据库
            await _repository.AddAsync(userEntity);
            await _context.SaveChangesAsync();

            // 返回DTO
            var userDto = _mapper.Map<UserDto>(userEntity);
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            return ServiceResult<UserDto>.Failure($"创建失败: {ex.Message}", ex);
        }
    }
}
```

### 3. Desktop端使用（ViewModel）

```csharp
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserRepository _userRepository;

    // 用户列表（绑定到DataGrid）
    public ObservableCollection<UserDto> Users { get; set; } = new();

    // 选中的用户
    public UserDto? SelectedUser { get; set; }

    // 搜索条件
    public UserSearchDto SearchCriteria { get; set; } = new();

    /// <summary>
    /// 加载用户列表
    /// </summary>
    public async Task LoadUsersAsync()
    {
        IsBusy = true;
        try
        {
            // 调用Repository获取分页数据
            var pagedResult = await _userRepository.GetPagedAsync(SearchCriteria);

            // 更新UI
            Users.Clear();
            foreach (var user in pagedResult.Items)
            {
                Users.Add(user);
            }

            // 更新分页信息
            TotalCount = pagedResult.TotalCount;
            TotalPages = pagedResult.TotalPages;

            _logger.LogInformation($"加载用户列表成功: {pagedResult.Items.Count}条记录");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            await _dialogService.ShowAlertAsync("错误", $"加载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<bool> CreateUserAsync(UserCreateDto createDto)
    {
        try
        {
            // 验证密码匹配
            if (createDto.Password != createDto.ConfirmPassword)
            {
                await _dialogService.ShowAlertAsync("错误", "两次输入的密码不一致");
                return false;
            }

            // 调用Repository创建
            var newUser = await _userRepository.CreateAsync(createDto);

            // 添加到列表
            Users.Add(newUser);

            await _dialogService.ShowAlertAsync("成功", "用户创建成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败");
            await _dialogService.ShowAlertAsync("错误", $"创建失败: {ex.Message}");
            return false;
        }
    }
}
```

### 4. 枚举转换使用

```csharp
// 在ViewModel中获取所有角色选项
public List<EnumItem> RoleOptions { get; set; } =
    EnumExtensions.GetAllItems<UserRole>();

// 在XAML中绑定到ComboBox
<ComboBox ItemsSource="{Binding RoleOptions}"
          SelectedValuePath="Value"
          DisplayMemberPath="Description"
          SelectedValue="{Binding SelectedUser.Role}" />
```

### 5. DTO扩展方法使用

```csharp
// UserDtoExtensions示例
public static class UserDtoExtensions
{
    /// <summary>
    /// 判断用户是否为管理员
    /// </summary>
    public static bool IsAdmin(this UserDto user)
    {
        return user.Role == UserRole.Admin;
    }

    /// <summary>
    /// 获取用户显示名称（优先真实姓名）
    /// </summary>
    public static string GetDisplayName(this UserDto user)
    {
        return !string.IsNullOrWhiteSpace(user.RealName)
            ? user.RealName
            : user.Username;
    }

    /// <summary>
    /// 转换为简化DTO（用于列表显示）
    /// </summary>
    public static UserSimplifiedDto ToSimplified(this UserDto user)
    {
        return new UserSimplifiedDto
        {
            Id = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Role = user.Role,
            Status = user.Status
        };
    }
}

// 在代码中使用
var user = await _userRepository.GetByIdAsync(userId);
if (user.IsAdmin())
{
    // 管理员权限操作
}

var displayName = user.GetDisplayName(); // "张医生" or "admin"
```

## 🎯 最佳实践

### 1. DTO设计原则
-  **使用继承优化代码复用** - 统一基类减少重复代码
-  **分离创建和更新DTO** - 创建不包含ID，更新包含ID
-  **使用DataAnnotations验证** - 前后端统一验证规则
-  **提供计算属性而非冗余字段** - 避免数据不一致
-  **使用可空类型表示可选字段** - 明确必填/可选语义
-  **接口驱动设计** - 通过接口（IRemarkable、ICodeable等）实现功能组合

### 2. 命名规范
-  **DTO后缀**：`UserDto`（实体DTO）
-  **创建DTO**：`UserCreateDto`（创建操作）
-  **更新DTO**：`UserUpdateDto`（更新操作）
-  **查询DTO**：`UserQueryDto`（基础查询）、`UserSearchDto`（高级搜索）
-  **响应DTO**：`LoginResponse`、`OperationResult`（特定响应）

### 3. 验证规则
-  **使用DataAnnotations属性** - Required、StringLength、RegularExpression等
-  **提供清晰的错误消息** - ErrorMessage明确说明验证失败原因
-  **前后端验证保持一致** - Client端和Server端使用相同的验证规则
-  **使用FluentValidation处理复杂验证** - 跨字段验证、业务规则验证

### 4. JSON序列化
-  **使用camelCase命名** - [JsonPropertyName("success")]
-  **枚举序列化为字符串** - [JsonConverter(typeof(JsonStringEnumConverter))]
-  **忽略null值** - JsonIgnoreCondition.WhenWritingNull
-  **使用UTC时间** - DateTime.UtcNow避免时区问题

### 5. 性能优化
-  **轻量级DTO** - 仅包含必要字段，避免过度包含
-  **计算属性** - 避免存储冗余数据，按需计算
-  **延迟加载** - 导航属性按需加载，避免N+1查询
-  **投影查询** - 使用AutoMapper ProjectTo优化查询性能

### 6. 安全考虑
-  **密码处理** - 密码字段仅在创建/修改时传输，不在UserDto中暴露
-  **敏感信息** - 避免在DTO中暴露敏感数据（如PasswordHash、Salt等）
-  **权限控制** - 根据角色过滤返回字段
-  **输入验证** - 严格的输入验证防止注入攻击

## 📈 性能优化

### 1. DTO投影优化

```csharp
// ❌ 不推荐：查询完整实体再映射
var users = await _context.Users.ToListAsync();
var userDtos = _mapper.Map<List<UserDto>>(users);

//  推荐：使用ProjectTo直接投影
var userDtos = await _context.Users
    .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

### 2. 分页查询优化

```csharp
//  先计数再查询
var totalCount = await query.CountAsync();
var items = await query
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

### 3. 计算属性优化

```csharp
//  使用只读计算属性
public decimal TotalPrice => SingleDosePrice * DosageCount;

// ❌ 避免存储冗余字段
// public decimal TotalPrice { get; set; } // 需要手动维护一致性
```

## 🔒 安全考虑

### 1. 密码安全

```csharp
//  创建DTO包含密码（仅传输一次）
public class UserCreateDto
{
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

//  查询DTO不包含密码
public class UserDto : StatusDto
{
    // ❌ 不包含 PasswordHash
    // ❌ 不包含 Salt
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
}
```

### 2. 敏感信息过滤

```csharp
//  根据角色过滤敏感字段
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // 敏感信息（仅管理员可见）
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdNumber { get; set; }
}
```

### 3. 输入验证防护

```csharp
//  严格的输入验证
public class UserCreateDto
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$")] // 防止SQL注入、XSS攻击
    public string Username { get; set; } = string.Empty;
}
```

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/shared/models/](../../../../docs/reference/modules/shared/models/) *(待创建)*
- **DTO设计规范**: [docs/explanation/architecture/shared/dto-design-standard.md](../../../../docs/explanation/architecture/shared/dto-design-standard.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/shared/dto-development.md](../../../../docs/how-to-guides/shared/dto-development.md) *(待创建)*

---

> 📌 **最新成果**: DTO三阶段优化完成，类型安全和代码质量大幅提升（16目录/70文件）
> 🎆 **生产就绪**: 完整的DTO体系，支撑整个系统的数据传输（5个核心接口 + 6个基类 + 8个业务模块DTO）
> **最后更新**: 2025-10-29
> **维护负责**: Shared层开发组

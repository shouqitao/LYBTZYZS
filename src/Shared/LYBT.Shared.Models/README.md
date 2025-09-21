# LYBT.Shared.Models

> **共享数据模型库** - .NET 8 DTO与契约定义
> DTO优化完成 | 类型安全 | 前后端一致 | 验证增强
> **模块状态**: ✅ **生产就绪** | 🎆 **DTO三阶段优化完成** | **零编译错误** | **2025-09-20更新**

## 🎯 项目概述

LYBT.Shared.Models 是系统核心数据模型项目，定义了前后端共享的数据传输对象(DTO)、实体模型、枚举类型、异常类和扩展方法。完成了DTO三阶段系统性优化，实现了完全类型安全的数据契约。

**技术栈**: .NET 8 + System.Text.Json + FluentValidation + DataAnnotations
**架构模式**: 分层DTO设计 + 继承优化 + 接口驱动
**最新成就**: DTO优化三阶段全部完成，代码质量显著提升

## 🎆 DTO优化三阶段成果

### 第一阶段：查询命名标准化 ✅
- 统一查询DTO命名模式：`QueryDto`（基础）和`SearchDto`（高级）
- 所有查询DTO继承`PagedQueryBaseDto`基类
- 统一分页、排序、关键词搜索参数

### 第二阶段：操作结果基类抽取 ✅
- 创建`StatusDto`基类（BaseDto + Status字段）
- 所有业务DTO继承自`StatusDto`
- 统一状态管理模式（CommonStatus枚举）

### 第三阶段：继承层次优化 ✅
- 完全分离`CreateDto`和`UpdateDto`
- 提取`InputBaseDto`共享字段
- 实现`IIdentifiable<T>`接口
- 删除未使用的字段和过时代码

## 📦 项目结构

```
LYBT.Shared.Models/
├── Common/                           # 通用模型
│   ├── BaseDto.cs                   # 基础DTO（包含Id）
│   ├── StatusDto.cs                 # 状态DTO（BaseDto + Status）
│   ├── PagedQueryBaseDto.cs         # 分页查询基类
│   ├── ApiResponse.cs               # 统一API响应格式
│   ├── ServiceResult.cs             # 服务层结果包装
│   ├── PagedResult.cs               # 分页结果模型
│   └── IIdentifiable.cs             # ID接口定义
├── Contracts/                        # 数据传输对象(DTO)
│   ├── Common/                       # 通用契约
│   │   ├── IRemarkable.cs           # 备注接口
│   │   └── BatchOperationDto.cs     # 批量操作DTO
│   ├── Auth/                         # 认证相关DTO
│   │   └── AuthDtos.cs              # 完整认证DTO集合
│   ├── Users/                        # 用户相关DTO（优化完成）
│   │   └── UserDtos.cs              # UserDto, UserCreateDto, UserUpdateDto, UserSearchDto
│   ├── Patients/                     # 患者相关DTO（优化完成）
│   │   └── PatientDtos.cs           # PatientDto, PatientCreateDto, PatientUpdateDto, PatientSearchDto
│   ├── MedicalCase/                  # 医案相关DTO（优化完成）
│   │   └── MedicalCaseDtos.cs       # MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseSearchDto
│   ├── Consultation/                 # 诊疗相关DTO（优化完成）
│   │   └── ConsultationDtos.cs      # ConsultationDetailDto, ConsultationCreateDto, ConsultationSearchDto
│   ├── Prescriptions/                # 处方相关DTO（优化完成）
│   │   ├── PrescriptionDtos.cs      # PrescriptionDto, PrescriptionCreateDto, PrescriptionSearchDto
│   │   └── PrescriptionCalculationDto.cs # 处方计算DTO
│   ├── Herbs/                        # 药材相关DTO（优化完成）
│   │   └── HerbDtos.cs              # HerbDto, HerbCreateDto, HerbUpdateDto, HerbSearchDto
│   └── Formula/                      # 验方相关DTO（优化完成）
│       └── FormulaDtos.cs           # FormulaDto, FormulaCreateDto, FormulaSearchDto
├── Enums/                           # 枚举定义
│   ├── CommonStatus.cs              # 通用状态（Enabled/Disabled）
│   ├── UserRole.cs                  # 用户角色（Doctor/Admin）
│   ├── Gender.cs                    # 性别枚举
│   └── MedicalCaseStatus.cs         # 医案状态
├── Exceptions/                       # 异常定义
│   ├── AppException.cs              # 应用异常基类
│   ├── BusinessException.cs         # 业务异常
│   ├── ValidationException.cs       # 验证异常
│   └── NotFoundException.cs         # 资源未找到异常
└── Extensions/                       # 扩展方法
    ├── EnumExtensions.cs            # 枚举扩展
    ├── DateTimeExtensions.cs        # 日期时间扩展
    └── StringExtensions.cs          # 字符串扩展
```

## 🎯 核心DTO层次结构

### 基类继承体系

```csharp
// 1. 基础DTO - 所有DTO的根基类
public abstract class BaseDto : IIdentifiable<Guid>
{
    [DisplayName("ID")]
    public Guid Id { get; set; }
}

// 2. 状态DTO - 添加状态管理
public abstract class StatusDto : BaseDto
{
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

// 3. 分页查询基类 - 统一查询参数
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
```

## 📚 模块DTO设计示例

### 用户模块（完整优化示例）

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

### 处方模块（计算属性示例）

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

## 🔧 通用响应格式

### ApiResponse<T> - API统一响应

```csharp
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

    // 静态工厂方法
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

### ServiceResult<T> - 服务层结果

```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }

    // 兼容性属性
    public string? Message => ErrorMessage;

    // 静态工厂方法
    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

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

## 📊 枚举定义

### 核心业务枚举

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
```

## 🚀 使用示例

### 控制器使用

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController
{
    // 分页查询
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] UserSearchDto searchDto)
    {
        var result = await _userService.GetPagedAsync(searchDto);

        if (result.IsSuccess)
            return Ok(ApiResponse<PagedResult<UserDto>>.CreateSuccess(result.Data));

        return BadRequest(ApiResponse<PagedResult<UserDto>>.CreateFailure(result.ErrorMessage));
    }

    // 创建用户
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] UserCreateDto createDto)
    {
        var result = await _userService.CreateAsync(createDto);

        if (result.IsSuccess)
            return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data, "用户创建成功"));

        return BadRequest(ApiResponse<UserDto>.CreateFailure(result.ErrorMessage));
    }

    // 更新用户
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

### 服务层使用

```csharp
public class UserService : IUserService
{
    // 分页查询
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
}
```

## 🎯 最佳实践

### 1. DTO设计原则
- ✅ 使用继承优化代码复用
- ✅ 分离创建和更新DTO
- ✅ 使用DataAnnotations验证
- ✅ 提供计算属性而非冗余字段
- ✅ 使用可空类型表示可选字段

### 2. 命名规范
- ✅ DTO后缀：`UserDto`
- ✅ 创建DTO：`UserCreateDto`
- ✅ 更新DTO：`UserUpdateDto`
- ✅ 查询DTO：`UserQueryDto`（基础）、`UserSearchDto`（高级）

### 3. 验证规则
- ✅ 使用DataAnnotations属性
- ✅ 提供清晰的错误消息
- ✅ 前后端验证保持一致
- ✅ 使用FluentValidation处理复杂验证

### 4. JSON序列化
- ✅ 使用camelCase命名
- ✅ 枚举序列化为字符串
- ✅ 忽略null值
- ✅ 使用UTC时间

## 📈 性能优化

- **轻量级DTO**: 仅包含必要字段
- **计算属性**: 避免存储冗余数据
- **延迟加载**: 导航属性按需加载
- **投影查询**: 使用AutoMapper投影优化查询

## 🔒 安全考虑

- **密码处理**: 密码字段仅在创建/修改时传输
- **敏感信息**: 避免在DTO中暴露敏感数据
- **权限控制**: 根据角色过滤返回字段
- **输入验证**: 严格的输入验证防止注入

---

> 📌 **最新成果**: DTO三阶段优化完成，类型安全和代码质量大幅提升
> 🎆 **生产就绪**: 完整的DTO体系，支撑整个系统的数据传输
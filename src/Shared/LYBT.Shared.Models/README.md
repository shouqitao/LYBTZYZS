# LYBT.Shared.Models

凌隐宝堂中医诊所系统 - 共享数据模型项目

## 项目概述

这是系统的核心数据模型项目，定义了前后端共享的数据传输对象(DTO)、实体模型、枚举类型、异常类和扩展方法。作为整个系统的数据契约基础，确保前后端数据结构的一致性和类型安全。

## 目录结构

```
LYBT.Shared.Models/
├── Base/                              # 基础模型定义
├── Common/                           # 通用模型和工具类
│   ├── BaseModel.cs                  # 基础模型抽象类
│   ├── BatchIdsDto.cs                # 批量ID操作DTO
│   ├── BatchOperationDto.cs          # 批量操作DTO
│   ├── EnumItem.cs                   # 枚举项展示模型
│   └── NullableEnumItem.cs           # 可空枚举项模型
├── Constants/                        # 系统常量定义
│   └── SystemConstants.cs            # 系统级常量
├── Contracts/                        # 数据传输对象(DTO)
│   ├── Auth/                         # 认证相关DTO
│   ├── Common/                       # 通用DTO和响应格式
│   ├── Configuration/                # 配置相关DTO
│   ├── Consultation/                 # 诊断相关DTO
│   ├── Formula/                      # 验方相关DTO
│   ├── Herbs/                        # 中药材相关DTO
│   ├── MedicalCase/                  # 医疗案例相关DTO
│   ├── Patients/                     # 患者相关DTO
│   ├── Prescriptions/                # 处方相关DTO
│   └── Users/                        # 用户相关DTO
├── Core/                             # 核心业务模型
│   └── BaseAuthSession.cs            # 基础认证会话模型
├── Enums/                           # 枚举定义
│   ├── AuthEnums.cs                  # 认证相关枚举
│   ├── Gender.cs                     # 性别枚举
│   ├── LogEnums.cs                   # 日志相关枚举
│   ├── MedicalCaseEnums.cs          # 医疗案例枚举
│   ├── PatientStatus.cs              # 患者状态枚举
│   ├── PrescriptionStatus.cs         # 处方状态枚举
│   ├── RecordEnums.cs                # 记录相关枚举
│   └── SystemEnums.cs                # 系统级枚举
├── Exceptions/                       # 异常定义
│   ├── ApiException.cs               # API异常
│   ├── AppException.cs               # 应用程序异常基类
│   ├── BusinessException.cs          # 业务异常
│   ├── ExceptionFactory.cs           # 异常工厂
│   ├── NotFoundException.cs          # 资源未找到异常
│   └── ValidationException.cs        # 验证异常
└── Extensions/                       # 扩展方法
    ├── DateTimeExtensions.cs         # 日期时间扩展
    ├── EnumExtensions.cs             # 枚举扩展方法
    ├── ServiceResultExtensions.cs    # 服务结果扩展
    └── StringExtensions.cs           # 字符串扩展方法
```

## 核心功能

### 1. 通用响应格式 (Contracts/Common)

#### ApiResponse<T> - 统一API响应格式
系统所有API端点使用的标准响应格式：

```csharp
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;
}
```

**响应示例**:
```json
{
    "success": true,
    "message": "操作成功",
    "data": { "id": "123", "name": "患者张三" },
    "timestamp": 1704067200,
    "requestId": "req-123456"
}
```

#### ServiceResult<T> - 服务层结果包装
前端业务服务使用的结果包装器：

```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public string? Message => ErrorMessage; // 兼容性属性
}
```

#### PagedResult<T> - 分页结果
支持分页查询的统一结果格式：

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### 2. 核心业务枚举 (Enums/)

#### UserRole - 用户角色枚举
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    [Description("管理员")]
    Admin = 0,
    
    [Description("医生")]
    Doctor = 1,
    
    [Description("药师")]
    Pharmacist = 2,
    
    [Description("前台")]
    Receptionist = 3,
    
    [Description("收银员")]
    Cashier = 4,
    
    [Description("理疗师")]
    Therapist = 5
}
```

#### Gender - 性别枚举
```csharp
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

#### CommonStatus - 通用状态枚举
```csharp
public enum CommonStatus
{
    [Description("禁用")]
    Disabled = 0,
    
    [Description("启用")]
    Enabled = 1
}
```

### 3. 业务数据传输对象

#### 认证模块 (Contracts/Auth)
- **LoginRequest**: 登录请求参数
- **LoginResponse**: 登录响应，包含JWT令牌
- **ChangePasswordRequest**: 修改密码请求
- **ChangeSysAdminPassword**: 系统管理员密码修改
- **LogoutRequest**: 登出请求

#### 用户管理 (Contracts/Users)
- **UserDto**: 用户基本信息展示
- **UserCreateDto**: 用户创建参数
- **UserUpdateDto**: 用户更新参数
- **UserSearchDto**: 用户搜索条件
- **UserOperationDto**: 用户操作相关DTO

#### 患者管理 (Contracts/Patients)
- **PatientDto**: 患者基本信息
- **PatientCreateDto**: 患者创建参数
- **PatientUpdateDto**: 患者更新参数
- **PatientSearchDto**: 患者搜索条件
- **PatientStatisticsDto**: 患者统计信息

#### 医疗诊断 (Contracts/Consultation)
- **ConsultationDto**: 诊断记录详情
- **ConsultationCreateDto**: 创建诊断记录
- **ConsultationUpdateDto**: 更新诊断记录
- **ConsultationOperationDto**: 诊断操作DTO

#### 处方管理 (Contracts/Prescriptions)
- **PrescriptionDto**: 处方详细信息
- **PrescriptionCreateDto**: 处方创建参数
- **PrescriptionCalculationDto**: 处方价格计算
- **PrescriptionItemDto**: 处方明细项

#### 中药材管理 (Contracts/Herbs)
- **HerbDto**: 中药材基本信息
- **HerbCreateDto**: 中药材创建参数
- **HerbUpdateDto**: 中药材更新参数
- **HerbOperationDto**: 中药材操作DTO

#### 验方管理 (Contracts/Formula)
- **FormulaDto**: 验方基本信息
- **FormulaCreateDto**: 验方创建参数
- **FormulaAnalysisDto**: 验方分析数据
- **FormulaItemDto**: 验方药材明细

### 4. 异常处理体系 (Exceptions/)

#### 异常层级结构
```
Exception (System)
└── AppException (基础应用异常)
    ├── BusinessException (业务逻辑异常)
    ├── ValidationException (数据验证异常)  
    ├── NotFoundException (资源未找到)
    └── ApiException (API调用异常)
```

#### BusinessException - 业务异常
用于业务规则验证失败的场景：
```csharp
public class BusinessException : AppException
{
    public string? BusinessRule { get; set; }
    
    public BusinessException(string message, string businessRule) : base(message)
    {
        BusinessRule = businessRule;
        ShowDetailToUser = true; // 业务异常通常需要显示给用户
    }
}
```

#### ValidationException - 验证异常  
用于数据格式或约束验证失败：
```csharp
public class ValidationException : AppException
{
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    
    public ValidationException(string message) : base(message)
    {
        ShowDetailToUser = true;
    }
}
```

#### ExceptionFactory - 异常工厂
提供统一的异常创建方法：
```csharp
public static class ExceptionFactory
{
    public static BusinessException CreateBusinessException(string message, string rule = "")
    public static ValidationException CreateValidationException(string field, string message)
    public static NotFoundException CreateNotFoundException(string resourceType, object id)
}
```

### 5. 扩展方法库 (Extensions/)

#### EnumExtensions - 枚举扩展
```csharp
public static class EnumExtensions
{
    // 获取枚举的Description特性值
    public static string GetDescription(this Enum enumValue)
    
    // 获取所有枚举值的键值对
    public static List<KeyValuePair<TEnum, string>> GetKeyValuePairs<TEnum>()
}
```

#### DateTimeExtensions - 日期扩展
```csharp
public static class DateTimeExtensions
{
    // 转换为Unix时间戳
    public static long ToUnixTimestamp(this DateTime dateTime)
    
    // 从Unix时间戳转换
    public static DateTime FromUnixTimestamp(long timestamp)
    
    // 友好的时间显示
    public static string ToFriendlyString(this DateTime dateTime)
}
```

#### StringExtensions - 字符串扩展
```csharp
public static class StringExtensions
{
    // 安全截取字符串
    public static string SafeSubstring(this string str, int maxLength)
    
    // 检查是否为有效的中文姓名
    public static bool IsValidChineseName(this string name)
    
    // 生成拼音首字母
    public static string ToPinyinInitials(this string chineseText)
}
```

### 6. 系统常量 (Constants/)

#### SystemConstants - 系统级常量
```csharp
public static class SystemConstants
{
    // API相关常量
    public const string API_VERSION = "v1";
    public const int DEFAULT_PAGE_SIZE = 20;
    public const int MAX_PAGE_SIZE = 100;
    
    // JWT相关常量
    public const int JWT_EXPIRE_HOURS = 8;
    public const int REMEMBER_ME_DAYS = 30;
    
    // 文件上传常量
    public const int MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    public const string[] ALLOWED_IMAGE_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".gif" };
    
    // 业务常量
    public const decimal MIN_PRESCRIPTION_AMOUNT = 0.01m;
    public const int MAX_PRESCRIPTION_DAYS = 30;
}
```

## 数据模型设计原则

### 1. 一致性原则
- **命名规范**: 所有DTO使用Pascal命名法，属性名与数据库字段对应
- **时间字段**: 统一使用`DateTime`类型，UTC时间存储
- **ID字段**: 统一使用`Guid`类型作为主键
- **状态字段**: 使用枚举类型，避免魔法数字

### 2. 验证约束
```csharp
public class UserCreateDto : DtoBase
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "真实姓名长度必须在2-20个字符之间")]
    public string RealName { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
    
    [Phone(ErrorMessage = "电话号码格式不正确")]
    public string? PhoneNumber { get; set; }
}
```

### 3. JSON序列化配置
```csharp
[JsonPropertyName("username")]        // 小写驼峰命名
[JsonConverter(typeof(JsonStringEnumConverter))]  // 枚举字符串序列化
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]  // 忽略null值
```

## 使用示例

### API响应处理
```csharp
// 控制器中创建成功响应
public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
{
    var user = await _userService.GetByIdAsync(id);
    return ApiResponse<UserDto>.CreateSuccess(user, "获取用户信息成功");
}

// 前端处理API响应
var response = await _userApi.GetUserAsync(userId);
if (response.Success)
{
    var userData = response.Data;
    DisplayUserInfo(userData);
}
else
{
    ShowError(response.Message);
}
```

### 服务层结果处理
```csharp
// 服务层返回结果
public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
{
    try
    {
        var user = await _repository.CreateAsync(dto);
        return ServiceResult<User>.Success(user);
    }
    catch (ValidationException ex)
    {
        return ServiceResult<User>.Failure(ex.Message, ex);
    }
}

// 调用服务并处理结果
var result = await _userService.CreateUserAsync(createDto);
if (result.IsSuccess)
{
    await ShowSuccessMessage("用户创建成功");
}
else
{
    await ShowError(result.ErrorMessage);
}
```

### 分页数据处理
```csharp
// 控制器中分页查询
public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] UserSearchDto searchDto)
{
    var pagedResult = await _userService.GetPagedUsersAsync(searchDto);
    return ApiResponse<PagedResult<UserDto>>.CreateSuccess(pagedResult);
}

// 前端分页处理
var searchRequest = new UserSearchDto 
{ 
    PageNumber = 1, 
    PageSize = 20, 
    Keyword = "张三" 
};
var response = await _userApi.GetUsersAsync(searchRequest);
if (response.Success)
{
    var pagedData = response.Data;
    UpdateUserList(pagedData.Items);
    UpdatePagination(pagedData.TotalCount, pagedData.PageNumber, pagedData.PageSize);
}
```

### 异常处理
```csharp
// 抛出业务异常
if (user.Status == CommonStatus.Disabled)
{
    throw new BusinessException("用户已被禁用，无法执行此操作", "UserDisabled");
}

// 抛出验证异常
if (!IsValidPhoneNumber(dto.PhoneNumber))
{
    throw new ValidationException("电话号码格式不正确");
}

// 统一异常处理
try
{
    await _userService.UpdateUserAsync(userId, updateDto);
}
catch (BusinessException ex)
{
    await ShowWarning(ex.Message);
}
catch (ValidationException ex)
{
    await ShowValidationErrors(ex.ValidationErrors);
}
```

## 技术特性

### 现代化C#语法支持
- **记录类型**: 使用`record`定义不可变DTO
- **可空引用类型**: 启用nullable reference types
- **模式匹配**: 枚举和状态判断使用模式匹配
- **集合表达式**: 使用`[]`初始化集合

### JSON序列化优化
- **System.Text.Json**: 使用现代JSON序列化器
- **驼峰命名**: API返回字段使用camelCase命名
- **枚举处理**: 枚举序列化为字符串而非数字
- **日期格式**: 统一的ISO 8601日期格式

### 国际化支持
- **多语言异常消息**: 支持中英文错误消息
- **本地化枚举**: 枚举Description支持多语言
- **文化相关格式**: 日期、数字格式本地化

## 开发指南

### 添加新的DTO
1. **选择合适的命名空间**: 根据业务模块放置DTO文件
2. **继承基类**: 继承`DtoBase`或合适的基础类
3. **添加验证特性**: 使用`DataAnnotations`添加验证
4. **JSON配置**: 配置JSON序列化属性
5. **XML文档**: 为所有公共成员添加XML注释

### 异常处理最佳实践
1. **选择合适异常类型**: 根据错误性质选择异常类型
2. **提供详细信息**: 包含错误代码和用户友好消息
3. **避免敏感信息泄露**: 生产环境隐藏技术细节
4. **使用异常工厂**: 统一异常创建方式

### 版本兼容性
1. **向前兼容**: 新增字段使用可选属性
2. **弃用处理**: 使用`[Obsolete]`标记过时成员
3. **版本标记**: 使用命名空间区分版本
4. **迁移支持**: 提供版本间数据迁移方法

## 测试支持

### 单元测试辅助
```csharp
// 测试数据构建器
public class UserDtoBuilder
{
    public static UserDto CreateSampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "testuser",
        RealName = "测试用户",
        Role = "Doctor",
        Status = CommonStatus.Enabled
    };
}

// 异常测试
[Test]
public void BusinessException_Should_SetShowDetailToUser_True()
{
    var exception = new BusinessException("业务错误");
    Assert.IsTrue(exception.ShowDetailToUser);
}
```

## 相关文档

- [LYBT.Shared.Interfaces](../LYBT.Shared.Interfaces/README.md) - 共享接口定义
- [LYBT.Shared.Utilities](../LYBT.Shared.Utilities/README.md) - 共享工具类库
- [API规范文档](../../docs/api/api-standards.md) - API设计标准
- [数据模型设计指南](../../docs/guides/data-model-design-guide.md) - 模型设计规范

---

**项目状态**: ✅ 生产就绪 | **最后更新**: 2025-01-01
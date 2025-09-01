# LYBT.Shared.Models 共享数据模型项目文档

## 项目概览

**项目名称**: LYBT.Shared.Models  
**项目类型**: 共享数据模型库  
**技术框架**: .NET 8.0  
**业务领域**: API契约和数据传输对象定义  
**更新时间**: 2025-01-01

## 项目定位

### 核心功能
LYBT.Shared.Models是整个系统的数据契约核心，负责定义前后端交互的所有数据模型：

1. **API响应包装**: 统一的API响应格式定义
2. **业务数据传输对象**: 8个业务模块的完整DTO定义
3. **服务结果封装**: 业务层统一的结果返回模型
4. **分页数据模型**: 标准化的分页查询和响应
5. **枚举和常量**: 系统级枚举和常量定义
6. **异常处理模型**: 标准化异常和错误处理
7. **扩展方法库**: 常用数据类型扩展

### 架构角色
- **数据契约中心**: 定义前后端API交互标准
- **类型安全保障**: 强类型数据传输和验证
- **业务模型抽象**: 跨层数据传输对象定义
- **序列化标准**: JSON序列化和数据转换规范

## 技术架构

### 核心依赖
```xml
<PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
<PackageReference Include="System.Text.Json" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

## 核心模型定义

### API响应包装器

#### ApiResponse&lt;T&gt;
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
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    // 静态工厂方法
    public static ApiResponse<T> CreateSuccess(T? data = default, string message = "操作成功");
    public static ApiResponse<T> CreateFail(string message = "操作失败", object? errors = null);
    public static ApiResponse<T> Ok(T? data = default, string message = "操作成功");
    public static ApiResponse<T> Fail(string message = "操作失败", string? errorCode = null);
}
```

#### 非泛型ApiResponse
```csharp
public class ApiResponse : ApiResponse<object>
{
    // 继承泛型版本，提供便捷的非泛型操作
    public static new ApiResponse CreateSuccess(object? data = null, string message = "操作成功");
    public static new ApiResponse CreateFail(string message = "操作失败", object? errors = null);
    public static ApiResponse Ok(string message = "操作成功");
    public static new ApiResponse Fail(string message = "操作失败", string? errorCode = null);
}
```

### 服务结果封装

#### ServiceResult&lt;T&gt;
```csharp
public class ServiceResult<T>
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>响应数据</summary>
    public T? Data { get; set; }

    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>异常信息（可选）</summary>
    public Exception? Exception { get; set; }

    /// <summary>消息 - 兼容性属性，返回ErrorMessage</summary>
    public string? Message => ErrorMessage;

    // 静态工厂方法
    public static ServiceResult<T> Success(T data);
    public static ServiceResult<T> Failure(string errorMessage, Exception? exception = null);
}
```

#### 非泛型ServiceResult
```csharp
public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public string? Message => ErrorMessage;

    public static ServiceResult Success();
    public static ServiceResult Success(string message);
    public static ServiceResult Failure(string errorMessage, Exception? exception = null);
}
```

### 分页数据模型

#### PagedResult&lt;T&gt;
```csharp
public class PagedResult<T>
{
    /// <summary>UltraThink统一构造函数</summary>
    public PagedResult() { }

    /// <summary>UltraThink统一构造函数 - 4参数版本</summary>
    public PagedResult(List<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items ?? new List<T>();
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
    }

    /// <summary>数据列表</summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>总记录数</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>当前页码</summary>
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    /// <summary>每页条数</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>总页数</summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    /// <summary>是否有上一页</summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>是否有下一页</summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>错误信息（用于传递API错误）</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    // UltraThink兼容性别名 - 确保架构统一
    /// <summary>数据兼容性别名</summary>
    [JsonIgnore]
    public List<T> Data { get => Items; set => Items = value; }
}
```

#### PagedQueryBaseDto
```csharp
public abstract class PagedQueryBaseDto
{
    /// <summary>页码</summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>每页大小</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>搜索关键词</summary>
    public string? Keyword { get; set; }

    /// <summary>排序字段</summary>
    public string? SortField { get; set; }

    /// <summary>是否降序</summary>
    public bool IsDescending { get; set; } = false;
}
```

## 业务数据传输对象

### 认证相关DTO

#### LoginRequest
```csharp
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
```

#### LoginResponse
```csharp
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto? User { get; set; }
}
```

#### ChangePasswordRequest
```csharp
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### 用户相关DTO

#### UserDto
```csharp
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}
```

#### UserDetailDto
```csharp
public class UserDetailDto : UserDto
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Remark { get; set; }
}
```

### 患者相关DTO

#### PatientDto
```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
    public string? Address { get; set; }
    public PatientStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}
```

### 其他业务模块DTO

项目包含完整的业务模块DTO定义：
- **Consultation**: 看诊相关数据传输对象
- **MedicalCase**: 医疗案例数据模型
- **Prescriptions**: 处方相关DTO
- **Herbs**: 中药材数据模型
- **Formula**: 验方模板数据传输对象

## 枚举定义

### CommonStatus
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommonStatus
{
    /// <summary>禁用</summary>
    [Description("禁用")]
    Disabled = 0,

    /// <summary>启用</summary>
    [Description("启用")]
    Enabled = 1
}
```

### 其他系统枚举
- **Gender**: 性别枚举
- **UserRole**: 用户角色定义
- **PatientStatus**: 患者状态
- **PrescriptionStatus**: 处方状态
- **MedicalCaseStatus**: 医疗案例状态
- **AuditStatus**: 审核状态
- **PaymentStatus**: 支付状态

## 异常处理模型

### AppException
```csharp
public class AppException : Exception
{
    public string ErrorCode { get; }
    public object? ErrorData { get; }

    public AppException(string message) : base(message)
    public AppException(string message, Exception innerException) : base(message, innerException)
    public AppException(string errorCode, string message) : base(message)
    public AppException(string errorCode, string message, object? errorData) : base(message)
}
```

### 特定异常类型
- **ApiException**: API调用异常
- **BusinessException**: 业务逻辑异常
- **ValidationException**: 数据验证异常
- **NotFoundException**: 资源未找到异常

## 扩展方法库

### DateTimeExtensions
```csharp
public static class DateTimeExtensions
{
    public static string ToChineseString(this DateTime dateTime);
    public static bool IsToday(this DateTime dateTime);
    public static bool IsWeekend(this DateTime dateTime);
    public static DateTime StartOfDay(this DateTime dateTime);
    public static DateTime EndOfDay(this DateTime dateTime);
}
```

### StringExtensions
```csharp
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? str);
    public static bool IsNotNullOrEmpty(this string? str);
    public static string SafeSubstring(this string str, int startIndex, int length);
    public static bool ContainsIgnoreCase(this string source, string value);
}
```

### ServiceResultExtensions
```csharp
public static class ServiceResultExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this ServiceResult<T> serviceResult);
    public static ApiResponse ToApiResponse(this ServiceResult serviceResult);
    public static ServiceResult<T> AsServiceResult<T>(this T data);
}
```

## 常量定义

### SystemConstants
```csharp
public static class SystemConstants
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int TokenExpiryHours = 8;
    public const int RefreshTokenExpiryDays = 30;
    
    // 业务常量
    public static class Business
    {
        public const int MaxPrescriptionItems = 20;
        public const decimal MaxHerbQuantity = 999.99m;
        public const int MaxPatientNameLength = 50;
    }
}
```

## 数据验证支持

### 验证特性
项目使用System.ComponentModel.Annotations进行数据验证：

```csharp
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码长度不能少于6位")]
    public string Password { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
}
```

## JSON序列化配置

### 序列化标准
使用System.Text.Json 9.0.7进行JSON序列化，配置特点：

1. **JsonPropertyName**: 统一使用camelCase命名
2. **JsonConverter**: 枚举使用字符串转换
3. **JsonIgnore**: 隐藏内部实现属性
4. **Nullable支持**: 完整的空值处理

```csharp
[JsonPropertyName("success")]
public bool Success { get; set; }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status { }

[JsonIgnore]
public string InternalProperty { get; set; }
```

## 性能优化

### 内存管理
1. **对象池化**: 常用DTO对象复用
2. **延迟初始化**: 集合属性按需创建
3. **字符串优化**: 使用string.Empty避免分配
4. **结构体优化**: 值类型数据使用struct

### 序列化优化
1. **预编译序列化器**: 使用Source Generator
2. **属性缓存**: JsonPropertyName预计算
3. **类型转换器**: 自定义高效转换器
4. **流式处理**: 大数据量分块处理

## 扩展指南

### 添加新的业务DTO
1. 在对应的Contracts目录创建DTO文件
2. 继承DtoBase基类（如适用）
3. 添加数据验证特性
4. 配置JSON序列化属性
5. 创建相关的查询DTO和操作DTO

### 扩展枚举定义
1. 在Enums目录添加新枚举
2. 使用JsonConverter和Description特性
3. 在EnumExtensions中添加扩展方法
4. 更新相关的DTO引用

### 添加新的异常类型
1. 继承AppException基类
2. 定义特定的错误代码
3. 添加构造函数重载
4. 在ExceptionFactory中注册

## 维护说明

### 版本兼容性
- 向前兼容：新属性使用可空类型
- API版本控制：通过命名空间区分版本
- 弃用标记：使用Obsolete特性标记过时API

### 测试支持
- 单元测试：每个DTO包含序列化测试
- 验证测试：数据注解验证测试
- 兼容性测试：版本升级兼容性验证

### 性能监控
- 序列化性能：监控JSON转换耗时
- 内存使用：监控DTO对象分配
- 网络传输：监控数据传输大小

---

**版本**: v1.0  
**维护**: 开发团队  
**更新**: 2025-01-01
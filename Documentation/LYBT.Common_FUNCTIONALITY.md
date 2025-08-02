# LYBT.Common 功能说明文档

## 模块概述
通用模块为整个系统提供基础的枚举定义、数据传输对象、扩展方法、工具类等共享功能。作为系统的"工具箱"，为所有业务模块提供统一的数据类型和通用功能。

## 核心组件

### 1. 枚举定义 (Enums)

#### 系统枚举 (System)
**文件位置**: `Enums/System/SystemEnums.cs`

##### Gender (性别)
```csharp
public enum Gender {
    Unknown = 0,    // 未知
    Male = 1,       // 男性
    Female = 2      // 女性
}
```
**使用场景**: 患者性别、医生性别、用户性别等

#### 用户相关枚举 (Users)
**文件位置**: `Enums/Users/`

##### UserRole (用户角色)
```csharp
public enum UserRole {
    Admin = 1,              // 系统管理员
    DiagnosingDoctor = 2,   // 诊疗医生
    Pharmacist = 3,         // 药剂师
    Receptionist = 4,       // 前台接待
    Nurse = 5              // 护士
}
```

##### UserStatus (用户状态)
```csharp
public enum UserStatus {
    Normal = 1,     // 正常
    Disabled = 2,   // 禁用
    Locked = 3      // 锁定
}
```

#### 患者相关枚举 (Patients)
**文件位置**: `Enums/Patients/PatientEnums.cs`

##### PatientStatus (患者状态)
```csharp
public enum PatientStatus {
    Normal = 1,     // 正常
    Disabled = 2,   // 禁用
    VIP = 3,        // VIP患者
    Blacklist = 4   // 黑名单
}
```

#### 医生相关枚举 (Doctors)
**文件位置**: `Enums/Doctors/DoctorEnums.cs`

##### DoctorTitle (医生职称)
```csharp
public enum DoctorTitle {
    Resident = 1,       // 住院医师
    Attending = 2,      // 主治医师
    Associate = 3,      // 副主任医师
    Chief = 4,          // 主任医师
    Professor = 5       // 教授
}
```

##### DoctorStatus (医生状态)
```csharp
public enum DoctorStatus {
    Active = 1,     // 在职
    Inactive = 2,   // 离职
    Suspended = 3,  // 停职
    Retired = 4     // 退休
}
```

#### 药材相关枚举 (Herbs)
**文件位置**: `Enums/Herbs/HerbStatus.cs`

##### HerbStatus (药材状态)
```csharp
public enum HerbStatus {
    Available = 1,      // 可用
    OutOfStock = 2,     // 缺货
    Expired = 3,        // 过期
    Discontinued = 4,   // 停用
    Reserved = 5        // 预留
}
```

#### 日志相关枚举 (Logs)
**文件位置**: `Enums/Logs/`

##### LogType (日志类型)
```csharp
public enum LogType {
    System = 1,         // 系统日志
    Operation = 2,      // 操作日志
    Error = 3,          // 错误日志
    Performance = 4,    // 性能日志
    Security = 5        // 安全日志
}
```

##### ActionType (操作类型)
```csharp
public enum ActionType {
    Create = 1,         // 创建
    Edit = 2,           // 编辑
    Delete = 3,         // 删除
    View = 4,           // 查看
    Enable = 5,         // 启用
    Disable = 6,        // 禁用
    Login = 7,          // 登录
    Logout = 8,         // 登出
    ResetPassword = 9,  // 重置密码
    Other = 99          // 其他
}
```

### 2. 响应模型 (Models)

#### ApiResponse (API响应)
**文件位置**: `Models/ApiSuccessResponse.cs`, `Responses/ApiResponse.cs`

```csharp
public class ApiResponse<T> {
    public bool Success { get; set; }           // 操作是否成功
    public string Message { get; set; }         // 响应消息
    public T Data { get; set; }                 // 响应数据
    public int Code { get; set; }               // 状态码
    public DateTime Timestamp { get; set; }     // 时间戳
    public string TraceId { get; set; }         // 追踪ID
}

// 静态工厂方法
public static ApiResponse<T> Success(T data, string message = "操作成功")
public static ApiResponse<T> Error(string message, int code = 500)
public static ApiResponse<T> NotFound(string message = "资源未找到")
public static ApiResponse<T> Unauthorized(string message = "未授权访问")
public static ApiResponse<T> BadRequest(string message = "请求参数错误")
```

**使用场景**: 所有API接口的标准返回格式

#### PagedResult (分页结果)
**文件位置**: `Models/PagedResult.cs`, `Models/PagedResultDto.cs`

```csharp
public class PagedResult<T> {
    public List<T> Items { get; set; }          // 数据列表
    public int TotalCount { get; set; }         // 总记录数
    public int PageIndex { get; set; }          // 当前页码
    public int PageSize { get; set; }           // 每页大小
    public int TotalPages { get; set; }         // 总页数
    public bool HasPreviousPage { get; set; }   // 是否有上一页
    public bool HasNextPage { get; set; }       // 是否有下一页
}
```

**使用场景**: 分页查询结果的标准返回格式

#### PaginationRequest (分页请求)
**文件位置**: `Models/PaginationRequest.cs`

```csharp
public class PaginationRequest {
    public int Page { get; set; } = 1;          // 页码（从1开始）
    public int PageSize { get; set; } = 20;     // 每页大小
    public string? SortField { get; set; }      // 排序字段
    public bool SortDescending { get; set; }    // 是否降序
}
```

**使用场景**: 分页查询请求的基类

#### EnumItem (枚举项)
**文件位置**: `Models/EnumItem.cs`

```csharp
public class EnumItem {
    public int Value { get; set; }              // 枚举值
    public string Name { get; set; }            // 枚举名称
    public string Description { get; set; }     // 枚举描述
    public string DisplayName { get; set; }     // 显示名称
}
```

**使用场景**: 前端下拉选择框、单选按钮等控件的数据源

### 3. 扩展方法 (Extensions)

#### EnumExtensions (枚举扩展)
**文件位置**: `Extensions/EnumExtensions.cs`

```csharp
// 获取枚举描述
public static string GetDescription(this Enum value)

// 获取枚举显示名称
public static string GetDisplayName(this Enum value)

// 转换为枚举项
public static EnumItem ToEnumItem(this Enum value)

// 获取所有枚举项
public static List<EnumItem> GetEnumItems<T>() where T : Enum
```

**使用场景**: 枚举值的显示转换和数据绑定

#### StringExtensions (字符串扩展)
**文件位置**: `Extensions/StringExtensions.cs`

```csharp
// 判断字符串是否为空或空白
public static bool IsNullOrWhiteSpace(this string value)

// 截取指定长度
public static string Truncate(this string value, int maxLength)

// 转换为拼音码
public static string ToPinyinCode(this string chineseText)

// 格式化手机号
public static string FormatPhoneNumber(this string phoneNumber)

// 脱敏处理
public static string Mask(this string value, int startIndex, int length, char maskChar = '*')
```

**使用场景**: 字符串处理和格式化

#### DateTimeExtensions (时间扩展)
**文件位置**: `Extensions/DateTimeExtensions.cs`

```csharp
// 转换为中文日期格式
public static string ToChineseDateString(this DateTime dateTime)

// 转换为相对时间描述
public static string ToRelativeString(this DateTime dateTime)

// 获取年龄
public static int GetAge(this DateTime birthDate)

// 判断是否为工作日
public static bool IsWorkDay(this DateTime dateTime)

// 获取季度
public static int GetQuarter(this DateTime dateTime)
```

**使用场景**: 时间格式化和计算

### 4. 工具类 (Helpers)

#### CommonHelper (通用工具)
**文件位置**: `Helpers/CommonHelper.cs`

```csharp
// 生成拼音码
public static string GetPinyinCode(string chineseText)

// 验证身份证号
public static bool CheckIdNumber(string idNumber)

// 验证手机号
public static bool CheckPhoneNumber(string phoneNumber)

// 生成随机字符串
public static string GenerateRandomString(int length)

// 计算年龄
public static int CalculateAge(DateTime birthDate)

// 格式化文件大小
public static string FormatFileSize(long bytes)
```

**使用场景**: 常用的数据验证和格式化

#### PasswordHelper (密码工具)
**文件位置**: `Helpers/PasswordHelper.cs`

```csharp
// 生成密码哈希
public static string Hash(string password)

// 验证密码
public static bool Verify(string hashedPassword, string providedPassword)

// 生成随机密码
public static string GenerateRandomPassword(int length = 8)

// 验证密码强度
public static bool ValidatePasswordStrength(string password)
```

**使用场景**: 用户密码的加密存储和验证

#### EnumHelper (枚举工具)
**文件位置**: `Helpers/EnumHelper.cs`

```csharp
// 获取枚举下拉数据源
public static List<EnumItem> GetEnumItems<T>() where T : Enum

// 根据值获取枚举描述
public static string GetEnumDescription<T>(int value) where T : Enum

// 根据名称获取枚举值
public static T? ParseEnum<T>(string name) where T : struct, Enum

// 获取枚举的所有值
public static List<T> GetEnumValues<T>() where T : Enum
```

**使用场景**: 枚举数据的处理和转换

#### LogHelper (日志工具)
**文件位置**: `Helpers/LogHelper.cs`

```csharp
// 生成操作描述
public static string GenerateActionDescription(ActionType actionType, string objectName)

// 格式化日志消息
public static string FormatLogMessage(string template, params object[] args)

// 获取调用者信息
public static string GetCallerInfo(string memberName, string filePath, int lineNumber)

// 序列化对象为日志字符串
public static string SerializeForLog(object obj)
```

**使用场景**: 日志记录的格式化和标准化

### 5. 常量定义 (Constants)

#### SystemConstants (系统常量)
**文件位置**: `Constants/SystemConstants.cs`

```csharp
public static class SystemConstants {
    // 系统配置
    public const string DefaultPassword = "123456";
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    
    // 文件上传
    public const int MaxFileSize = 10 * 1024 * 1024; // 10MB
    public static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif" };
    
    // 缓存键
    public const string UserCacheKeyPrefix = "user:";
    public const string ConfigCacheKeyPrefix = "config:";
    
    // 默认值
    public const string DefaultClinicName = "凌隐宝堂中医诊所诊疗系统";
    public const int DefaultSessionTimeout = 30; // 分钟
}
```

## 使用示例

### 枚举使用
```csharp
// 获取性别描述
var genderDesc = Gender.Male.GetDescription(); // "男性"

// 获取用户角色列表
var roleItems = EnumHelper.GetEnumItems<UserRole>();

// 在API中返回枚举选项
[HttpGet("gender-options")]
public ApiResponse<List<EnumItem>> GetGenderOptions() {
    var options = EnumHelper.GetEnumItems<Gender>();
    return ApiResponse.Success(options);
}
```

### API响应使用
```csharp
// 成功响应
var users = await _userService.GetUsersAsync();
return ApiResponse.Success(users, "获取用户列表成功");

// 错误响应
if (user == null) {
    return ApiResponse.NotFound("用户不存在");
}

// 分页响应
var pagedUsers = await _userService.GetPagedUsersAsync(query);
return ApiResponse.Success(pagedUsers);
```

### 扩展方法使用
```csharp
// 字符串处理
var phone = "13800138000".FormatPhoneNumber(); // "138****8000"
var pinyin = "张三".ToPinyinCode(); // "ZS"

// 时间处理
var age = birthDate.GetAge();
var relativeTime = DateTime.Now.ToRelativeString(); // "刚刚"、"3分钟前"

// 枚举处理
var statusDesc = PatientStatus.Normal.GetDescription(); // "正常"
```

### 工具类使用
```csharp
// 密码处理
var hashedPassword = PasswordHelper.Hash("123456");
var isValid = PasswordHelper.Verify(hashedPassword, inputPassword);

// 数据验证
var isValidId = CommonHelper.CheckIdNumber("110101199001011234");
var isValidPhone = CommonHelper.CheckPhoneNumber("13800138000");

// 拼音码生成
var pinyinCode = CommonHelper.GetPinyinCode("张三丰"); // "ZSF"
```

### 分页查询使用
```csharp
// 控制器中
[HttpGet]
public async Task<ApiResponse<PagedResult<UserDto>>> GetUsers([FromQuery] UserQueryDto query) {
    var result = await _userService.GetPagedUsersAsync(query);
    return ApiResponse.Success(result);
}

// 服务层中
public async Task<PagedResult<UserDto>> GetPagedUsersAsync(UserQueryDto query) {
    var (users, total) = await _repository.GetPagedAsync(query);
    
    return new PagedResult<UserDto> {
        Items = users.Select(MapToDto).ToList(),
        TotalCount = total,
        PageIndex = query.Page,
        PageSize = query.PageSize
    };
}
```

## 配置和约定

### 枚举约定
- 所有枚举都应该有明确的描述特性
- 状态类枚举从1开始，0通常表示未知或无效状态
- 枚举名称使用英文，描述使用中文

### 响应约定
- 成功响应统一使用ApiResponse.Success
- 错误响应根据HTTP状态码选择合适的方法
- 所有API都应该返回标准的ApiResponse格式

### 命名约定
- 扩展方法类名以Extensions结尾
- 工具类名以Helper结尾
- 常量类名以Constants结尾
- 枚举类型名使用单数形式

## 性能优化

### 枚举缓存
```csharp
// 枚举描述缓存，避免重复反射
private static readonly ConcurrentDictionary<Enum, string> DescriptionCache = new();

public static string GetDescription(this Enum value) {
    return DescriptionCache.GetOrAdd(value, v => {
        // 反射获取描述的逻辑
    });
}
```

### 字符串池
```csharp
// 常用字符串使用字符串池
public static class StringPool {
    public static readonly string Success = "操作成功";
    public static readonly string Failed = "操作失败";
    public static readonly string NotFound = "资源未找到";
}
```

## 测试支持

### 测试工具
```csharp
public static class TestHelper {
    // 生成测试数据
    public static UserDto CreateTestUser(string name = "测试用户") {
        return new UserDto {
            Id = Guid.NewGuid(),
            UserName = $"test_{Guid.NewGuid():N}",
            RealName = name,
            CreatedTime = DateTime.Now
        };
    }
    
    // 创建分页测试数据
    public static PagedResult<T> CreatePagedResult<T>(List<T> items, int total = 0) {
        return new PagedResult<T> {
            Items = items,
            TotalCount = total > 0 ? total : items.Count,
            PageIndex = 1,
            PageSize = items.Count
        };
    }
}
```

## 扩展建议

### 功能扩展
1. **国际化支持**: 为枚举描述添加多语言支持
2. **数据验证**: 添加更多的验证特性和验证器
3. **序列化支持**: 为特殊类型添加JSON序列化器
4. **缓存增强**: 添加更多的缓存工具和策略
5. **日志增强**: 添加结构化日志支持

### 性能优化
1. **内存优化**: 使用对象池减少GC压力
2. **序列化优化**: 使用更高效的序列化库
3. **反射缓存**: 缓存反射结果避免重复调用
4. **字符串优化**: 使用StringBuilder和字符串池
5. **并发优化**: 使用并发安全的集合类型

这个通用模块为整个系统提供了统一的基础类型和工具函数，确保了代码的一致性和可维护性，是系统架构的重要基石。
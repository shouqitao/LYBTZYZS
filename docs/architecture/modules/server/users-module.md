# Users模块设计 - Server端

## 📋 模块概述
**职责**：用户信息管理、角色权限、用户CRUD操作
**命名空间**：`LYBT.Module.Users`
**API路径**：`/api/v1/users/*`

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/           # HTTP控制器（位于WebAPI项目）
│   └── UsersController.cs
├── Services/             # 业务服务（简化架构）
│   └── UserService.cs       # 主服务（实现IUserService）
├── Repositories/         # 数据访问
│   └── UserRepository.cs    # 数据访问实现
├── Interfaces/           # 服务接口
│   ├── IUserService.cs      # 业务服务接口
│   └── IUserRepository.cs   # 仓储接口
├── Mapping/             # 对象映射
│   └── UserMappingProfile.cs
├── UsersModule.cs       # 模块注册
└── README.md
```

## 🔌 API接口设计

### GET /api/v1/users
**功能**：分页查询用户列表
```csharp
// Query Parameters
?page=1&pageSize=20&keyword=search&role=admin&status=active

// Response 200
{
  "items": [
    {
      "id": "guid",
      "username": "admin",
      "email": "admin@example.com",
      "role": "Administrator",
      "status": "Active",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20
}
```

### GET /api/v1/users/{id}
**功能**：获取用户详情
```csharp
// Response 200
{
  "id": "guid",
  "username": "admin",
  "email": "admin@example.com",
  "fullName": "System Administrator",
  "role": "Administrator",
  "status": "Active",
  "lastLoginAt": "2024-01-01T10:00:00Z",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### POST /api/v1/users
**功能**：创建新用户
```csharp
// Request
{
  "username": "newuser",
  "email": "newuser@example.com",
  "fullName": "New User",
  "password": "SecurePassword123!",
  "role": "Doctor",
  "status": "Active"
}

// Response 201
{
  "id": "new-guid",
  "username": "newuser",
  "email": "newuser@example.com",
  // ... other fields
}
```

### PUT /api/v1/users/{id}
**功能**：更新用户信息
```csharp
// Request
{
  "email": "updated@example.com",
  "fullName": "Updated Name",
  "role": "Nurse",
  "status": "Active"
}

// Response 200 - Updated user object
```

### DELETE /api/v1/users/{id}
**功能**：删除用户（软删除）
```csharp
// Response 204 No Content
```

## 🔧 核心服务

### UserService (业务服务)
**职责**：用户业务逻辑（简化版）
```csharp
public interface IUserService
{
    // 基础CRUD - 已实现
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```csharp
public interface IUserService
{
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

csharp
public interface IUserQueryService
{
    Task<PagedResult<UserDto>> GetPagedUsersAsync(UserSearchDto searchDto);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<List<UserDto>> GetActiveUsersAsync();
    Task<List<UserDto>> GetUsersByRoleAsync(string role);
}
```

### UserRepository (数据访问)
**职责**：数据库操作封装
```csharp
public interface IUserRepository
{
    Task<User> AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
    Task<List<User>> GetAllAsync();
}
```

## 📊 数据模型

### 核心实体 (User)
```csharp
[Table("Users")]
public class User : BaseEntity
{
    /// <summary>用户名（注意：实体中为UsernName，DTO中为UserName）</summary>
    [Required]
    [StringLength(50)]
    [Column("Username")]
    [DisplayName("用户名")]
    public string UsernName { get; set; } = string.Empty;  // 实体属性名

    /// <summary>真实姓名</summary>
    [Required]
    [StringLength(50)]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>电话号码</summary>
    [StringLength(20)]
    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>邮箱地址</summary>
    [StringLength(100)]
    [DisplayName("邮箱")]
    public string? Email { get; set; }

    /// <summary>用户角色</summary>
    [DisplayName("角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

    /// <summary>用户状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>密码哈希</summary>
    [Required]
    [StringLength(256)]
    [DisplayName("密码哈希")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>失败登录次数</summary>
    [DisplayName("失败登录次数")]
    public int FailedLoginCount { get; set; } = 0;

    /// <summary>锁定结束时间</summary>
    [DisplayName("锁定结束时间")]
    public DateTime? LockoutEnd { get; set; }

    /// <summary>最后登录时间</summary>
    [DisplayName("最后登录时间")]
    public DateTime? LastLoginTime { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    [StringLength(500)]
    public string? Remark { get; set; }
    
    // Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted等字段继承自BaseEntity
}
```csharp
public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    
    // Navigation Properties
    public List<RefreshToken> RefreshTokens { get; set; }
}
```

### DTO模型

#### UserDto - 用户信息传输对象
```csharp
public class UserDto : StatusDto
{
    /// <summary>用户名（DTO中的属性名）</summary>
    [DisplayName("用户名")]
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;  // DTO属性名

    /// <summary>真实姓名</summary>
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>用户角色</summary>
    [DisplayName("用户角色")]
    public UserRole Role { get; set; } = UserRole.Doctor;

    /// <summary>电话号码</summary>
    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>邮箱地址</summary>
    [DisplayName("邮箱地址")]
    public string? Email { get; set; }

    /// <summary>拼音码</summary>
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>账号启用状态</summary>
    [DisplayName("账号启用状态")]
    public bool IsActive => Status == CommonStatus.Enabled;
}
```

#### UserCreateDto - 用户创建请求
```csharp
public class UserCreateDto : UserInputBaseDto
{
    /// <summary>用户名（创建DTO中的属性名）</summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
    [DisplayName("用户名")]
    public string Username { get; set; } = string.Empty;  // 创建DTO属性名

    /// <summary>密码</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
    [DisplayName("密码")]
    public string Password { get; set; } = string.Empty;

    /// <summary>确认密码</summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [DisplayName("确认密码")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### 枚举定义
```csharp
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3,
    Suspended = 4
}

public enum UserRole
{
    SystemAdmin = 1,
    Administrator = 2,
    Doctor = 3,
    Nurse = 4,
    Pharmacist = 5,
    Receptionist = 6
}
```

## 🛡️ 验证与安全

### 数据验证
```csharp
public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 32).WithMessage("用户名长度必须在3-32个字符之间")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(8).WithMessage("密码长度至少8位")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("密码必须包含大小写字母、数字和特殊字符");
    }
}
```

### 权限控制
- 系统管理员：完整用户管理权限
- 普通管理员：受限用户管理权限  
- 其他角色：只能查看和修改自己的信息

### 超级管理员保护机制

#### 1. 用户名冲突预防
系统自动防止创建与超级管理员相同的用户名：
```csharp
// UserService.CreateAsync 中的保护逻辑
var sysAdminUsername = _configuration["Lybt:Business:SystemAdmin:Username"] ?? "clinic_admin";
if (string.Equals(dto.Username, sysAdminUsername, StringComparison.OrdinalIgnoreCase))
{
    return ServiceResult<UserDto>.Failure($"用户名 '{dto.Username}' 为系统保留用户名");
}
```

#### 2. 保留用户名列表
```csharp
private static readonly HashSet<string> ReservedUsernames = new()
{
    "admin", "administrator", "root", 
    "system", "superadmin", "sysadmin"
};
```

#### 3. 超级管理员隔离
- **数据隔离**：超级管理员不存在于 Users 表中
- **认证隔离**：通过 AdminSecrets 表进行认证
- **配置驱动**：用户名从配置文件读取，不存储在数据库

## 📝 配置管理

### UserModuleOptions
```csharp
public class UserModuleOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public bool EnableCache { get; set; } = false;
    public int CacheExpirationMinutes { get; set; } = 10;
    public bool RequireEmailVerification { get; set; } = false;
    public bool AllowSelfRegistration { get; set; } = false;
}
```

## 📋 实现状态

### ✅ 已实现
- **UserService基础CRUD** - 完整的用户增删改查操作
- **UserRepository数据访问** - 基础数据访问层实现
- **AutoMapper映射配置** - 实体与DTO间的映射
- **用户搜索功能** - 支持关键词搜索
- **分页查询功能** - 支持分页查询用户列表
- **数据验证器** - UserCreateDto和UserUpdateDto的验证规则

### ⚠️ 需要修复的问题
- **属性命名不一致** - User实体中为`UsernName`，DTO中为`UserName`/`Username`
- **UserSearchDto缺少Page属性** - 分页查询可能有问题
- **IUserRepository部分方法未实现** - 如GetByUsernameAsync、GetByEmailAsync等

### 🔄 部分实现
- **UsersController** - 基础API已实现，功能相对完整
- **权限控制** - 基础框架存在，需要完善具体权限逻辑
- **密码管理** - 基础加密存在，缺少密码策略验证

### ❌ 待实现
- **邮箱验证功能** - 用户注册时的邮箱验证
- **用户头像上传** - 用户头像管理功能
- **批量用户操作** - 批量启用/禁用用户
- **用户活动日志** - 用户操作行为记录
- **用户个人资料管理** - 用户自主修改个人信息
- **密码重置** - 忘记密码的重置流程

## 🧪 测试覆盖

### 单元测试
- UserService业务逻辑测试
- UserRepository数据访问测试
- 验证器规则测试
- AutoMapper映射测试

### 集成测试
- 用户API端到端测试
- 权限控制测试
- 并发操作测试

## 🔗 依赖关系

### 依赖组件
- **Infrastructure** - 数据库上下文
- **Shared.Models** - DTO定义
- **Shared.Utilities** - 通用工具

### 被依赖模块
- **Auth模块** - 用户认证
- **所有业务模块** - 创建人/修改人字段

## 📈 性能优化

### 查询优化
- 使用AsNoTracking()进行只读查询
- 索引优化：Username, Email字段
- 分页查询避免COUNT(*)

### 缓存策略
- 用户基本信息缓存
- 角色权限信息缓存
- 活跃用户列表缓存

## 🔍 监控与健康检查

### 健康检查指标
- 用户表连接状态
- 种子用户数据完整性
- 缓存服务可用性

### 监控指标
- 用户注册量
- 登录成功率
- 用户活跃度
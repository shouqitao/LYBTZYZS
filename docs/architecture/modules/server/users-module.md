# Users模块设计 - Server端

## 📋 模块概述
**职责**：用户信息管理、角色权限、用户CRUD操作
**命名空间**：`LYBT.Module.Users`
**API路径**：`/api/v1/users/*`

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/           # HTTP控制器
│   └── UsersController.cs
├── Services/             # 业务服务
│   ├── UserService.cs
│   └── UserQueryService.cs
├── Repositories/         # 数据访问
│   ├── UserRepository.cs
│   └── RefreshTokenRepository.cs
├── Interfaces/           # 服务接口
│   ├── IUserService.cs
│   ├── IUserQueryService.cs
│   └── IUserRepository.cs
├── Validators/           # 验证器
│   ├── UserCreateDtoValidator.cs
│   └── UserUpdateDtoValidator.cs
├── Mapping/             # 对象映射
│   └── UserMappingProfile.cs
├── Configuration/       # 配置选项
│   └── UserModuleOptions.cs
├── HealthChecks/        # 健康检查
│   └── UsersModuleHealthCheck.cs
└── UsersModule.cs       # 模块注册
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
**职责**：用户业务逻辑，写操作
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

### UserQueryService (查询服务)
**职责**：用户查询优化，只读操作
```csharp
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
- **UserDto** - 用户信息传输对象
- **UserCreateDto** - 创建用户请求
- **UserUpdateDto** - 更新用户请求
- **UserSearchDto** - 用户查询条件

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
- 用户CRUD完整实现
- 分页查询功能
- 数据验证器
- AutoMapper映射配置
- 基础权限控制

### ⚠️ 需要修复
- UserSearchDto.Page属性缺失
- IUserRepository部分方法未实现
- UserCreateDto.UserName属性不匹配

### 🔄 待实现
- 邮箱验证功能
- 用户头像上传
- 批量用户操作
- 用户活动日志

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
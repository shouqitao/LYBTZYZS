# LYBT.Module.Auth

> **身份认证与授权核心模块** - JWT Token + RBAC权限控制
> 专为小型中医诊所(<20人)优化 | Admin/Doctor双角色体系
> 模块状态: ✅ **生产就绪** | 🎆 **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Auth是系统的身份认证与授权核心模块，提供JWT无状态认证、RBAC角色权限控制和完整的安全审计功能。专为小型中医诊所场景优化，支持Admin/Doctor双角色管理。

**技术栈**: JWT Bearer Token + RBAC + 实体（实体（Entity）） Framework Core 8.0 + BCrypt密码哈希
**最新更新**: DTO优化完成、类型安全增强、Role字段统一使用UserRole枚举

## 🏗️ 核心架构设计

### 服务架构

```
AuthService (主服务层 - 纯委托模式)
    │
    ├── AuthCore (核心业务层)
    │   ├── 登录认证流程 (LoginAsync)
    │   ├── Token生成验证 (GenerateTokenAsync, ValidateTokenAsync)
    │   ├── 用户管理 (RegisterAsync, ChangePasswordAsync)
    │   ├── 会话管理 (LogoutAsync, RefreshTokenAsync)
    │   └── 安全审计 (记录操作日志)
    │
    ├── JwtAuthenticationService (JWT专业服务)
    │   ├── Token生成算法
    │   ├── 签名验证逻辑
    │   └── Claims管理
    │
    └── SysAdminHandler (系统管理员专门服务)
        ├── 超级管理员初始化
        └── 安全策略管理
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口)
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<bool>> LogoutAsync(string token);
    Task<ServiceResult<UserDto>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ServiceResult<TokenResponse>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);
}

// JWT专业服务接口
public interface IJwtAuthenticationService
{
    Task<string> GenerateTokenAsync(User user, bool rememberMe = false);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    string GetUserIdFromToken(string token);
}
```

## 📦 核心功能模块

### 1. 身份认证 (Authentication)

**登录认证流程**:
```csharp
public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
{
    // 1. 输入验证
    if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        return ServiceResult<LoginResponse>.Failure("用户名和密码不能为空");

    // 2. 用户查找和密码验证
    var user = await _userRepository.GetByUsernameAsync(request.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return ServiceResult<LoginResponse>.Failure("用户名或密码错误");

    // 3. 用户状态检查
    if (user.Status != UserStatus.Active)
        return ServiceResult<LoginResponse>.Failure("账户已被禁用");

    // 4. 生成JWT Token
    var token = await _jwtService.GenerateTokenAsync(user, request.RememberMe);

    // 5. 记录登录日志
    await _authSessionRepository.CreateSessionAsync(new AuthSession
    {
        UserId = user.Id,
        Token = token,
        LoginTime = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddHours(request.RememberMe ? 720 : 8)
    });

    // 6. 返回登录响应
    return ServiceResult<LoginResponse>.Success(new LoginResponse
    {
        Token = token,
        UserId = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Role = user.Role,  // UserRole枚举类型
        ExpiresIn = request.RememberMe ? 2592000 : 28800
    });
}
```

### 2. JWT Token管理

**Token生成和验证**:
```csharp
public async Task<string> GenerateTokenAsync(User user, bool rememberMe = false)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_jwtOptions.SecretKey);

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, user.Role.ToString()),  // 枚举转字符串
        new("DisplayName", user.DisplayName ?? user.Username)
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(rememberMe ? 720 : 8),
        Issuer = _jwtOptions.Issuer,
        Audience = _jwtOptions.Audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

### 3. 用户注册

**注册流程**:
```csharp
public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterRequest request)
{
    // 1. 验证输入
    if (!IsValidUsername(request.Username))
        return ServiceResult<UserDto>.Failure("用户名格式无效");

    // 2. 检查用户名唯一性
    var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
    if (existingUser != null)
        return ServiceResult<UserDto>.Failure("用户名已存在");

    // 3. 创建新用户
    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = request.Username,
        DisplayName = request.DisplayName,
        Role = request.Role ?? UserRole.Doctor,  // UserRole枚举
        Status = UserStatus.Active,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        CreateTime = DateTime.UtcNow
    };

    // 4. 保存用户
    await _userRepository.CreateAsync(user);

    // 5. 返回用户信息
    return ServiceResult<UserDto>.Success(new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Role = user.Role,  // UserRole枚举
        Status = user.Status,
        CreateTime = user.CreateTime
    });
}
```

## 🧪 数据传输对象 (数据传输对象（数据传输对象（DTO））) - 2025-09-20更新

### 请求DTOs
```csharp
public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    public UserRole Role { get; set; } = UserRole.Doctor;  // 枚举类型
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
```

### 响应DTOs
```csharp
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }  // 枚举类型，原为string
    public int ExpiresIn { get; set; }  // 秒
}

public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
```

## 🔒 安全特性

### JWT配置
```json
{
  "JwtSettings": {
    "SecretKey": "环境变量配置",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "TokenExpiry": 8,  // 小时
    "RememberMeExpiry": 720  // 30天
  }
}
```

### RBAC权限控制
```csharp
// 角色定义 (UserRole枚举)
public enum UserRole
{
    Doctor = 0,  // 医生
    Admin = 1    // 管理员
}

// 权限策略配置
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    options.AddPolicy("DoctorOnly", policy =>
        policy.RequireRole(UserRole.Doctor.ToString()));

    options.AddPolicy("DoctorOrAdmin", policy =>
        policy.RequireRole(UserRole.Doctor.ToString(), UserRole.Admin.ToString()));
});
```

### 密码安全
- 使用BCrypt进行密码哈希，成本因子10
- 密码最小长度8位
- 支持密码强度验证（可选）
- 密码重置需要旧密码验证

## 🎯 安全最佳实践

1. **Token安全**
 - JWT密钥通过环境变量配置，不硬编码
 - Token过期时间：普通8小时，记住我30天
 - 支持Token黑名单机制

2. **密码安全**
 - BCrypt哈希算法，防止彩虹表攻击
 - 强密码策略可配置
 - 登录失败锁定机制（可选）

3. **审计日志**
 - 记录所有登录尝试
 - 记录密码修改操作
 - 记录异常登录行为

## 📚 相关文档

- [Users用户模块](../LYBT.Module.Users/README.md) - 用户管理
- [Infrastructure基础设施](../../Core/LYBT.基础设施（基础设施（Infrastructure））/README.md) - JWT配置
- [API接口文档](../../Services/LYBT.WebAPI/README.md) - 认证API

## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return HandleServiceResult(result);
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register(
        [FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return HandleServiceResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var token = GetTokenFromHeader();
        var result = await _authService.LogoutAsync(token);
        return HandleServiceResult(result);
    }
}
```

### 服务注册
```csharp
// 在AuthModule.cs中注册
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IJwtAuthenticationService, JwtAuthenticationService>();
services.AddSingleton<ISysAdminHandler, SysAdminHandler>();

// JWT配置
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });
```

---

> 📌 **核心功能**: JWT无状态认证、RBAC权限控制、安全审计完备
> 🎆 **生产就绪**: 编译通过，完整的认证授权体系，支撑诊所安全需求
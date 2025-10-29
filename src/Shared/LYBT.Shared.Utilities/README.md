# LYBT.Shared.Utilities

> **共享工具类库** - .NET 8通用工具与扩展方法
> 配置管理 | 安全工具 | 扩展方法 | 中间件配置
> **模块状态**: ✅ **生产就绪** | 🎆 **工具集完善** | **零编译错误** | **2025-09-20更新**

## 📦 项目定位

- **层级**: Shared层（跨端共享）
- **类型**: 通用工具库（Utilities Library）
- **职责**: 提供配置管理、安全工具、扩展方法、中间件配置等核心功能。为整个系统（Server/Desktop/Mobile）提供统一的工具支持，简化开发流程，提高代码复用性。本项目遵循"工具类分层"原则，按功能分类组织（Configuration/Extensions/Helpers/Security），确保易于查找和维护。

## 🎯 项目概述

LYBT.Shared.Utilities 是系统的共享工具类库，提供配置管理、安全工具、扩展方法等核心功能。为整个系统提供统一的工具支持，简化开发流程，提高代码复用性。

**技术栈**: .NET 8 + Microsoft.AspNetCore.Identity + System.Text.Json
**架构模式**: 工具类分层 + 扩展方法模式 + 配置集中化
**核心功能**: 密码安全、JWT配置、缓存扩展、中间件配置

## 📂 代码结构

```
LYBT.Shared.Utilities/
├── Configuration/                    # 配置管理（2个类）
│   ├── ConfigurationHelper.cs       # 配置读取帮助类（3个方法：连接字符串/配置节/必需值）
│   └── EnvironmentHelper.cs         # 环境变量管理（3个方法：开发/生产/当前环境）
├── Extensions/                       # 扩展方法（5个类）
│   ├── Application/                  # 应用程序扩展（2个类）
│   │   ├── ApplicationInitializationExtensions.cs  # 应用初始化扩展（应用启动配置）
│   │   └── MiddlewareConfigurationExtensions.cs    # 中间件配置扩展（异常处理/CORS）
│   └── ServiceCollection/            # 服务注册扩展（3个类）
│       ├── AuthenticationExtensions.cs  # JWT认证扩展（AddJwtAuthentication）
│       ├── AuthorizationExtensions.cs   # RBAC授权扩展（AddRoleBasedAuthorization）
│       └── CacheExtensions.cs           # 缓存服务扩展（AddMemoryCache/AddDistributedCache）
├── Helpers/                          # 帮助类（1个类）
│   └── PasswordHelper.cs            # 密码安全工具（4个方法：哈希/验证/强度检查/生成安全密码）
└── Security/                         # 安全工具（2个类）
    ├── ClaimsHelper.cs              # Claims处理工具（4个方法：用户ID/用户名/角色/创建Claims）
    └── RoleHelper.cs                # 角色管理工具（4个方法：IsAdmin/IsDoctor/HasAccess/GetDisplayName）
```

**说明**:
- **6个目录，11个文件**: 按功能分类组织，易于查找
- **Configuration**: 配置管理基础工具（2个类）
- **Extensions**: 扩展方法（5个类：2个Application + 3个ServiceCollection）
- **Helpers**: 通用帮助类（1个类：PasswordHelper）
- **Security**: 安全相关工具（2个类：Claims + Role）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型（UserDto、ApiResponse等）
2. **无其他项目依赖** - 保持工具库独立性

### 被依赖项目
1. **LYBT.WebAPI** - Web服务层使用JWT认证、授权、异常处理
2. **LYBT.Server模块** - Server端业务模块使用安全工具和扩展方法
3. **LYBT.Infrastructure** - 基础设施层使用配置管理和缓存扩展
4. **LYBT.Desktop.Infrastructure** - Desktop端基础设施使用配置和安全工具
5. **测试项目**:
   - LYBT.Shared.Utilities.Tests（单元测试）

### NuGet包
- **Microsoft.Extensions.Configuration** (8.0.x) - 配置管理基础
- **Microsoft.Extensions.Configuration.Binder** (8.0.x) - 配置绑定
- **Microsoft.AspNetCore.Identity** (8.0.x) - 密码哈希（PasswordHasher）
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.x) - JWT认证
- **Microsoft.Extensions.Caching.Memory** (8.0.x) - 内存缓存
- **Microsoft.Extensions.Caching.StackExchangeRedis** (8.0.x) - Redis缓存
- **System.Text.Json** (8.0.x) - JSON序列化

## 🛠 技术栈

- **.NET 8**: 目标框架
- **C# 12**: 编程语言
- **ASP.NET Core Identity**: 密码哈希（PasswordHasher<T>）
- **JWT Bearer Authentication**: JWT令牌认证
- **Memory Cache**: 内存缓存服务
- **Redis Cache**: 分布式缓存（可选）
- **System.Text.Json**: JSON序列化
- **扩展方法模式**: IServiceCollection扩展 + IApplicationBuilder扩展

## 🏗️ 核心工具类总览

### 快速索引（11个工具类）

| 分类 | 工具类 | 核心方法数 | 用途 |
|------|--------|----------|------|
| **配置管理** | ConfigurationHelper | 3 | 连接字符串/配置节/必需值 |
| **配置管理** | EnvironmentHelper | 3 | 环境检测（开发/生产） |
| **JWT认证** | AuthenticationExtensions | 1 | AddJwtAuthentication |
| **RBAC授权** | AuthorizationExtensions | 1 | AddRoleBasedAuthorization |
| **缓存服务** | CacheExtensions | 2 | AddMemoryCache/AddDistributedCache |
| **应用初始化** | ApplicationInitializationExtensions | N | 应用启动配置 |
| **中间件配置** | MiddlewareConfigurationExtensions | 2 | 异常处理/CORS |
| **密码安全** | PasswordHelper | 4 | Hash/Verify/CheckStrength/Generate |
| **Claims处理** | ClaimsHelper | 4 | GetUserId/GetUserName/GetRoles/CreateJwtClaims |
| **角色管理** | RoleHelper | 4 | IsAdmin/IsDoctor/HasAccess/GetDisplayName |

**总计**: 6个目录，11个工具类，24+个核心方法

## 🔧 核心功能模块

### 1. 配置管理（Configuration）

#### ConfigurationHelper - 配置读取

```csharp
/// <summary>
/// 配置读取帮助类
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// 获取连接字符串
    /// </summary>
    public static string GetConnectionString(IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' not found");
        }
        return connectionString;
    }

    /// <summary>
    /// 获取配置节（强类型绑定）
    /// </summary>
    public static T GetSection<T>(IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }

    /// <summary>
    /// 获取必需的配置值
    /// </summary>
    public static string GetRequiredValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Configuration key '{key}' is required but not found");
        }
        return value;
    }
}
```

**使用场景**:
- 读取数据库连接字符串（`GetConnectionString("DefaultConnection")`）
- 强类型配置绑定（`GetSection<JwtSettings>("JwtSettings")`）
- 必需配置验证（`GetRequiredValue("ApiKey")`）

#### EnvironmentHelper - 环境管理

```csharp
/// <summary>
/// 环境变量管理
/// </summary>
public static class EnvironmentHelper
{
    /// <summary>
    /// 是否为开发环境
    /// </summary>
    public static bool IsDevelopment()
        => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

    /// <summary>
    /// 是否为生产环境
    /// </summary>
    public static bool IsProduction()
        => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

    /// <summary>
    /// 获取当前环境名称
    /// </summary>
    public static string GetCurrentEnvironment()
        => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
}
```

**使用场景**:
- 环境特定配置（`if (EnvironmentHelper.IsDevelopment()) { ... }`）
- 日志级别控制（开发环境详细日志，生产环境警告级别）
- 调试功能开关（仅开发环境启用）

### 2. 扩展方法（Extensions）

#### AuthenticationExtensions - JWT认证配置

```csharp
/// <summary>
/// JWT认证扩展方法
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// 添加JWT认证
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            // 事件处理：令牌过期
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
```

**使用场景**:
- WebAPI项目JWT认证配置（`services.AddJwtAuthentication(Configuration)`）
- 令牌验证参数配置（Issuer/Audience/SecretKey）
- 令牌过期事件处理（添加Token-Expired响应头）

#### AuthorizationExtensions - RBAC授权配置

```csharp
/// <summary>
/// RBAC授权扩展方法
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// 添加基于角色的授权策略
    /// </summary>
    public static IServiceCollection AddRoleBasedAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // 管理员策略
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            // 医生策略
            options.AddPolicy("DoctorOnly", policy =>
                policy.RequireRole("Doctor"));

            // 医生或管理员
            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireRole("Doctor", "Admin"));

            // 认证用户
            options.AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
```

**使用场景**:
- 控制器授权（`[Authorize(Policy = "AdminOnly")]`）
- 方法级别授权（`[Authorize(Policy = "DoctorOrAdmin")]`）
- 最小权限原则（每个API明确所需角色）

#### CacheExtensions - 缓存服务配置

```csharp
/// <summary>
/// 缓存服务扩展方法
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 添加内存缓存服务
    /// </summary>
    public static IServiceCollection AddMemoryCacheService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 添加内存缓存
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = configuration.GetValue<long>("Cache:SizeLimit", 1024);
            options.CompactionPercentage = configuration.GetValue<double>("Cache:CompactionPercentage", 0.05);
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(
                configuration.GetValue<int>("Cache:ExpirationScanFrequencyMinutes", 5));
        });

        // 注册缓存服务
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// 添加分布式缓存
    /// </summary>
    public static IServiceCollection AddDistributedCacheService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "LYBT";
            });
        }
        else
        {
            // 降级为内存缓存
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
```

**使用场景**:
- 单机部署（内存缓存）：`services.AddMemoryCacheService(Configuration)`
- 分布式部署（Redis缓存）：`services.AddDistributedCacheService(Configuration)`
- 缓存配置（大小限制/压缩百分比/过期扫描频率）

### 3. 安全工具（Security）

#### PasswordHelper - 密码安全

```csharp
/// <summary>
/// 密码安全工具类
/// </summary>
public static class PasswordHelper
{
    private static readonly PasswordHasher<object> _hasher = new();

    /// <summary>
    /// 生成密码哈希（使用BCrypt算法）
    /// </summary>
    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        return _hasher.HashPassword(null, password);
    }

    /// <summary>
    /// 验证密码（防时间攻击）
    /// </summary>
    public static bool Verify(string hashedPassword, string password)
    {
        var result = _hasher.VerifyHashedPassword(null, hashedPassword, password);
        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>
    /// 检查密码强度（7个评分标准）
    /// </summary>
    public static PasswordStrength CheckPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.Weak;

        int score = 0;

        // 长度检查（3分）
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // 字符类型检查（4分）
        if (Regex.IsMatch(password, @"[a-z]")) score++;
        if (Regex.IsMatch(password, @"[A-Z]")) score++;
        if (Regex.IsMatch(password, @"[0-9]")) score++;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+=\-{}\[\]:;""'<>,.?/|\\]")) score++;

        // 返回强度级别
        return score switch
        {
            >= 7 => PasswordStrength.VeryStrong,
            >= 5 => PasswordStrength.Strong,
            >= 3 => PasswordStrength.Good,
            >= 2 => PasswordStrength.Fair,
            _ => PasswordStrength.Weak
        };
    }

    /// <summary>
    /// 生成安全密码（确保包含各类字符）
    /// </summary>
    public static string GenerateSecurePassword(int length = 12)
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_-+={}[]|:;<>,.?";

        var chars = uppercase + lowercase + digits + special;
        var random = new Random();
        var password = new StringBuilder();

        // 确保至少包含各类字符
        password.Append(uppercase[random.Next(uppercase.Length)]);
        password.Append(lowercase[random.Next(lowercase.Length)]);
        password.Append(digits[random.Next(digits.Length)]);
        password.Append(special[random.Next(special.Length)]);

        // 填充剩余字符
        for (int i = 4; i < length; i++)
        {
            password.Append(chars[random.Next(chars.Length)]);
        }

        // 打乱顺序
        return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
    }
}
```

**使用场景**:
- 用户注册（`PasswordHelper.Hash(password)`）
- 用户登录（`PasswordHelper.Verify(hashedPassword, password)`）
- 密码强度检查（`PasswordHelper.CheckPasswordStrength(password)`）
- 临时密码生成（`PasswordHelper.GenerateSecurePassword(12)`）

#### ClaimsHelper - Claims处理

```csharp
/// <summary>
/// Claims处理工具
/// </summary>
public static class ClaimsHelper
{
    /// <summary>
    /// 获取用户ID（多Claims兼容）
    /// </summary>
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                       ?? principal.FindFirst("sub")
                       ?? principal.FindFirst("UserId");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        return userId;
    }

    /// <summary>
    /// 获取用户名（多Claims兼容）
    /// </summary>
    public static string GetUserName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("username")?.Value
            ?? throw new UnauthorizedAccessException("Username not found in claims");
    }

    /// <summary>
    /// 获取用户角色（支持多角色）
    /// </summary>
    public static IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// 创建JWT Claims（标准Claims）
    /// </summary>
    public static List<Claim> CreateJwtClaims(UserDto user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("RealName", user.RealName ?? ""),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("jti", Guid.NewGuid().ToString()), // JWT ID
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // Issued At
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        return claims;
    }
}
```

**使用场景**:
- 控制器获取当前用户（`ClaimsHelper.GetUserId(User)`）
- 审计日志（`ClaimsHelper.GetUserName(User)`）
- 权限检查（`ClaimsHelper.GetRoles(User)`）
- 登录成功生成JWT（`ClaimsHelper.CreateJwtClaims(userDto)`）

#### RoleHelper - 角色管理

```csharp
/// <summary>
/// 角色管理工具
/// </summary>
public static class RoleHelper
{
    /// <summary>
    /// 检查是否为管理员
    /// </summary>
    public static bool IsAdmin(ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    /// <summary>
    /// 检查是否为医生
    /// </summary>
    public static bool IsDoctor(ClaimsPrincipal principal)
    {
        return principal.IsInRole("Doctor");
    }

    /// <summary>
    /// 检查是否有权限访问（多角色OR）
    /// </summary>
    public static bool HasAccess(ClaimsPrincipal principal, params string[] allowedRoles)
    {
        return allowedRoles.Any(role => principal.IsInRole(role));
    }

    /// <summary>
    /// 获取角色显示名称（中文）
    /// </summary>
    public static string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "管理员",
            UserRole.Doctor => "医生",
            _ => "未知角色"
        };
    }
}
```

**使用场景**:
- 控制器权限检查（`if (!RoleHelper.IsAdmin(User)) return Forbid();`）
- 业务逻辑权限判断（`if (RoleHelper.HasAccess(User, "Admin", "Doctor"))`）
- UI角色显示（`RoleHelper.GetRoleDisplayName(user.Role)`）

### 4. 中间件配置（Middleware）

#### MiddlewareConfigurationExtensions

```csharp
/// <summary>
/// 中间件配置扩展
/// </summary>
public static class MiddlewareConfigurationExtensions
{
    /// <summary>
    /// 配置全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app)
    {
        app.UseExceptionHandler(appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature != null)
                {
                    // 记录异常日志
                    var logger = context.RequestServices.GetService<ILogger<Program>>();
                    logger?.LogError(exceptionFeature.Error, "Global exception occurred");

                    // 返回统一错误响应
                    var response = new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Internal server error occurred",
                        Data = null
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            });
        });

        return app;
    }

    /// <summary>
    /// 配置CORS（跨域资源共享）
    /// </summary>
    public static IApplicationBuilder UseConfiguredCors(
        this IApplicationBuilder app)
    {
        app.UseCors(builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });

        return app;
    }
}
```

**使用场景**:
- WebAPI全局异常处理（`app.UseGlobalExceptionHandler()`）
- 开发环境CORS配置（`app.UseConfiguredCors()`）
- 统一错误响应格式（ApiResponse<T>）

## 🚀 使用示例

### 场景1: Startup配置（WebAPI项目）

```csharp
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // 1. 添加JWT认证
        services.AddJwtAuthentication(_configuration);

        // 2. 添加RBAC授权（4个策略）
        services.AddRoleBasedAuthorization();

        // 3. 添加内存缓存服务
        services.AddMemoryCacheService(_configuration);

        // 4. 添加分布式缓存（Redis）
        services.AddDistributedCacheService(_configuration);

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 1. 配置全局异常处理
        app.UseGlobalExceptionHandler();

        // 2. 配置CORS（开发环境）
        if (EnvironmentHelper.IsDevelopment())
        {
            app.UseConfiguredCors();
        }

        // 3. 认证授权
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

### 场景2: 控制器使用（用户管理）

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 需要认证
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // 1. 获取当前用户信息（从Claims）
        var userId = ClaimsHelper.GetUserId(User);
        var userName = ClaimsHelper.GetUserName(User);
        var roles = ClaimsHelper.GetRoles(User);

        // 2. 检查权限（管理员或医生）
        if (!RoleHelper.HasAccess(User, "Admin", "Doctor"))
        {
            _logger.LogWarning($"User {userId} attempted to access profile without permission");
            return Forbid();
        }

        // 3. 查询用户信息
        var userDto = await _userService.GetByIdAsync(userId);

        return Ok(new ApiResponse<object>.CreateSuccess(new
        {
            userId,
            userName,
            roles,
            userDto
        }));
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = ClaimsHelper.GetUserId(User);
        var user = await _userService.GetByIdAsync(userId);

        // 1. 验证旧密码
        if (!PasswordHelper.Verify(user.PasswordHash, dto.OldPassword))
        {
            return BadRequest(ApiResponse<object>.CreateFailure("旧密码不正确"));
        }

        // 2. 检查新密码强度
        var strength = PasswordHelper.CheckPasswordStrength(dto.NewPassword);
        if (strength < PasswordStrength.Good)
        {
            return BadRequest(ApiResponse<object>.CreateFailure("密码强度不足，需至少包含大小写字母、数字和特殊字符"));
        }

        // 3. 生成新密码哈希
        user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);
        await _userService.UpdateAsync(user);

        _logger.LogInformation($"User {userId} changed password successfully");
        return Ok(ApiResponse<object>.CreateSuccess(null, "密码修改成功"));
    }

    /// <summary>
    /// 创建用户（仅管理员）
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateUser(UserCreateDto dto)
    {
        // 1. 生成安全密码（如果未提供）
        if (string.IsNullOrEmpty(dto.Password))
        {
            dto.Password = PasswordHelper.GenerateSecurePassword(12);
            _logger.LogInformation($"Generated secure password for new user: {dto.Username}");
        }

        // 2. 哈希密码
        dto.PasswordHash = PasswordHelper.Hash(dto.Password);

        // 3. 创建用户
        var result = await _userService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetProfile), new { id = result.Data.Id }, result);
    }
}
```

### 场景3: 配置管理（appsettings.json）

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LYBT;Trusted_Connection=True;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Issuer": "LYBT",
    "Audience": "LYBT-Client",
    "SecretKey": "your-secret-key-at-least-32-characters",
    "ExpiresInMinutes": 60,
    "RefreshExpiresInDays": 7
  },
  "Cache": {
    "SizeLimit": 1024,
    "CompactionPercentage": 0.05,
    "ExpirationScanFrequencyMinutes": 5
  }
}
```

**使用示例**:
```csharp
// 读取连接字符串
var connString = ConfigurationHelper.GetConnectionString(Configuration, "DefaultConnection");

// 读取JWT配置（强类型）
var jwtSettings = ConfigurationHelper.GetSection<JwtSettings>(Configuration, "JwtSettings");

// 读取必需的API密钥
var apiKey = ConfigurationHelper.GetRequiredValue(Configuration, "ApiKey");
```

### 场景4: 登录逻辑（JWT生成）

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // 1. 查询用户
        var user = await _userService.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.CreateFailure("用户名或密码错误"));
        }

        // 2. 验证密码
        if (!PasswordHelper.Verify(user.PasswordHash, request.Password))
        {
            return Unauthorized(ApiResponse<object>.CreateFailure("用户名或密码错误"));
        }

        // 3. 创建JWT Claims
        var claims = ClaimsHelper.CreateJwtClaims(user);

        // 4. 生成JWT令牌
        var jwtSettings = ConfigurationHelper.GetSection<JwtSettings>(_configuration, "JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpiresInMinutes),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // 5. 返回令牌
        return Ok(ApiResponse<object>.CreateSuccess(new
        {
            AccessToken = accessToken,
            ExpiresIn = jwtSettings.ExpiresInMinutes * 60,
            TokenType = "Bearer",
            User = user
        }));
    }
}
```

### 场景5: 环境特定配置

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 开发环境：详细日志 + Swagger
        if (EnvironmentHelper.IsDevelopment())
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSwaggerGen();
        }

        // 生产环境：警告日志 + 分布式缓存
        if (EnvironmentHelper.IsProduction())
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            services.AddDistributedCacheService(Configuration);
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        // 开发环境：CORS + Swagger UI
        if (EnvironmentHelper.IsDevelopment())
        {
            app.UseConfiguredCors();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
```

## 🎯 最佳实践

### 1. 配置管理
- ✅ **使用强类型配置类**: `ConfigurationHelper.GetSection<JwtSettings>("JwtSettings")`
- ✅ **验证必需配置项**: `GetRequiredValue()` 启动时检查配置完整性
- ✅ **环境特定配置分离**: `appsettings.Development.json` vs. `appsettings.Production.json`
- ✅ **敏感信息使用Secret Manager**: 开发环境 `dotnet user-secrets set "ApiKey" "value"`

### 2. 安全原则
- ✅ **密码必须哈希存储**: 使用 `PasswordHelper.Hash()`，禁止明文存储
- ✅ **强制密码复杂度要求**: `CheckPasswordStrength()` ≥ Good
- ✅ **JWT令牌定期刷新**: AccessToken 60分钟 + RefreshToken 7天
- ✅ **基于角色的访问控制**: 使用 `[Authorize(Policy = "AdminOnly")]`

### 3. 扩展方法
- ✅ **保持方法简洁单一**: 每个扩展方法只做一件事
- ✅ **提供默认参数值**: `AddMemoryCacheService()` 提供默认缓存配置
- ✅ **异常处理完善**: `GetConnectionString()` 配置缺失时抛出清晰异常
- ✅ **返回链式调用支持**: `services.AddJwtAuthentication().AddRoleBasedAuthorization()`

### 4. 工具类设计
- ✅ **静态类 + 静态方法**: 无状态工具类使用 `static class`
- ✅ **线程安全**: `PasswordHasher<object>` 使用线程安全的静态实例
- ✅ **参数验证**: 所有公共方法验证参数（null/empty检查）
- ✅ **文档注释**: 所有公共方法提供XML文档注释

### 5. 依赖管理
- ✅ **最小依赖**: 仅依赖必需的NuGet包
- ✅ **可选依赖**: Redis缓存可选，降级为内存缓存
- ✅ **版本锁定**: 使用明确的NuGet包版本（8.0.x）
- ✅ **定期更新**: 跟随.NET版本更新依赖包

## 📈 性能优化

### 缓存策略
- **内存缓存**: 热点数据缓存（用户信息、角色权限），过期时间15-60分钟
- **分布式缓存**: 会话数据缓存（JWT刷新令牌黑名单），过期时间7天
- **缓存预热**: 应用启动时预加载常用配置和静态数据

### 密码哈希性能
- **BCrypt算法**: ASP.NET Core Identity内置PasswordHasher，平衡安全与性能
- **哈希强度**: 默认工作因子（不建议调整，已优化）
- **异步验证**: 密码验证使用同步方法（哈希验证本身是CPU密集操作）

### 配置缓存
- **配置值缓存**: `IOptions<T>` 模式，配置值自动缓存
- **避免重复读取**: 使用 `GetSection<T>()` 强类型绑定，避免每次读取JSON
- **环境变量缓存**: `EnvironmentHelper` 方法调用系统API，建议缓存结果

### 异步操作
- **I/O密集操作**: Claims处理、JWT生成等使用同步方法（CPU密集）
- **缓存服务**: ICacheService 提供异步方法（`GetAsync`/`SetAsync`）
- **配置读取**: IConfiguration 使用同步方法（已优化）

## 🔒 安全考虑

### 密码安全
- **BCrypt哈希 + 盐值**: ASP.NET Core Identity内置，防彩虹表攻击
- **密码强度检查**: 7个评分标准（长度3分 + 字符类型4分）
- **防暴力破解**: 登录失败计数（建议在Service层实现）
- **密码历史**: 防止重复使用旧密码（建议在Service层实现）

### 时间攻击防护
- **安全字符串比较**: `PasswordHasher.VerifyHashedPassword()` 使用恒定时间比较
- **JWT验证**: `JwtBearerAuthentication` 内置时间攻击防护
- **避免提前返回**: 密码验证失败时不暴露用户名是否存在

### JWT安全
- **短期令牌**: AccessToken 60分钟，降低令牌泄露风险
- **刷新令牌机制**: RefreshToken 7天，支持长期登录
- **令牌黑名单**: 使用分布式缓存实现（退出登录时加入黑名单）
- **ClockSkew设置**: `TimeSpan.Zero`，禁用时钟偏移（严格验证过期时间）

### 配置安全
- **敏感信息不入代码库**: JWT SecretKey、数据库密码使用Secret Manager
- **生产环境配置**: `appsettings.Production.json` 不提交到Git
- **环境变量**: 生产环境使用环境变量或Azure Key Vault
- **最小权限原则**: 数据库连接字符串使用专用账号，授予最小权限

## 🚀 快速开始

此项目是一个类库，作为工具库被其他项目引用。无法独立运行。

```bash
# 构建此项目
dotnet build src/Shared/LYBT.Shared.Utilities/LYBT.Shared.Utilities.csproj
```

**集成说明**:

### 1. 添加项目引用
```xml
<!-- 在需要使用工具库的项目中 -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
</ItemGroup>
```

### 2. 在Startup中配置
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 使用扩展方法配置服务
        services.AddJwtAuthentication(Configuration);
        services.AddRoleBasedAuthorization();
        services.AddMemoryCacheService(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        // 使用扩展方法配置中间件
        app.UseGlobalExceptionHandler();
        app.UseConfiguredCors();
    }
}
```

### 3. 在Controller中使用
```csharp
public class MyController : ControllerBase
{
    [HttpPost("action")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> MyAction()
    {
        var userId = ClaimsHelper.GetUserId(User);
        var isAdmin = RoleHelper.IsAdmin(User);
        // ...
    }
}
```

## 🧪 测试指南

### 单元测试覆盖

**PasswordHelper测试**:
```csharp
[Test]
public void Hash_ShouldGenerateDifferentHashesForSamePassword()
{
    var password = "TestPassword123!";
    var hash1 = PasswordHelper.Hash(password);
    var hash2 = PasswordHelper.Hash(password);

    Assert.AreNotEqual(hash1, hash2); // 每次哈希结果不同（盐值随机）
    Assert.IsTrue(PasswordHelper.Verify(hash1, password));
    Assert.IsTrue(PasswordHelper.Verify(hash2, password));
}

[Test]
public void CheckPasswordStrength_ShouldReturnCorrectStrength()
{
    Assert.AreEqual(PasswordStrength.Weak, PasswordHelper.CheckPasswordStrength("123"));
    Assert.AreEqual(PasswordStrength.Fair, PasswordHelper.CheckPasswordStrength("password"));
    Assert.AreEqual(PasswordStrength.Good, PasswordHelper.CheckPasswordStrength("Password1"));
    Assert.AreEqual(PasswordStrength.Strong, PasswordHelper.CheckPasswordStrength("Password1!"));
    Assert.AreEqual(PasswordStrength.VeryStrong, PasswordHelper.CheckPasswordStrength("MySecurePassword123!@#"));
}
```

**ClaimsHelper测试**:
```csharp
[Test]
public void GetUserId_ShouldReturnValidGuid()
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

    var userId = ClaimsHelper.GetUserId(principal);

    Assert.IsNotNull(userId);
    Assert.AreNotEqual(Guid.Empty, userId);
}
```

**ConfigurationHelper测试**:
```csharp
[Test]
public void GetRequiredValue_ShouldThrowIfKeyNotFound()
{
    var configuration = new ConfigurationBuilder().Build();

    Assert.Throws<InvalidOperationException>(() =>
        ConfigurationHelper.GetRequiredValue(configuration, "NonExistentKey"));
}
```

## 📚 相关文档

**架构设计**:
- [Server端架构指南](../../docs/explanation/architecture/server/README.md)
- [Desktop端架构指南](../../docs/explanation/architecture/client/README.md)
- [Shared层架构指南](../../docs/explanation/architecture/shared/README.md)

**开发指南**:
- [服务器端开发指南](../../docs/development/server/README.md)
- [桌面端开发指南](../../docs/development/client/README.md)
- [共享组件开发指南](../../docs/development/shared/README.md)

**API文档**:

**安全指南**:
- [密码安全最佳实践](../../docs/security/password-security.md)
- [JWT认证实施指南](../../docs/security/jwt-authentication.md)

---

> 📌 **最新成果**: 工具集完善，支持JWT、缓存、密码安全等核心功能
> 🎆 **生产就绪**: 经过充分测试的工具库，稳定可靠
> ⚡ **性能优化**: BCrypt哈希、配置缓存、异步操作
> 🔒 **安全保障**: BCrypt + 盐值、JWT黑名单、恒定时间比较

**最后更新**: 2025-10-29
**维护负责**: 架构组

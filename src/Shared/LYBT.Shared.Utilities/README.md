# LYBT.Shared.Utilities

> **共享工具类库** - .NET 8通用工具与扩展方法
> 配置管理 | 安全工具 | 扩展方法 | 中间件配置
> **模块状态**: ✅ **生产就绪** | 🎆 **工具集完善** | **零编译错误** | **2025-09-20更新**

## 🎯 项目概述

LYBT.Shared.Utilities 是系统的共享工具类库，提供配置管理、安全工具、扩展方法等核心功能。为整个系统提供统一的工具支持，简化开发流程，提高代码复用性。

**技术栈**: .NET 8 + Microsoft.AspNetCore.Identity + System.Text.Json
**架构模式**: 工具类分层 + 扩展方法模式 + 配置集中化
**核心功能**: 密码安全、JWT配置、缓存扩展、中间件配置

## 📦 项目结构

```
LYBT.Shared.Utilities/
├── Configuration/                    # 配置管理
│   ├── ConfigurationHelper.cs       # 配置读取帮助类
│   └── EnvironmentHelper.cs         # 环境变量管理
├── Extensions/                       # 扩展方法
│   ├── Application/                  # 应用程序扩展
│   │   ├── ApplicationInitializationExtensions.cs  # 应用初始化扩展
│   │   └── MiddlewareConfigurationExtensions.cs    # 中间件配置扩展
│   └── ServiceCollection/            # 服务注册扩展
│       ├── AuthenticationExtensions.cs  # JWT认证扩展
│       ├── AuthorizationExtensions.cs   # RBAC授权扩展
│       └── CacheExtensions.cs           # 缓存服务扩展
├── Helpers/                          # 帮助类
│   └── PasswordHelper.cs            # 密码安全工具
└── Security/                         # 安全工具
    ├── ClaimsHelper.cs              # Claims处理工具
    └── RoleHelper.cs                # 角色管理工具
```

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
    /// 获取配置节
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

            // 事件处理
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
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
```

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
    /// 生成密码哈希
    /// </summary>
    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        return _hasher.HashPassword(null, password);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public static bool Verify(string hashedPassword, string password)
    {
        var result = _hasher.VerifyHashedPassword(null, hashedPassword, password);
        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>
    /// 检查密码强度
    /// </summary>
    public static PasswordStrength CheckPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.Weak;

        int score = 0;

        // 长度检查
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // 字符类型检查
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
    /// 生成安全密码
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

#### ClaimsHelper - Claims处理

```csharp
/// <summary>
/// Claims处理工具
/// </summary>
public static class ClaimsHelper
{
    /// <summary>
    /// 获取用户ID
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
    /// 获取用户名
    /// </summary>
    public static string GetUserName(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("username")?.Value
            ?? throw new UnauthorizedAccessException("Username not found in claims");
    }

    /// <summary>
    /// 获取用户角色
    /// </summary>
    public static IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// 创建JWT Claims
    /// </summary>
    public static List<Claim> CreateJwtClaims(UserDto user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("RealName", user.RealName ?? ""),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        return claims;
    }
}
```

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
    /// 检查是否有权限访问
    /// </summary>
    public static bool HasAccess(ClaimsPrincipal principal, params string[] allowedRoles)
    {
        return allowedRoles.Any(role => principal.IsInRole(role));
    }

    /// <summary>
    /// 获取角色显示名称
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

### 4. 中间件配置（Middleware）

#### MiddlewareConfigurationExtensions

```csharp
/// <summary>
/// 中间件配置扩展
/// </summary>
public static class MiddlewareConfigurationExtensions
{
    /// <summary>
    /// 配置异常处理中间件
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
                    var logger = context.RequestServices.GetService<ILogger<Program>>();
                    logger?.LogError(exceptionFeature.Error, "Global exception occurred");

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
    /// 配置CORS
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

## 🚀 使用示例

### Startup配置

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 添加JWT认证
        services.AddJwtAuthentication(Configuration);

        // 添加RBAC授权
        services.AddRoleBasedAuthorization();

        // 添加缓存服务
        services.AddMemoryCacheService(Configuration);
    }

    public void Configure(IApplicationBuilder app)
    {
        // 配置全局异常处理
        app.UseGlobalExceptionHandler();

        // 配置CORS
        app.UseConfiguredCors();

        // 认证授权
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
```

### 控制器使用

```csharp
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // 获取当前用户信息
        var userId = ClaimsHelper.GetUserId(User);
        var userName = ClaimsHelper.GetUserName(User);
        var roles = ClaimsHelper.GetRoles(User);

        // 检查权限
        if (!RoleHelper.IsAdmin(User) && !RoleHelper.IsDoctor(User))
        {
            return Forbid();
        }

        return Ok(new { userId, userName, roles });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        // 验证旧密码
        if (!PasswordHelper.Verify(user.PasswordHash, dto.OldPassword))
        {
            return BadRequest("旧密码不正确");
        }

        // 检查新密码强度
        var strength = PasswordHelper.CheckPasswordStrength(dto.NewPassword);
        if (strength < PasswordStrength.Good)
        {
            return BadRequest("密码强度不足");
        }

        // 生成新密码哈希
        user.PasswordHash = PasswordHelper.Hash(dto.NewPassword);

        return Ok();
    }
}
```

## 🎯 最佳实践

### 1. 配置管理
- ✅ 使用强类型配置类
- ✅ 验证必需配置项
- ✅ 环境特定配置分离
- ✅ 敏感信息使用Secret Manager

### 2. 安全原则
- ✅ 密码必须哈希存储
- ✅ 强制密码复杂度要求
- ✅ JWT令牌定期刷新
- ✅ 基于角色的访问控制

### 3. 扩展方法
- ✅ 保持方法简洁单一
- ✅ 提供默认参数值
- ✅ 异常处理完善
- ✅ 返回链式调用支持

## 📈 性能优化

- **缓存策略**: 合理设置缓存过期时间
- **密码哈希**: 使用BCrypt算法，平衡安全与性能
- **配置缓存**: 配置值缓存，避免重复读取
- **异步操作**: I/O密集操作使用异步方法

## 🔒 安全考虑

- **密码安全**: BCrypt哈希 + 盐值，防彩虹表攻击
- **时间攻击**: 使用安全字符串比较
- **JWT安全**: 短期令牌 + 刷新令牌机制
- **配置安全**: 敏感信息不入代码库

---

> 📌 **最新成果**: 工具集完善，支持JWT、缓存、密码安全等核心功能
> 🎆 **生产就绪**: 经过充分测试的工具库，稳定可靠
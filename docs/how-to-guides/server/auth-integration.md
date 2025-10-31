# Server端认证集成指南

## 📋 文档说明

本指南面向**Server端后端开发者**,详细讲解如何在LYBT诊疗系统中集成JWT认证机制。涵盖从环境配置、JWT服务实现、认证中间件配置、双轨认证机制、到最佳实践的完整开发流程。

**适用场景**:
- ✅ 新增需要认证保护的API端点
- ✅ 实现自定义授权策略（基于角色/Claims）
- ✅ 配置生产环境JWT密钥管理
- ✅ 理解双轨认证机制（普通用户 + 超级管理员）
- ✅ 实现Token刷新和会话管理
- ✅ 集成第三方认证提供商（扩展）

**前置条件**:
- 阅读完成：`docs/architecture/server/README.md` - Server端三层架构
- 理解概念：JWT工作原理、Bearer Token、Claims-Based认证
- 熟悉框架：ASP.NET Core 8、Entity Framework Core 8

---

## 1. 认证架构总览

### 1.1 三层认证架构

```
┌─────────────────────────────────────────────────┐
│           API Controller Layer (入口层)          │
│  AuthController (登录/登出/验证/密码修改)         │
│  → 接收HTTP请求                                 │
│  → 参数验证                                     │
│  → 调用认证服务                                  │
│  → 返回ApiResponse<LoginResponse>                │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│          Service Layer (业务逻辑层)             │
│  AuthService (认证服务)                          │
│  → VerifyCredentialsAsync (验证凭证)             │
│  → LoginAsync (登录流程)                         │
│  → LogoutAsync (登出流程)                        │
│  JwtService (JWT服务)                            │
│  → GenerateToken (生成Token)                     │
│  → ValidateToken (验证Token)                     │
└───────────────┬─────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────┐
│        Repository Layer (数据访问层)             │
│  IUserRepository (用户仓储)                      │
│  → GetByUsernameAsync (按用户名查询)             │
│  AppDbContext (数据库上下文)                     │
│  → Users表 (普通用户)                            │
│  → AdminSecrets表 (超级管理员密码哈希)           │
└─────────────────────────────────────────────────┘
```

### 1.2 JWT认证流程

```
客户端                    Server                  数据库
  │                        │                        │
  ├──POST /auth/login─────▶│                        │
  │  {username, password}  │                        │
  │                        │──查询用户───────────▶│
  │                        │                        │
  │                        │◀──UserModel/AdminSecret│
  │                        │                        │
  │                        │ (BCrypt验证密码)       │
  │                        │ (生成JWT Token)        │
  │                        │                        │
  │◀─200 OK + JWT Token────│                        │
  │                        │                        │
  ├──GET /api/patients────▶│                        │
  │  Authorization: Bearer Token                    │
  │                        │                        │
  │                        │ (验证Token签名)        │
  │                        │ (检查过期时间)         │
  │                        │ (提取Claims)           │
  │                        │                        │
  │◀─200 OK + 患者数据─────│                        │
```

### 1.3 双轨认证机制

LYBT系统采用**双轨认证**,区分普通用户和超级管理员:

| 认证类型 | 用户名存储 | 密码哈希存储 | 认证流程 |
|---------|-----------|-------------|---------|
| **普通用户** | Users表 | Users表.PasswordHash | IUserRepository → BCrypt验证 |
| **超级管理员** | 配置文件 | AdminSecrets表.PasswordHash | 配置文件读取 → AdminSecrets查询 → BCrypt验证 |

**设计原因**:
- ✅ 防止SQL注入后超级管理员账户名泄露
- ✅ 独立的密码存储提高安全性
- ✅ 支持零停机密码轮换

---

## 2. 环境准备

### 2.1 NuGet包依赖

在`LYBT.Module.Auth.csproj`中引用:

```xml
<ItemGroup>
  <!-- JWT认证 -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />

  <!-- 密码哈希 -->
  <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />

  <!-- 配置绑定 -->
  <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
</ItemGroup>
```

在`LYBT.WebAPI.csproj`中引用:

```xml
<ItemGroup>
  <!-- 认证中间件 -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />

  <!-- 速率限制（防暴力破解） -->
  <PackageReference Include="System.Threading.RateLimiting" Version="8.0.0" />
</ItemGroup>
```

### 2.2 appsettings.json配置

```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "YourSecretKeyMustBeAtLeast32CharactersLong",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client",
        "ExpirationHours": 8
      }
    },
    "Business": {
      "SystemAdmin": {
        "UserName": "clinic_admin",
        "Email": "admin@lybt.com"
      }
    },
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "Login": {
          "PermitLimit": 5,
          "Window": "00:01:00"
        }
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LYBT;..."
  }
}
```

### 2.3 生产环境密钥生成

**PowerShell生成命令**:

```powershell
# 生成64字节随机密钥（Base64编码后约88字符）
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

**环境变量配置**（生产环境推荐）:

```bash
# Linux/MacOS
export JWT_SECRET="YourGeneratedSecretKeyFromAboveCommand"

# Windows PowerShell
$env:JWT_SECRET="YourGeneratedSecretKeyFromAboveCommand"

# Docker Compose
services:
  webapi:
    environment:
      - JWT_SECRET=${JWT_SECRET}
```

### 2.4 验证配置

```bash
# 方法1: 启动应用观察日志
dotnet run --project src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj

# 预期日志输出:
# ✅ JWT密钥验证通过 (长度: 88字符)
# ✅ Production配置验证通过

# 方法2: 集成测试验证
dotnet test tests/IntegrationTests/LYBT.Module.Auth.IntegrationTests/LYBT.Module.Auth.IntegrationTests.csproj
```

---

## 3. JWT服务实现

### 3.1 IJwtService接口定义

```csharp
// 位置: src/Server/Interfaces/LYBT.Server.Interfaces/Services/IJwtService.cs

using System.Security.Claims;
using LYBT.Entities;

namespace LYBT.Server.Interfaces.Services;

/// <summary>
/// JWT令牌服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <returns>JWT Token字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role);

    /// <summary>
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <param name="additionalClaims">额外的Claims（如IsSuperAdmin）</param>
    /// <returns>JWT Token字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims);

    /// <summary>
    /// 验证JWT令牌并返回Claims主体
    /// </summary>
    /// <param name="token">JWT Token字符串</param>
    /// <returns>验证成功返回ClaimsPrincipal，失败返回null</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
```

### 3.2 JwtService完整实现

```csharp
// 位置: src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LYBT.Entities;
using LYBT.Infrastructure.Configuration;
using LYBT.Module.Auth.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Module.Auth.Services;

public class JwtService : IJwtService
{
    private readonly LybtOptions _options;
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtService(IOptions<LybtOptions> options, IConfiguration configuration)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenHandler = new JwtSecurityTokenHandler();

        // 启动时验证JWT密钥强度（方案A：最小加固）
        ValidateSecretKeyStrength();
    }

    /// <summary>
    /// 验证JWT密钥强度,确保符合安全基线要求
    /// </summary>
    private void ValidateSecretKeyStrength()
    {
        var secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey未配置。请在appsettings.json中设置Lybt:Authentication:Jwt:SecretKey配置项。");
        }

        if (secretKey.Length < 32)
        {
            throw new ArgumentException(
                $"JWT SecretKey长度不足,需至少32字符(当前{secretKey.Length}字符)。" +
                "这是安全基线要求,可使用以下命令生成符合要求的密钥:\n" +
                "PowerShell: [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))");
        }
    }

    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        // 直接从配置读取JWT密钥（解决配置绑定问题）
        var secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey配置未找到或为空。请检查appsettings.json中的Lybt:Authentication:Jwt:SecretKey配置。");
        }

        var jwtConfig = _options.Authentication.Jwt;

        // 创建Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 创建签名密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 设置合理的过期时间（8小时，符合适度设计原则）
        var expirationHours = 8;
        var expires = DateTime.UtcNow.AddHours(expirationHours);

        // 创建Token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwtConfig.Issuer,
            Audience = jwtConfig.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    public string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        var secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey配置未找到或为空。");
        }

        var jwtConfig = _options.Authentication.Jwt;

        // 创建基础Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 添加额外的Claims
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwtConfig.Issuer,
            Audience = jwtConfig.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 验证JWT令牌并返回Claims主体
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var jwtConfig = _options.Authentication.Jwt;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // 5分钟时钟偏差容忍
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            // 验证失败，返回null
            return null;
        }
    }
}
```

**关键点**:
- ✅ 启动时强制验证密钥长度（≥32字符）
- ✅ 支持额外Claims（如IsSuperAdmin标记）
- ✅ 使用HMAC-SHA256签名算法
- ✅ 8小时过期时间（适度设计原则）
- ✅ 5分钟时钟偏差容忍（避免服务器时间不同步问题）

---

## 4. 认证服务实现

### 4.1 IAuthService接口定义

```csharp
// 位置: src/Server/Interfaces/LYBT.Server.Interfaces/Services/IAuthService.cs

using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Server.Interfaces.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 验证用户凭证
    /// </summary>
    Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登录
    /// </summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);

    /// <summary>
    /// 验证令牌
    /// </summary>
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);

    /// <summary>
    /// 获取会话信息
    /// </summary>
    Task<ServiceResult<object>> GetSessionInfoAsync(string token);

    /// <summary>
    /// 修改系统管理员密码
    /// </summary>
    Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request);
}
```

### 4.2 AuthService完整实现

```csharp
// 位置: src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs

using AutoMapper;
using LYBT.Entities;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(
        IJwtService jwtService,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<AuthService> logger,
        AppDbContext dbContext,
        IConfiguration configuration)
    {
        _jwtService = jwtService;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    #region 超级管理员认证

    /// <summary>
    /// 检查是否为超级管理员凭据
    /// 超级管理员不在Users表中，密码哈希独立存储在AdminSecrets表
    /// 用户名从配置文件读取，不存储在数据库中，防止SQL注入后暴露账户名
    /// </summary>
    private async Task<bool> IsSuperAdminCredentials(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            // 从配置获取超级管理员用户名
            var configUsername = _configuration["Lybt:Business:SystemAdmin:UserName"];
            if (string.IsNullOrEmpty(configUsername))
            {
                _logger.LogWarning("配置中未找到超级管理员用户名");
                return false;
            }

            // 验证用户名是否匹配
            if (!string.Equals(username, configUsername, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 从AdminSecrets表获取超级管理员密码哈希
            var adminSecret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(cancellationToken);
            if (adminSecret == null)
            {
                _logger.LogWarning("AdminSecrets表为空，超级管理员未初始化");
                return false;
            }

            // 使用BCrypt验证密码（与普通用户一致）
            bool isValid = BCrypt.Net.BCrypt.Verify(password, adminSecret.PasswordHash);

            if (isValid)
            {
                _logger.LogInformation("超级管理员登录成功");
            }
            else
            {
                _logger.LogWarning("超级管理员认证失败：密码错误");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证超级管理员凭据时发生错误");
            return false;
        }
    }

    #endregion

    #region 核心认证操作

    /// <summary>
    /// 验证用户凭证
    /// Issue #1008: 改为直接使用IUserRepository和BCrypt验证
    /// </summary>
    public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
            return ServiceResult<string>.Failure("用户名和密码不能为空");

        try
        {
            // 首先检查是否是超级管理员登录
            if (await IsSuperAdminCredentials(request.UserName, request.Password, cancellationToken))
            {
                _logger.LogInformation("超级管理员认证成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
                // 返回特殊的超级管理员标识
                return ServiceResult<string>.Success("SUPER_ADMIN:" + request.UserName);
            }

            // 普通用户认证流程 - 直接调用Repository
            var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
            if (userEntity == null)
                return ServiceResult<string>.Failure("用户名或密码错误");

            // 直接使用BCrypt验证密码
            if (BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
            {
                _logger.LogInformation("用户认证成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
                return ServiceResult<string>.Success(userEntity.Id.ToString());
            }

            _logger.LogWarning("用户认证失败 [用户名: {UserName}] [原因: 密码错误] [时间: {Timestamp}]",
                request.UserName, DateTime.UtcNow);
            return ServiceResult<string>.Failure("用户名或密码错误");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证用户凭据时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
            return ServiceResult<string>.Failure("认证过程中发生错误");
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证凭据
            var credentialsResult = await VerifyCredentialsAsync(request, cancellationToken);
            if (!credentialsResult.IsSuccess)
                return ServiceResult<LoginResponse>.Failure(credentialsResult.Message ?? "凭据验证失败");

            LoginResponse response;

            // 检查是否是超级管理员
            if (credentialsResult.Data != null && credentialsResult.Data.StartsWith("SUPER_ADMIN:"))
            {
                // 超级管理员登录
                var sysAdminUsername = credentialsResult.Data.Substring("SUPER_ADMIN:".Length);

                // 生成超级管理员专用的JWT令牌
                var token = _jwtService.GenerateToken(
                    "00000000-0000-0000-0000-000000000000", // 特殊ID表示超级管理员
                    sysAdminUsername,
                    UserRole.Admin,
                    new Dictionary<string, string>
                    {
                        { "IsSuperAdmin", "true" },
                        { "AuthSource", "AdminSecrets" }
                    });

                response = new LoginResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = Guid.Empty, // 特殊ID
                        UserName = sysAdminUsername,
                        RealName = "系统超级管理员",
                        Role = UserRole.Admin,
                        Email = _configuration["Lybt:Business:SystemAdmin:Email"] ?? "admin@lybt.com"
                    },
                    RefreshToken = "", // 简化版本不使用RefreshToken
                    ExpiresAt = DateTime.UtcNow.AddHours(8)
                };

                _logger.LogInformation("超级管理员登录成功（用户名已隐藏）");
            }
            else
            {
                // 普通用户登录流程
                var userEntity = await _userRepository.GetByUsernameAsync(request.UserName);
                if (userEntity == null)
                    return ServiceResult<LoginResponse>.Failure("获取用户信息失败");

                var userDto = _mapper.Map<UserDto>(userEntity);

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(
                    userDto.Id.ToString(),
                    userDto.UserName,
                    userDto.Role);

                response = new LoginResponse
                {
                    Token = token,
                    User = userDto,
                    RefreshToken = "", // 简化版本不使用RefreshToken
                    ExpiresAt = DateTime.UtcNow.AddHours(8)
                };

                _logger.LogInformation("用户登录成功 [用户名: {UserName}] [时间: {Timestamp}]",
                    request.UserName, DateTime.UtcNow);
            }

            return ServiceResult<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录时发生错误 [时间: {Timestamp}]", DateTime.UtcNow);
            return ServiceResult<LoginResponse>.Failure("登录过程中发生错误");
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
    {
        // 简化实现：无状态JWT，登出仅在客户端清除令牌
        await Task.CompletedTask;
        return ServiceResult<bool>.Success(true, "登出成功");
    }

    /// <summary>
    /// 验证令牌
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
    {
        await Task.CompletedTask;

        try
        {
            var principal = _jwtService.ValidateToken(token);
            return ServiceResult<bool>.Success(principal != null);
        }
        catch
        {
            return ServiceResult<bool>.Success(false);
        }
    }

    /// <summary>
    /// 获取会话信息
    /// </summary>
    public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
    {
        await Task.CompletedTask;

        try
        {
            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
                return ServiceResult<object>.Failure("令牌无效");

            var sessionInfo = new
            {
                UserId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                Role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            };

            return ServiceResult<object>.Success(sessionInfo);
        }
        catch
        {
            return ServiceResult<object>.Failure("获取会话信息失败");
        }
    }

    /// <summary>
    /// 修改系统管理员密码
    /// </summary>
    public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
    {
        // 简化实现：暂不支持此功能
        await Task.CompletedTask;
        return ServiceResult<bool>.Failure("系统管理员密码修改功能暂未实现");
    }

    #endregion
}
```

**关键点**:
- ✅ 双轨认证机制（普通用户 + 超级管理员）
- ✅ BCrypt密码验证（防彩虹表攻击）
- ✅ 结构化日志（记录认证成功/失败）
- ✅ 异常安全（所有方法包裹try-catch）
- ✅ 超级管理员用户名隔离（配置文件 vs 数据库）

---

## 5. 认证中间件配置

### 5.1 Program.cs服务注册

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Program.cs

using LYBT.WebAPI.Extensions;
using Serilog;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 配置主机和服务
        builder.Host.ConfigureEnvironmentAwareHosting();
        builder.Host.UseSerilog();

        // ⭐ 统一服务注册（包含认证服务）
        builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

        var app = builder.Build();

        // ⭐ 配置中间件（包含认证中间件）
        app.ConfigureAllMiddleware();

        await app.RunAsync();
    }
}
```

### 5.2 认证服务注册扩展

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs
// 更新说明（Issue #1732 Phase 2.5）: 服务注册已拆分为专责扩展类

using System.Text;
using LYBT.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterAllApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // ... 其他服务注册

        // ⭐ 认证服务注册（实际实现在AuthenticationServiceCollectionExtensions）
        services.RegisterAuthenticationServices(configuration);

        return services;
    }

    private static IServiceCollection RegisterAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var lybtOptions = configuration.GetLybtOptions();

        // 读取JWT密钥（环境变量优先）
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                       lybtOptions.Authentication.Jwt.SecretKey;

        if (string.IsNullOrEmpty(jwtSecret))
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("生产环境必须配置JWT密钥（JWT_SECRET或Lybt:Authentication:Jwt:SecretKey）。");
            }

            jwtSecret = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
        }

        var jwtConfig = lybtOptions.Authentication.Jwt;
        var issuer = jwtConfig.Issuer;
        var audience = jwtConfig.Audience;

        // ⭐ 配置JWT Bearer认证
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // 基本验证设置
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                // 发行者和接收者
                ValidIssuer = issuer,
                ValidAudience = audience,

                // 密钥设置
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

                // 时钟偏差
                ClockSkew = TimeSpan.FromSeconds(300), // 5分钟

                // 增强安全设置
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateTokenReplay = false,

                // Token类型验证
                ValidTypes = new[] { "JWT" },

                // 严格的签名验证
                TryAllIssuerSigningKeys = true // 支持密钥轮换
            };
        });

        // ⭐ 配置授权策略
        services.AddAuthorization(options =>
        {
            // 默认策略 - 要求所有端点默认需要认证
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 定义基于角色的策略
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy("DoctorOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Doctor", "Admin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
```

### 5.3 认证中间件配置

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs

namespace LYBT.WebAPI.Extensions;

public static class UnifiedMiddlewareConfiguration
{
    public static WebApplication ConfigureAllMiddleware(this WebApplication app)
    {
        // ===== 阶段1: 错误处理和安全 =====
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseSecurityHeaders();

        // ===== 阶段2: 性能优化 =====
        app.UseResponseCompression();

        // ===== 阶段3: 路由和请求处理 =====
        app.UseRouting(); // ⚠️ 必须在认证之前

        // 速率限制（防暴力破解登录）
        var config = app.Configuration.GetLybtOptions();
        if (config.Security.RateLimiting.Enabled)
        {
            app.UseRateLimiter();
        }

        // ===== 阶段4: 认证和授权 =====
        // ⭐ 认证中间件（验证JWT Token）
        app.UseAuthentication();

        // ⭐ Claims标准化（在认证后，授权前）
        app.UseClaimsNormalization();

        // ⭐ 授权中间件（检查权限）
        app.UseAuthorization();

        // ===== 阶段5: 缓存 =====
        app.UseResponseCaching();
        app.UseOutputCache();

        // ===== 阶段6: API文档 =====
        app.ConfigureSwaggerMiddleware();

        // ===== 阶段7: 终端映射 =====
        app.MapControllers();

        return app;
    }
}
```

**中间件顺序说明**:

```
1. UseRouting() → 建立路由匹配
2. UseRateLimiter() → 限流（防暴力破解）
3. UseAuthentication() → 验证JWT Token，填充User.Identity
4. UseClaimsNormalization() → 标准化Claims（可选）
5. UseAuthorization() → 检查[Authorize]特性权限
6. MapControllers() → 路由到Controller方法
```

⚠️ **顺序错误会导致**:
- ❌ UseAuthentication()在UseAuthorization()之后 → 401错误
- ❌ UseRouting()在UseAuthentication()之后 → 路由失效
- ❌ UseRateLimiter()在UseRouting()之前 → 限流失效

---

## 6. AuthController实现

### 6.1 Controller基类BaseApiController

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Controllers/BaseApiController.cs

using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// API控制器基类 - 提供统一的响应格式和错误处理
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;
    protected readonly IMemoryCache? _cache;

    protected BaseApiController(ILogger logger, IMemoryCache? cache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
    }

    /// <summary>
    /// 处理ServiceResult并返回标准ApiResponse
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> result, string successMessage = "操作成功")
    {
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<T>.SuccessResponse(result.Data!, result.Message ?? successMessage));
        }
        else
        {
            return BadRequest(ApiResponse<T>.ErrorResponse(result.Message ?? "操作失败"));
        }
    }

    /// <summary>
    /// 处理bool类型ServiceResult
    /// </summary>
    protected ActionResult<ApiResponse> HandleBoolServiceResult(ServiceResult<bool> result, string successMessage = "操作成功")
    {
        if (result.IsSuccess && result.Data)
        {
            return Ok(ApiResponse.SuccessResponse(result.Message ?? successMessage));
        }
        else
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message ?? "操作失败"));
        }
    }

    /// <summary>
    /// 返回验证失败响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> ValidationFail<T>(string message)
    {
        return BadRequest(ApiResponse<T>.ValidationErrorResponse(message));
    }

    protected ActionResult<ApiResponse> ValidationFail(string message)
    {
        return BadRequest(ApiResponse.ValidationErrorResponse(message));
    }

    /// <summary>
    /// 返回成功响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    protected ActionResult<ApiResponse> Success(string message = "操作成功")
    {
        return Ok(ApiResponse.SuccessResponse(message));
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operationName, object? requestData = null)
    {
        _logger.LogError(ex, "{Operation}失败 [请求数据: {RequestData}]", operationName, requestData);
        return StatusCode(500, ApiResponse<T>.ErrorResponse($"{operationName}失败: {ex.Message}"));
    }

    protected ActionResult<ApiResponse> HandleException(Exception ex, string operationName, object? requestData = null)
    {
        _logger.LogError(ex, "{Operation}失败 [请求数据: {RequestData}]", operationName, requestData);
        return StatusCode(500, ApiResponse.ErrorResponse($"{operationName}失败: {ex.Message}"));
    }

    /// <summary>
    /// 验证ModelState
    /// </summary>
    protected ActionResult<ApiResponse<T>>? ValidateModel<T>()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return ValidationFail<T>(string.Join("; ", errors));
        }
        return null;
    }

    protected ActionResult<ApiResponse>? ValidateModel()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return ValidationFail(string.Join("; ", errors));
        }
        return null;
    }
}
```

### 6.2 AuthController完整实现

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs

using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]  // 默认需要认证，公开端点使用AllowAnonymous覆盖
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IConfiguration Configuration;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        IMemoryCache cache,
        IConfiguration configuration)
        : base(logger, cache)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录响应，包含JWT Token</returns>
    [HttpPost("login")]
    [AllowAnonymous]  // 登录端点允许匿名访问
    [EnableRateLimiting("Login")]  // 启用登录限流保护，防暴力破解
    public async Task<ActionResult<ApiResponse<LoginResponse>>> LoginAsync([FromBody] LoginRequest request)
    {
        try
        {
            // 参数验证
            var validation = ValidateModel<LoginResponse>();
            if (validation != null)
            {
                return validation;
            }

            if (request == null)
            {
                return ValidationFail<LoginResponse>("登录请求不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return ValidationFail<LoginResponse>("用户名不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return ValidationFail<LoginResponse>("密码不能为空");
            }

            // 调用认证服务进行登录
            var result = await _authService.LoginAsync(request);
            return HandleServiceResult(result, "登录成功");
        }
        catch (Exception ex)
        {
            return HandleException<LoginResponse>(ex, "用户登录", request);
        }
    }

    /// <summary>
    /// 超级管理员登录（隐藏端点）
    /// 专用的超级管理员登录接口，用户名从配置读取，只需提供密码
    /// </summary>
    /// <param name="request">超级管理员登录请求（只包含密码）</param>
    /// <returns>登录响应，包含JWT Token</returns>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [ApiExplorerSettings(IgnoreApi = true)]  // 从Swagger文档中隐藏此端点
    public async Task<ActionResult<ApiResponse<LoginResponse>>> SuperAdminLoginAsync([FromBody] SuperAdminLoginRequest request)
    {
        try
        {
            var validation = ValidateModel<LoginResponse>();
            if (validation != null)
            {
                return validation;
            }

            if (request == null)
            {
                return ValidationFail<LoginResponse>("登录请求不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return ValidationFail<LoginResponse>("密码不能为空");
            }

            // 从配置获取超级管理员用户名
            var sysAdminUsername = Configuration["Lybt:Business:SystemAdmin:UserName"] ?? "clinic_admin";

            // 构造标准登录请求
            var loginRequest = new LoginRequest
            {
                UserName = sysAdminUsername,
                Password = request.Password,
                RememberMe = false
            };

            // 调用认证服务进行登录
            var result = await _authService.LoginAsync(loginRequest);

            // 如果登录成功且是超级管理员，返回成功
            if (result.IsSuccess && result.Data != null && result.Data.User.Id == Guid.Empty)
            {
                return HandleServiceResult(result, "超级管理员登录成功");
            }

            // 登录失败或不是超级管理员
            return ValidationFail<LoginResponse>("认证失败");
        }
        catch (Exception ex)
        {
            return HandleException<LoginResponse>(ex, "超级管理员登录", request);
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <param name="request">登出请求</param>
    /// <returns>登出结果</returns>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> LogoutAsync([FromBody] LogoutRequest request)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null)
            {
                return validation;
            }

            if (request == null)
            {
                return ValidationFail("登出请求不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return ValidationFail("用户名不能为空");
            }

            // 调用认证服务进行登出
            var result = await _authService.LogoutAsync(request);
            return HandleBoolServiceResult(result, "登出成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "用户登出", request);
        }
    }

    /// <summary>
    /// 验证Token (GET方法)
    /// 从Authorization header中获取Bearer Token进行验证
    /// </summary>
    /// <returns>验证结果包含token有效性、用户信息和过期时间</returns>
    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<object>>> ValidateTokenFromHeaderAsync()
    {
        try
        {
            // 从Authorization header中提取Token
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return Unauthorized(new { valid = false, message = "Missing Authorization header" });
            }

            // 检查Bearer格式
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { valid = false, message = "Invalid Authorization header format" });
            }

            // 提取token
            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized(new { valid = false, message = "Missing token in Authorization header" });
            }

            // 调用认证服务验证Token
            var result = await _authService.ValidateTokenAsync(token);

            if (result.IsSuccess && result.Data == true)
            {
                // Token有效，返回详细信息
                var sessionInfo = await _authService.GetSessionInfoAsync(token);
                object response = new
                {
                    valid = true,
                    sub = sessionInfo.Data,
                    message = "Token is valid"
                };
                return Success(response, "Token验证成功");
            }
            else
            {
                // Token无效
                return Unauthorized(new { valid = false, message = result.ErrorMessage ?? "Token is invalid" });
            }
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "验证Token从Header", null);
        }
    }

    /// <summary>
    /// 验证Token (POST方法)
    /// </summary>
    /// <param name="token">要验证的Token</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate")]
    [AllowAnonymous]  // Token验证端点需要允许匿名访问（通过参数传递token）
    public async Task<ActionResult<ApiResponse<bool>>> ValidateTokenAsync([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ValidationFail<bool>("Token不能为空");
            }

            // 调用认证服务验证Token
            var result = await _authService.ValidateTokenAsync(token);
            return HandleServiceResult(result, "Token验证完成");
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, "验证Token", token);
        }
    }

    /// <summary>
    /// 修改系统管理员密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <returns>修改结果</returns>
    [HttpPost("changeSysAdminPassword")]
    [Authorize(Roles = "Admin")]  // 仅管理员可访问
    public async Task<ActionResult<ApiResponse>> ChangeSysAdminPasswordAsync([FromBody] ChangeSysAdminPassword request)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null)
            {
                return validation;
            }

            if (request == null)
            {
                return ValidationFail("修改密码请求不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ValidationFail("新密码不能为空");
            }

            // 简化密码验证：仅检查长度（适度设计原则）
            if (request.NewPassword.Length < 6)
            {
                return ValidationFail("新密码长度不能少于6位");
            }

            // 调用认证服务修改密码
            var result = await _authService.ChangeSysAdminPasswordAsync(request);
            return HandleBoolServiceResult(result, "密码修改成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "修改密码", request);
        }
    }

    /// <summary>
    /// Auth基础端点 - 返回405 Method Not Allowed
    /// 用于冒烟测试验证路由存在
    /// </summary>
    /// <returns>405 Method Not Allowed</returns>
    [HttpGet]
    public ActionResult Get()
    {
        return StatusCode(405, new { message = "Method Not Allowed - Use POST endpoints for authentication" });
    }
}
```

**关键点**:
- ✅ [AllowAnonymous] 允许登录端点匿名访问
- ✅ [EnableRateLimiting("Login")] 防暴力破解
- ✅ [ApiExplorerSettings(IgnoreApi = true)] 隐藏超级管理员端点
- ✅ [Authorize(Roles = "Admin")] 限制密码修改仅管理员可用
- ✅ 统一的错误处理和响应格式

---

## 7. 速率限制配置（防暴力破解）

### 7.1 速率限制策略配置

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Extensions/RateLimitingExtensions.cs

using System.Threading.RateLimiting;
using LYBT.Infrastructure.Configuration;

namespace LYBT.WebAPI.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var lybtOptions = configuration.GetLybtOptions();
        var rateLimitConfig = lybtOptions.Security.RateLimiting;

        if (!rateLimitConfig.Enabled)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            // ⭐ Login端点限流（防暴力破解）
            options.AddPolicy("Login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.Login.PermitLimit,
                        Window = TimeSpan.Parse(rateLimitConfig.Login.Window),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // 不允许排队
                    }));

            // 全局限流（可选）
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                // 排除登录端点（已有专用限流）
                if (httpContext.Request.Path.StartsWithSegments("/api/v1/auth/login"))
                {
                    return RateLimitPartition.GetNoLimiter("auth");
                }

                // 其他端点按IP限流
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "global",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100, // 每分钟100请求
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // 限流触发时的响应
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                await context.HttpContext.Response.WriteAsync(
                    "请求过于频繁，请稍后再试。",
                    cancellationToken: token);
            };
        });

        return services;
    }
}
```

### 7.2 appsettings.json速率限制配置

```json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "Login": {
          "PermitLimit": 5,
          "Window": "00:01:00"
        }
      }
    }
  }
}
```

**说明**:
- ✅ 登录端点：每IP每分钟最多5次尝试
- ✅ 全局限流：每IP每分钟最多100请求
- ✅ 按IP分区（RemoteIpAddress）
- ✅ 超限返回429状态码

---

## 8. 端点权限控制

### 8.1 授权特性使用

**常用特性**:

```csharp
// 1. 允许匿名访问
[AllowAnonymous]
public async Task<ActionResult> PublicEndpoint() { }

// 2. 要求认证（任何登录用户）
[Authorize]
public async Task<ActionResult> ProtectedEndpoint() { }

// 3. 要求特定角色
[Authorize(Roles = "Admin")]
public async Task<ActionResult> AdminOnlyEndpoint() { }

// 4. 要求多个角色之一
[Authorize(Roles = "Doctor,Admin")]
public async Task<ActionResult> DoctorOrAdminEndpoint() { }

// 5. 使用自定义策略
[Authorize(Policy = "DoctorOrAdmin")]
public async Task<ActionResult> CustomPolicyEndpoint() { }
```

### 8.2 实际应用示例

```csharp
// 患者管理Controller示例
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Controller级别默认需要认证
public class PatientsController : BaseApiController
{
    // ✅ 继承Controller级别的[Authorize]，所有医生和管理员都可访问
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetPatients()
    {
        // 实现逻辑
    }

    // ✅ 创建患者需要认证（继承Controller级别）
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> CreatePatient([FromBody] CreatePatientDto dto)
    {
        // 实现逻辑
    }

    // ⚠️ 删除患者仅管理员可用（覆盖Controller级别）
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeletePatient(Guid id)
    {
        // 实现逻辑
    }
}

// Swagger端点示例（允许匿名）
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous] // Controller级别允许匿名
public class SwaggerController : ControllerBase
{
    [HttpGet("swagger.json")]
    public ActionResult GetSwaggerJson()
    {
        // 实现逻辑
    }
}
```

### 8.3 在Service层获取当前用户

```csharp
// Service层获取当前登录用户ID
public class PatientService : IPatientService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatientService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
    {
        // 从HttpContext获取当前用户Claims
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return ServiceResult<PatientDto>.Failure("用户未认证");
        }

        // 获取用户ID（从NameIdentifier Claim）
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return ServiceResult<PatientDto>.Failure("无法获取用户ID");
        }

        var userId = Guid.Parse(userIdClaim.Value);

        // 获取用户名
        var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

        // 获取用户角色
        var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        // 业务逻辑：创建患者并记录创建者
        var patient = new PatientModel
        {
            // ... 患者属性
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _patientRepository.AddAsync(patient);

        return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(patient));
    }
}
```

**注意事项**:
- ✅ 必须在Startup中注册IHttpContextAccessor
- ✅ Claims通过`httpContext.User.FindFirst(ClaimTypes.XXX)`获取
- ⚠️ 在非HTTP上下文（如后台任务）中无法使用

### 8.4 注册IHttpContextAccessor

```csharp
// 在ServiceCollectionExtensions.cs或相关扩展类中注册（Issue #1732 Phase 2.5更新）
services.AddHttpContextAccessor();
```

---

## 9. Swagger集成JWT认证

### 9.1 Swagger配置JWT Bearer

```csharp
// 位置: src/Server/Services/LYBT.WebAPI/Extensions/SwaggerExtensions.cs

using Microsoft.OpenApi.Models;

namespace LYBT.WebAPI.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LYBT WebAPI",
                Version = "v1",
                Description = "凌隐宝堂中医诊所诊疗系统API文档"
            });

            // ⭐ 添加JWT Bearer认证支持
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT授权认证。在下方输入框中输入Bearer Token（格式：Bearer {token}）",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // 添加XML注释文档
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
```

### 9.2 使用Swagger测试认证

**步骤**:

1. **启动应用并访问Swagger UI**:
   ```
   https://localhost:5001/swagger
   ```

2. **登录获取Token**:
   - 找到`POST /api/v1/auth/login`端点
   - 点击"Try it out"
   - 输入用户名和密码:
     ```json
     {
       "userName": "doctor1",
       "password": "123456"
     }
     ```
   - 点击"Execute"
   - 复制返回的`token`字段值

3. **配置Swagger Bearer Token**:
   - 点击页面右上角的"Authorize"按钮
   - 在弹出框中输入：`Bearer {你的token}`
   - 点击"Authorize"，然后"Close"

4. **测试受保护端点**:
   - 找到`GET /api/v1/patients`端点
   - 点击"Try it out"，然后"Execute"
   - 观察到请求自动携带`Authorization: Bearer {token}`头
   - 返回200成功响应

---

## 10. 生产环境安全加固

### 10.1 环境变量管理

**方案A: 使用.env文件（本地开发）**

```bash
# .env文件（不提交到Git）
JWT_SECRET=YourProductionSecretKeyGeneratedBySecureRandomGenerator
CONNECTION_STRING=Server=prod-db.example.com;Database=LYBT;User Id=lybuser;Password=***;
```

**加载.env文件**（已在Program.cs实现）:

```csharp
// 在Program.cs中自动加载
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (environment == "Development")
{
    // 加载.env文件
    DotNetEnv.Env.Load();
}
```

**方案B: 使用Azure Key Vault（生产推荐）**

```csharp
// 在Program.cs中配置Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**方案C: 使用Docker Secrets**

```yaml
# docker-compose.yml
services:
  webapi:
    image: lybt-webapi:latest
    secrets:
      - jwt_secret
    environment:
      - JWT_SECRET_FILE=/run/secrets/jwt_secret

secrets:
  jwt_secret:
    file: ./secrets/jwt_secret.txt
```

### 10.2 密钥轮换策略

**实施步骤**:

1. **生成新密钥**:
   ```powershell
   $newSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
   Write-Output $newSecret
   ```

2. **配置多密钥验证**（在TokenValidationParameters中）:
   ```csharp
   options.TokenValidationParameters = new TokenValidationParameters
   {
       // 支持多密钥验证
       IssuerSigningKeys = new[]
       {
           new SymmetricSecurityKey(Encoding.UTF8.GetBytes(oldSecret)),
           new SymmetricSecurityKey(Encoding.UTF8.GetBytes(newSecret))
       },
       TryAllIssuerSigningKeys = true, // ⚠️ 必须设置
       // ... 其他配置
   };
   ```

3. **更新密钥**:
   - 第1天：添加新密钥到`IssuerSigningKeys`数组
   - 第2-7天：旧Token逐渐过期，新Token使用新密钥
   - 第8天：移除旧密钥

4. **验证轮换成功**:
   ```bash
   # 使用旧Token测试（应该在第8天后失败）
   curl -H "Authorization: Bearer {old_token}" https://api.lybt.com/api/v1/patients
   ```

### 10.3 HTTPS强制和HSTS

```csharp
// 在UnifiedMiddlewareConfiguration.cs中配置
if (!app.Environment.IsDevelopment())
{
    // ⭐ HTTPS重定向
    app.UseHttpsRedirection();

    // ⭐ HSTS（HTTP Strict Transport Security）
    app.UseHsts();
}
```

**appsettings.json配置**:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "path/to/cert.pfx",
          "Password": "cert_password"
        }
      }
    }
  }
}
```

### 10.4 生产配置验证

```csharp
// 位置: src/Server/Infrastructure/LYBT.Infrastructure/Configuration/Validation/ProductionConfigurationValidator.cs

public class ProductionConfigurationValidator
{
    private readonly IConfiguration _configuration;

    public ProductionConfigurationValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ValidateOrThrow()
    {
        var errors = new List<string>();

        // ⭐ JWT密钥验证
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                       _configuration["Lybt:Authentication:Jwt:SecretKey"];

        if (string.IsNullOrEmpty(jwtSecret))
        {
            errors.Add("生产环境必须配置JWT_SECRET环境变量或Lybt:Authentication:Jwt:SecretKey");
        }
        else if (jwtSecret.Length < 32)
        {
            errors.Add($"JWT密钥长度不足（当前{jwtSecret.Length}字符，需要≥32字符）");
        }
        else if (jwtSecret.Contains("Development") || jwtSecret.Contains("Default"))
        {
            errors.Add("生产环境不能使用开发环境默认密钥");
        }

        // ⭐ 数据库连接字符串验证
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            errors.Add("生产环境必须配置数据库连接字符串");
        }
        else if (connectionString.Contains("localhost") || connectionString.Contains("127.0.0.1"))
        {
            errors.Add("生产环境不应使用localhost数据库");
        }

        // ⭐ HTTPS验证
        var useHttps = _configuration.GetValue<bool>("Lybt:Security:RequireHttps", true);
        if (!useHttps)
        {
            errors.Add("生产环境必须启用HTTPS");
        }

        if (errors.Any())
        {
            throw new ProductionConfigurationException(
                "生产环境配置验证失败:\n" + string.Join("\n", errors.Select((e, i) => $"{i + 1}. {e}")));
        }
    }
}
```

**在Program.cs中调用验证**:

```csharp
if (builder.Environment.IsProduction())
{
    var validator = new ProductionConfigurationValidator(builder.Configuration);
    try
    {
        validator.ValidateOrThrow();
        Log.Information("✅ Production配置验证通过");
    }
    catch (ProductionConfigurationException ex)
    {
        Log.Fatal(ex, "❌ Production配置验证失败");
        Console.Error.WriteLine(ex.Message);
        Environment.Exit(1); // ⚠️ 配置错误时拒绝启动
    }
}
```

---

## 11. 集成测试

### 11.1 测试基类配置

```csharp
// 位置: tests/IntegrationTests/LYBT.Module.Auth.IntegrationTests/AuthIntegrationTestBase.cs

using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LYBT.Module.Auth.IntegrationTests;

public class AuthIntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    public AuthIntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 移除Production数据库配置
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 使用InMemory数据库
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // 初始化测试数据
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db);
            });
        });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    private void SeedTestData(AppDbContext db)
    {
        // 添加测试用户
        var doctor = new UserModel
        {
            Id = Guid.NewGuid(),
            UserName = "doctor1",
            RealName = "张医生",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = UserRole.Doctor,
            Email = "doctor1@lybt.com"
        };

        db.Users.Add(doctor);

        // 添加超级管理员密码哈希
        var adminSecret = new AdminSecretModel
        {
            Id = Guid.NewGuid(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            CreatedAt = DateTime.UtcNow
        };

        db.AdminSecrets.Add(adminSecret);
        db.SaveChanges();
    }

    /// <summary>
    /// 登录并获取Token
    /// </summary>
    protected async Task<string> GetAuthTokenAsync(string userName = "doctor1", string password = "123456")
    {
        var loginRequest = new LoginRequest
        {
            UserName = userName,
            Password = password
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return loginResponse!.Data!.Token;
    }

    /// <summary>
    /// 设置Authorization Header
    /// </summary>
    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// 清除Authorization Header
    /// </summary>
    protected void ClearAuthHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}
```

### 11.2 登录测试

```csharp
// 位置: tests/IntegrationTests/LYBT.Module.Auth.IntegrationTests/LoginTests.cs

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace LYBT.Module.Auth.IntegrationTests;

public class LoginTests : AuthIntegrationTestBase
{
    public LoginTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "doctor1",
            Password = "123456"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal("doctor1", result.Data.User.UserName);
        Assert.Equal(UserRole.Doctor, result.Data.User.Role);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "doctor1",
            Password = "wrong_password"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("用户名或密码错误", result.Message);
    }

    [Fact]
    public async Task Login_SuperAdmin_ReturnsTokenWithSuperAdminClaim()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "clinic_admin", // 从配置读取
            Password = "admin123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal(Guid.Empty, result.Data.User.Id); // 超级管理员ID为Empty
        Assert.Equal(UserRole.Admin, result.Data.User.Role);
    }
}
```

### 11.3 授权测试

```csharp
// 位置: tests/IntegrationTests/LYBT.Module.Auth.IntegrationTests/AuthorizationTests.cs

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LYBT.Module.Auth.IntegrationTests;

public class AuthorizationTests : AuthIntegrationTestBase
{
    public AuthorizationTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        ClearAuthHeader();

        // Act
        var response = await Client.GetAsync("/api/v1/patients");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithValidToken_Returns200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/v1/patients");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminOnlyEndpoint_WithDoctorToken_Returns403()
    {
        // Arrange
        var token = await GetAuthTokenAsync("doctor1", "123456");
        SetAuthHeader(token);

        // Act
        var response = await Client.DeleteAsync("/api/v1/patients/some-guid");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminOnlyEndpoint_WithAdminToken_Returns200()
    {
        // Arrange
        var token = await GetAuthTokenAsync("clinic_admin", "admin123");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", new
        {
            NewPassword = "newpassword123"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### 11.4 速率限制测试

```csharp
// 位置: tests/IntegrationTests/LYBT.Module.Auth.IntegrationTests/RateLimitTests.cs

using LYBT.Shared.Models.Contracts.Auth;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LYBT.Module.Auth.IntegrationTests;

public class RateLimitTests : AuthIntegrationTestBase
{
    public RateLimitTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Login_ExceedRateLimit_Returns429()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "doctor1",
            Password = "wrong_password"
        };

        // Act - 尝试6次登录（限制为5次/分钟）
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        }

        // Assert
        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode); // 429

        var content = await lastResponse.Content.ReadAsStringAsync();
        Assert.Contains("请求过于频繁", content);
    }
}
```

---

## 12. 常见问题与陷阱

### 陷阱1: 中间件顺序错误

❌ **错误示例**:

```csharp
public static WebApplication ConfigureAllMiddleware(this WebApplication app)
{
    app.UseRouting();
    app.UseAuthorization(); // ❌ 错误：在UseAuthentication()之前
    app.UseAuthentication();
    app.MapControllers();
    return app;
}
```

✅ **正确示例**:

```csharp
public static WebApplication ConfigureAllMiddleware(this WebApplication app)
{
    app.UseRouting();
    app.UseAuthentication(); // ✅ 认证在前
    app.UseAuthorization();  // ✅ 授权在后
    app.MapControllers();
    return app;
}
```

**原因**: `UseAuthorization()`依赖`UseAuthentication()`填充的`User.Identity`。

---

### 陷阱2: JWT密钥长度不足

❌ **错误示例**:

```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "short_key"
      }
    }
  }
}
```

**错误信息**:
```
JWT SecretKey长度不足,需至少32字符(当前9字符)。
```

✅ **正确示例**:

```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "YourSecretKeyMustBeAtLeast32CharactersLongForSecurityReasons"
      }
    }
  }
}
```

或使用PowerShell生成:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

---

### 陷阱3: 忘记注册IHttpContextAccessor

❌ **错误现象**:

```csharp
public class PatientService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatientService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor; // ❌ 运行时抛出异常：未注册
    }
}
```

✅ **正确做法**:

```csharp
// 在ServiceCollectionExtensions.cs或相关扩展类中注册（Issue #1732 Phase 2.5更新）
services.AddHttpContextAccessor();
```

---

### 陷阱4: 生产环境使用开发密钥

❌ **错误示例**:

```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction"
      }
    }
  }
}
```

**后果**: 攻击者可以伪造JWT Token,冒充任何用户。

✅ **正确做法**:

1. 使用环境变量:
   ```bash
   export JWT_SECRET="$(openssl rand -base64 64)"
   ```

2. 启用生产配置验证:
   ```csharp
   if (builder.Environment.IsProduction())
   {
       if (jwtSecret.Contains("Development") || jwtSecret.Contains("Default"))
       {
           throw new InvalidOperationException("生产环境不能使用开发环境默认密钥");
       }
   }
   ```

---

### 陷阱5: 未启用速率限制

❌ **错误配置**:

```json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "Enabled": false
      }
    }
  }
}
```

**后果**: 攻击者可以暴力破解登录密码（每秒尝试1000次）。

✅ **正确配置**:

```json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "Login": {
          "PermitLimit": 5,
          "Window": "00:01:00"
        }
      }
    }
  }
}
```

并在Controller上添加:

```csharp
[HttpPost("login")]
[EnableRateLimiting("Login")]
public async Task<ActionResult> LoginAsync([FromBody] LoginRequest request)
```

---

### 陷阱6: Token过期时间过长

❌ **错误配置**:

```csharp
var expires = DateTime.UtcNow.AddDays(365); // ❌ 365天
```

**后果**: Token泄露后长期有效,增加安全风险。

✅ **正确配置**:

```csharp
var expires = DateTime.UtcNow.AddHours(8); // ✅ 8小时（适度设计原则）
```

**权衡**:
- ✅ 8小时 - 平衡安全性和用户体验
- ⚠️ 15分钟 - 安全性高但用户频繁登录
- ❌ 30天 - 严重安全风险

---

## 13. 检查清单

### 13.1 开发阶段检查清单

**配置检查**:
- [ ] appsettings.json中JWT密钥长度≥32字符
- [ ] Issuer和Audience正确配置
- [ ] 速率限制已启用（RateLimiting.Enabled = true）
- [ ] 数据库连接字符串正确

**代码检查**:
- [ ] 中间件顺序正确（UseAuthentication → UseAuthorization）
- [ ] Controller标注[Authorize]或[AllowAnonymous]
- [ ] 登录端点添加[EnableRateLimiting("Login")]
- [ ] 敏感端点添加角色验证[Authorize(Roles = "Admin")]
- [ ] Service层正确获取当前用户（IHttpContextAccessor）

**测试检查**:
- [ ] 登录成功返回Token
- [ ] 错误密码返回401
- [ ] 无Token访问受保护端点返回401
- [ ] 非管理员访问管理员端点返回403
- [ ] 速率限制生效（第6次登录返回429）

### 13.2 生产部署检查清单

**安全配置**:
- [ ] JWT_SECRET环境变量已设置（非默认值）
- [ ] JWT密钥长度≥32字符
- [ ] HTTPS已启用（UseHttpsRedirection + UseHsts）
- [ ] 生产配置验证通过（ProductionConfigurationValidator）
- [ ] 数据库连接字符串不包含localhost
- [ ] AdminSecrets表已初始化

**密钥管理**:
- [ ] 密钥存储在Key Vault或环境变量（不在代码中）
- [ ] 密钥轮换策略已文档化
- [ ] 多密钥验证已配置（TryAllIssuerSigningKeys = true）

**监控告警**:
- [ ] 登录失败日志已配置
- [ ] 速率限制触发告警已配置
- [ ] Token验证失败监控已配置
- [ ] 超级管理员登录告警已配置

**文档完整性**:
- [ ] API文档包含JWT认证说明
- [ ] 密钥轮换流程已文档化
- [ ] 应急响应预案已准备（密钥泄露处理）

---

## 14. 参考资料

### 14.1 内部文档

**架构文档**:
- `docs/architecture/server/README.md` - Server端三层架构（8个模块、服务标准）
- `docs/architecture/server/webapi-design.md` - WebAPI层设计（中间件、Controller）
- `docs/architecture/server/auth-design.md` - 认证架构设计（双轨认证机制）

**开发指南**:
- `docs/how-to-guides/server/webapi-development.md` - WebAPI开发指南（Controller实现）
- `docs/how-to-guides/server/interfaces-usage.md` - Interfaces层使用指南（ServiceResult模式）
- `docs/how-to-guides/shared/dto-development.md` - DTO开发指南（LoginRequest/LoginResponse）

**快速参考**:
- `docs/quick-reference/api-reference.md` - API端点速查（/api/v1/auth/*）
- `docs/quick-reference/code-patterns.md` - 代码模式速查（认证相关模式）

### 14.2 外部资源

**官方文档**:
- [ASP.NET Core认证概述](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/)
- [JWT Bearer认证](https://learn.microsoft.com/zh-cn/aspnet/core/security/authentication/jwt)
- [速率限制中间件](https://learn.microsoft.com/zh-cn/aspnet/core/performance/rate-limit)
- [HTTPS强制和HSTS](https://learn.microsoft.com/zh-cn/aspnet/core/security/enforcing-ssl)

**安全最佳实践**:
- [OWASP认证备忘单](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [JWT最佳实践](https://tools.ietf.org/html/rfc8725)
- [密钥管理指南](https://learn.microsoft.com/zh-cn/azure/key-vault/general/best-practices)

**相关工具**:
- BCrypt.Net-Next - 密码哈希库
- System.IdentityModel.Tokens.Jwt - JWT处理库
- Microsoft.AspNetCore.Authentication.JwtBearer - JWT认证中间件

---

**最后更新**: 2025-10-30
**维护负责**: Server端开发组
**文档版本**: v1.0

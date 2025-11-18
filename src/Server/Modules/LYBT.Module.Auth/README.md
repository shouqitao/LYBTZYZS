# LYBT.Module.Auth - 身份认证与授权模块

## 📦 项目定位

- **层级**：Server端
- **类型**：业务模块(认证与授权)
- **职责**：提供JWT无状态认证、双轨认证架构（超级管理员物理隔离 + 普通用户标准认证）、RefreshToken机制和完整的会话管理功能。专为小型中医诊所场景优化，支持Admin/Doctor双角色管理，确保认证安全和令牌管理的高效性。采用标准三层架构（Controller → Service → JWT服务）,确保认证逻辑清晰、安全策略严格。

## 📂 代码结构

```
LYBT.Module.Auth/
├── AuthModule.cs                      # 模块依赖注入注册
│   └── AddAuthModule()                # 依赖注入配置(认证服务+JWT服务)
├── Interfaces/                        # 模块接口定义
│   └── IJwtService.cs                 # JWT服务接口(3个方法)
├── Services/                          # 业务逻辑实现
│   ├── AuthService.cs                 # 认证服务(9个方法)
│   │   ├── IsSuperAdminCredentials()  # 超级管理员凭证验证(双轨认证第一轨)
│   │   ├── VerifyCredentialsAsync()   # 普通用户凭证验证(双轨认证第二轨)
│   │   ├── LoginAsync()               # 用户登录(生成JWT + RefreshToken)
│   │   ├── LogoutAsync()              # 用户登出(撤销RefreshToken)
│   │   ├── RefreshTokenAsync()        # 刷新访问令牌(RefreshToken机制)
│   │   ├── ValidateTokenAsync()       # 验证JWT令牌有效性
│   │   ├── RevokeTokenAsync()         # 撤销RefreshToken
│   │   ├── GetSessionInfoAsync()      # 获取当前会话信息
│   │   └── ChangeSysAdminPasswordAsync() # 超级管理员密码修改
│   └── JwtService.cs                  # JWT令牌生成与验证服务(3个方法)
│       ├── GenerateToken()            # 生成JWT AccessToken(两个重载)
│       ├── ValidateToken()            # 验证令牌签名和有效期
│       └── ValidateSecretKeyStrength() # 密钥强度验证(安全性检查)
└── README.md
```

**说明**：
- **AuthModule**：依赖注入注册中心，统一注册认证服务和JWT服务
- **AuthService**：9个方法覆盖完整认证生命周期（双轨认证、登录、登出、令牌刷新、会话管理）
- **JwtService**：3个方法提供JWT令牌生成、验证和密钥强度检查
- **双轨认证架构**：超级管理员（AdminSecrets表物理隔离）+ 普通用户（Users表标准认证）
- **RefreshToken机制**：AccessToken 2小时有效期，RefreshToken 7天有效期，支持撤销
- **会话管理**：AuthSessions表记录登录历史、设备信息、过期时间
- **密码加密**：BCrypt哈希算法（工作因子12）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(User, AdminSecret, AuthSession)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(LoginRequest, LoginResponse, UserDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IAuthService基接口)
5. **LYBT.Shared.Utilities** - 共享工具类库(密码哈希、配置扩展)
6. **LYBT.Module.Users** - 用户管理模块(用户数据访问)

### 被依赖项目
1. **LYBT.WebAPI** - Web服务层通过AuthController暴露认证API（`/api/v1/auth/*`）
2. **所有需要认证的模块** - 通过JWT令牌验证访问权限

### NuGet包
- **AutoMapper** (13.0.x) - DTO与实体模型映射
- **Microsoft.EntityFrameworkCore** (8.0.x) - ORM框架
- **Microsoft.EntityFrameworkCore.SqlServer** (8.0.x) - SQL Server数据库提供程序
- **Microsoft.IdentityModel.Tokens** (8.3.x) - JWT令牌验证基础库
- **System.IdentityModel.Tokens.Jwt** (8.3.x) - JWT令牌生成与验证

## 🛠 技术栈

- **.NET 8**: 基础框架
- **JWT (JSON Web Tokens)**: 无状态认证令牌（AccessToken 2小时有效期）
- **RefreshToken机制**: 长期令牌（7天有效期），支持撤销
- **BCrypt.Net**: 密码哈希算法（通过LYBT.Shared.Utilities，工作因子12）
- **Entity Framework Core 8**: 通过仓储模式间接使用，用于数据持久化
- **AutoMapper 13.x**: 实体与DTO之间的自动映射
- **异步编程**: 全异步方法(async/await)，提升性能

##  快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Auth/LYBT.Module.Auth.csproj
```

**集成说明**：

### 1. 注册认证模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册认证模块(自动注册AuthService+JwtService)
        services.AddAuthModule(configuration);

        // 配置JWT认证
        var jwtSettings = configuration.GetSection("Jwt");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
                };
            });
    }
}
```

### 2. JWT配置(appsettings.json)
```json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Desktop",
    "ExpirationMinutes": 120,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 3. 双轨认证实现(超级管理员 + 普通用户)
```csharp
// AuthService中的双轨认证逻辑
public class AuthService : IAuthService
{
    // 第一轨：超级管理员凭证验证(AdminSecrets表物理隔离)
    public bool IsSuperAdminCredentials(string username, string password)
    {
        // 从配置读取超级管理员用户名列表
        var adminUsernames = _configuration
            .GetSection("Authentication:SuperAdmin:Usernames")
            .Get<List<string>>() ?? new List<string>();

        // 用户名不在超级管理员列表中
        if (!adminUsernames.Contains(username, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // 从AdminSecrets表验证密码
        var adminSecret = _dbContext.AdminSecrets
            .FirstOrDefault(a => a.Username.ToLower() == username.ToLower());

        if (adminSecret == null) return false;

        // BCrypt密码验证
        return BCrypt.Net.BCrypt.Verify(password, adminSecret.PasswordHash);
    }

    // 第二轨：普通用户凭证验证(Users表标准认证)
    public async Task<User?> VerifyCredentialsAsync(string username, string password)
    {
        // 检查是否为超级管理员用户名(禁止普通用户使用)
        if (IsSuperAdminUsername(username))
        {
            _logger.LogWarning($"尝试使用保留用户名登录: {username}");
            return null;
        }

        // 从Users表查询用户
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return null;

        // 检查用户状态
        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning($"用户状态异常: {username}, 状态: {user.Status}");
            return null;
        }

        // BCrypt密码验证
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning($"密码验证失败: {username}");
            return null;
        }

        return user;
    }

    // 统一登录入口(自动判断双轨认证)
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 先尝试超级管理员认证(第一轨)
        if (IsSuperAdminCredentials(request.Username, request.Password))
        {
            // 超级管理员登录
            var adminToken = _jwtService.GenerateToken(
                userId: Guid.Empty,
                username: request.Username,
                role: UserRole.Admin,
                isSuperAdmin: true
            );

            return new LoginResponse
            {
                AccessToken = adminToken,
                RefreshToken = null,  // 超级管理员无RefreshToken
                UserId = Guid.Empty,
                Username = request.Username,
                Role = UserRole.Admin,
                IsSuperAdmin = true,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }

        // 再尝试普通用户认证(第二轨)
        var user = await VerifyCredentialsAsync(request.Username, request.Password);
        if (user == null)
        {
            throw new UnauthorizedException("用户名或密码错误");
        }

        // 生成JWT AccessToken
        var accessToken = _jwtService.GenerateToken(
            userId: user.Id,
            username: user.Username,
            role: user.Role,
            isSuperAdmin: false
        );

        // 生成RefreshToken
        var refreshToken = Guid.NewGuid().ToString("N");
        var session = new AuthSession
        {
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            DeviceInfo = request.DeviceInfo,
            IpAddress = request.IpAddress
        };
        await _dbContext.AuthSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            IsSuperAdmin = false,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
    }
}
```

### 4. RefreshToken机制(刷新、撤销、有效期管理)
```csharp
// 刷新访问令牌(RefreshToken机制核心)
public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
{
    // 查询RefreshToken会话
    var session = await _dbContext.AuthSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    if (session == null)
    {
        throw new UnauthorizedException("无效的RefreshToken");
    }

    // 检查是否过期
    if (session.ExpiresAt < DateTime.UtcNow)
    {
        throw new UnauthorizedException("RefreshToken已过期");
    }

    // 检查是否被撤销
    if (session.RevokedAt.HasValue)
    {
        throw new UnauthorizedException("RefreshToken已被撤销");
    }

    var user = session.User;

    // 生成新的AccessToken
    var newAccessToken = _jwtService.GenerateToken(
        userId: user.Id,
        username: user.Username,
        role: user.Role,
        isSuperAdmin: false
    );

    // 更新会话最后使用时间
    session.LastUsedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    return new LoginResponse
    {
        AccessToken = newAccessToken,
        RefreshToken = refreshToken,  // RefreshToken不变
        UserId = user.Id,
        Username = user.Username,
        Role = user.Role,
        IsSuperAdmin = false,
        ExpiresAt = DateTime.UtcNow.AddHours(2)
    };
}

// 撤销RefreshToken(登出或安全事件)
public async Task RevokeTokenAsync(string refreshToken)
{
    var session = await _dbContext.AuthSessions
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    if (session == null) return;

    // 标记为已撤销
    session.RevokedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    _logger.LogInformation($"RefreshToken已撤销: {refreshToken}");
}

// 用户登出(撤销所有会话)
public async Task LogoutAsync(Guid userId)
{
    var sessions = await _dbContext.AuthSessions
        .Where(s => s.UserId == userId && !s.RevokedAt.HasValue)
        .ToListAsync();

    foreach (var session in sessions)
    {
        session.RevokedAt = DateTime.UtcNow;
    }

    await _dbContext.SaveChangesAsync();
    _logger.LogInformation($"用户{userId}的所有会话已撤销");
}
```

### 5. 会话管理功能(设备信息、IP地址、过期时间)
```csharp
// 获取当前会话信息
public async Task<SessionInfoDto> GetSessionInfoAsync(Guid userId)
{
    var sessions = await _dbContext.AuthSessions
        .Where(s => s.UserId == userId && !s.RevokedAt.HasValue)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync();

    var activeSessions = sessions
        .Where(s => s.ExpiresAt > DateTime.UtcNow)
        .Select(s => new SessionDetailDto
        {
            SessionId = s.Id,
            DeviceInfo = s.DeviceInfo,
            IpAddress = s.IpAddress,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            LastUsedAt = s.LastUsedAt
        })
        .ToList();

    return new SessionInfoDto
    {
        UserId = userId,
        TotalSessions = sessions.Count,
        ActiveSessions = activeSessions.Count,
        Sessions = activeSessions
    };
}

// AuthSession实体定义
public class AuthSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? DeviceInfo { get; set; }  // 设备信息(如:"Windows 11, Chrome 120")
    public string? IpAddress { get; set; }   // 登录IP地址

    // 导航属性
    public virtual User User { get; set; } = null!;
}
```

### 6. JWT密钥强度验证(安全性检查)
```csharp
// JwtService中的密钥强度验证
public class JwtService : IJwtService
{
    public void ValidateSecretKeyStrength(string secretKey)
    {
        // 最小长度检查(至少32字符，256位)
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SecretKey必须至少32个字符（256位）以确保安全性"
            );
        }

        // 复杂度检查(可选，生产环境建议启用)
        var hasUpperCase = secretKey.Any(char.IsUpper);
        var hasLowerCase = secretKey.Any(char.IsLower);
        var hasDigit = secretKey.Any(char.IsDigit);
        var hasSpecialChar = secretKey.Any(c => !char.IsLetterOrDigit(c));

        if (!(hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar))
        {
            _logger.LogWarning(
                "JWT SecretKey复杂度不足，建议包含大小写字母、数字和特殊字符"
            );
        }

        _logger.LogInformation("JWT SecretKey强度验证通过");
    }

    // 生成JWT Token
    public string GenerateToken(
        Guid userId,
        string username,
        UserRole role,
        bool isSuperAdmin = false)
    {
        // 验证密钥强度
        var secretKey = _configuration["Jwt:SecretKey"];
        ValidateSecretKeyStrength(secretKey);

        // 生成Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("IsSuperAdmin", isSuperAdmin.ToString())
        };

        // 生成签名密钥
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 生成Token
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 7. 超级管理员密码修改(AdminSecrets表独立管理)
```csharp
// 超级管理员密码修改
public async Task ChangeSysAdminPasswordAsync(
    string username,
    string oldPassword,
    string newPassword)
{
    // 验证旧密码
    if (!IsSuperAdminCredentials(username, oldPassword))
    {
        throw new UnauthorizedException("旧密码验证失败");
    }

    // 从AdminSecrets表查询
    var adminSecret = await _dbContext.AdminSecrets
        .FirstOrDefaultAsync(a => a.Username.ToLower() == username.ToLower());

    if (adminSecret == null)
    {
        throw new NotFoundException("超级管理员记录不存在");
    }

    // 生成新密码哈希
    var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

    // 更新密码
    adminSecret.PasswordHash = newPasswordHash;
    adminSecret.UpdatedAt = DateTime.UtcNow;

    await _dbContext.SaveChangesAsync();

    _logger.LogInformation($"超级管理员{username}密码已修改");
}

// AdminSecret实体定义(物理隔离)
public class AdminSecret : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime? LastPasswordChangedAt { get; set; }
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `AuthController` 对外暴露。

- **API路由前缀**: `/api/v1/auth`

**主要端点**:
- `POST /api/v1/auth/login` - 用户登录（双轨认证自动判断）
- `POST /api/v1/auth/admin/login` - 超级管理员专用登录端点（隐藏端点，不在Swagger暴露）
- `POST /api/v1/auth/logout` - 用户登出（撤销所有会话）
- `POST /api/v1/auth/refresh-token` - 刷新访问令牌（RefreshToken机制）
- `POST /api/v1/auth/revoke-token` - 撤销RefreshToken
- `POST /api/v1/auth/validate-token` - 验证JWT令牌有效性
- `GET /api/v1/auth/session-info` - 获取当前用户会话信息
- `PUT /api/v1/auth/admin/change-password` - 超级管理员密码修改
- `POST /api/v1/auth/change-password` - 普通用户密码修改
- `GET /api/v1/auth/verify` - 验证当前JWT令牌（用于前端心跳检查）

**完整API定义**请参考 `IAuthService` 接口和 `AuthController` 的实现。

## 🔒 安全特性

### 双轨认证架构
- **超级管理员隔离**：AdminSecrets表物理隔离，用户名通过配置驱动（appsettings.json）
- **普通用户认证**：Users表标准认证流程
- **保留用户名列表**：防止普通用户注册冲突超级管理员用户名（如：sysadmin, root）

### 认证机制
- **JWT认证**：AccessToken有效期2小时，无状态验证，支持Issuer/Audience/Expiration验证
- **RefreshToken**：有效期7天，支持撤销，存储在AuthSessions表，记录设备信息和IP地址
- **密码加密**：BCrypt哈希算法（工作因子12），每次哈希结果不同（salt随机生成）

### 会话管理
- **多设备支持**：AuthSessions表记录设备信息、IP地址、登录时间、过期时间
- **会话撤销**：支持单会话撤销（RevokeTokenAsync）和全部会话撤销（LogoutAsync）
- **过期清理**：定期清理过期会话（建议通过后台任务实现）

### 隐藏端点
- `/api/v1/auth/admin/login`：超级管理员专用端点（不在Swagger中暴露，防止暴露攻击面）

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/auth/](../../../../docs/reference/modules/auth/) *(待创建)*
- **架构设计**: [docs/explanation/architecture/server/auth-design.md](../../../../docs/explanation/architecture/server/auth-design.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/server/auth-integration.md](../../../../docs/how-to-guides/server/auth-integration.md) *(待创建)*
- **API参考**: [docs/reference/api/auth-api.md](../../../../docs/reference/api/auth-api.md)
- **业务规则**: [docs/explanation/business-rules.md](../../../../docs/explanation/business-rules.md) - 参见"认证与授权规则"章节

---

**最后更新**：2025-10-29
**维护负责**：Server端开发组

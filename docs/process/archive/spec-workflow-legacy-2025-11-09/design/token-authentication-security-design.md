# Token认证安全重构 - 技术设计文档

**文档状态**: ✅ 设计完成
**创建日期**: 2025-11-06
**关联需求**: token-authentication-security-requirements.md
**架构方案**: 方案C - 当前设计简化版

---

## 一、架构设计

### 1.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      WPF Desktop Client                      │
├──────────────────────────┬──────────────────────────────────┤
│   LoginViewModel         │   AuthenticationService          │
│   (MVVM Pattern)         │   (Foundation Layer)             │
│                          │                                  │
│   用户交互                │   ┌──────────────────────────┐  │
│   ├─ 输入用户名密码       │   │ LocalTokenValidator      │  │
│   ├─ 显示登录状态        │   │ (JWT自验证)               │  │
│   └─ 处理重新登录        │   └──────────────────────────┘  │
│                          │                                  │
│                          │   ┌──────────────────────────┐  │
│                          │   │ SecureTokenStorage       │  │
│                          │   │ (DPAPI加密存储)           │  │
│                          │   └──────────────────────────┘  │
└──────────────────────────┴──────────────────────────────────┘
                               │
                               │ HTTPS (Refit)
                               ↓
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Web API                      │
├──────────────────────────┬──────────────────────────────────┤
│   AuthController         │   AuthService                    │
│   (API Layer)            │   (Business Layer)               │
│                          │                                  │
│   POST /api/v1/auth/     │   ┌──────────────────────────┐  │
│   ├─ login               │   │ TokenRevocationService   │  │
│   ├─ logout              │   │ (撤销管理)                │  │
│   ├─ refresh             │   └──────────────────────────┘  │
│   ├─ revoke-token        │                                  │
│   └─ revoke-all-tokens   │   ┌──────────────────────────┐  │
│                          │   │ SecurityAuditService     │  │
│                          │   │ (审计日志)                │  │
│                          │   └──────────────────────────┘  │
│                          │                                  │
│                          │   ┌──────────────────────────┐  │
│                          │   │ JwtService               │  │
│                          │   │ (Token生成/验证)          │  │
│                          │   └──────────────────────────┘  │
└──────────────────────────┴──────────────────────────────────┘
                               │
                               ↓
┌─────────────────────────────────────────────────────────────┐
│                      SQL Server 2022                         │
├──────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐│
│  │  AdminSecrets   │  │  Users          │  │RefreshTokens ││
│  │  (Auth模块)      │  │  (User模块)     │  │(新增字段)    ││
│  └─────────────────┘  └─────────────────┘  └──────────────┘│
│                          ┌──────────────────────────────┐   │
│                          │  SecurityAuditLogs (新增表)   │   │
│                          └──────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 认证流程对比

#### 当前流程（重构前）
```
Client启动
    ↓
加载本地Token（明文）
    ↓
调用Server API验证 (/api/v1/auth/validate POST)
    ↓
Server验证签名、过期、Claims
    ↓
返回ValidateTokenResponse (Username可能为null)
    ↓
Client判断有效性 → 无效则清除Token
```

**问题**：
- ❌ Token明文存储
- ❌ 网络往返降低性能
- ❌ Server不检查撤销状态
- ❌ Username为null导致认证失败

#### 重构后流程
```
Client启动
    ↓
清除所有旧Token（迁移策略）
    ↓
用户登录
    ↓
Server验证凭据 → 生成Token
    ↓
Client DPAPI加密存储
    ↓
───────────────────────────────
下次启动
    ↓
读取加密Token
    ↓
Client本地JWT验证（签名、过期、Claims）
    ↓
验证成功 → 自动登录
验证失败 → 要求重新登录
```

**改进**：
- ✅ Token加密存储（DPAPI）
- ✅ 本地验证无网络往返
- ✅ RefreshToken时Server检查撤销状态
- ✅ Claims完整提取

---

## 二、Client端设计

### 2.1 LocalTokenValidator

#### 类设计
```csharp
namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// Client端JWT Token本地验证器
/// </summary>
public class LocalTokenValidator
{
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalTokenValidator> _logger;

    public LocalTokenValidator(
        IConfiguration configuration,
        ILogger<LocalTokenValidator> logger)
    {
        _tokenHandler = new JwtSecurityTokenHandler();
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 本地验证JWT Token
    /// </summary>
    /// <param name="token">要验证的Token</param>
    /// <returns>验证结果包含用户信息</returns>
    public TokenValidationResult ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return TokenValidationResult.Failed("Token不能为空");
            }

            // 构造验证参数
            var validationParameters = BuildValidationParameters();

            // 验证Token
            var principal = _tokenHandler.ValidateToken(
                token,
                validationParameters,
                out var validatedToken);

            // 提取Claims
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = principal.FindFirst(ClaimTypes.Name)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var userType = principal.FindFirst("user_type")?.Value;

            // 验证必需Claims
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Token缺少必需的Claims: UserId或UserName");
                return TokenValidationResult.Failed("Token Claims不完整");
            }

            // 提取过期时间
            var jwtToken = validatedToken as JwtSecurityToken;
            var expiresAt = jwtToken?.ValidTo ?? DateTime.MinValue;

            _logger.LogInformation("Token验证成功: {UserName}, 过期时间: {ExpiresAt}",
                userName, expiresAt);

            return TokenValidationResult.Success(new UserSession
            {
                UserId = Guid.Parse(userId),
                UserName = userName,
                Role = Enum.Parse<UserRole>(role),
                UserType = userType ?? "user",
                AccessToken = token,
                AccessTokenExpiry = expiresAt
            });
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogInformation("Token已过期: {Message}", ex.Message);
            return TokenValidationResult.Failed("Token已过期");
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("Token签名无效: {Message}", ex.Message);
            return TokenValidationResult.Failed("Token签名无效");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token验证异常");
            return TokenValidationResult.Failed($"Token验证失败: {ex.Message}");
        }
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        var secretKey = _configuration["Lybt:Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey配置未找到");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _configuration["Lybt:Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Lybt:Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // 5分钟时钟偏差容忍
        };
    }
}

/// <summary>
/// Token验证结果
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public UserSession? Session { get; init; }

    public static TokenValidationResult Success(UserSession session)
        => new() { IsValid = true, Session = session };

    public static TokenValidationResult Failed(string errorMessage)
        => new() { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// 用户会话信息
/// </summary>
public class UserSession
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string UserType { get; init; } = "user";
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public DateTime RefreshTokenExpiry { get; init; }
}
```

#### 配置示例（appsettings.json）
```json
{
  "Lybt": {
    "Jwt": {
      "SecretKey": "YourSecretKeyMustBeAtLeast32CharactersLong!",
      "Issuer": "LYBTZYZS-Server",
      "Audience": "LYBTZYZS-Desktop"
    }
  }
}
```

---

### 2.2 SecureTokenStorage

#### 类设计
```csharp
namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 使用Windows DPAPI的安全Token存储
/// </summary>
public class SecureTokenStorage : ITokenStorage
{
    private readonly ILogger<SecureTokenStorage> _logger;
    private readonly string _tokenFilePath;
    private static readonly byte[]? _entropy = null; // 可选：额外的熵值

    public SecureTokenStorage(ILogger<SecureTokenStorage> logger)
    {
        _logger = logger;

        // Token文件路径：%LOCALAPPDATA%\LYBTZYZS\tokens.dat
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, "LYBTZYZS");
        Directory.CreateDirectory(appFolder);
        _tokenFilePath = Path.Combine(appFolder, "tokens.dat");
    }

    /// <summary>
    /// 保存Token（DPAPI加密）
    /// </summary>
    public async Task SaveTokenAsync(UserSession session)
    {
        try
        {
            // 序列化为JSON
            var json = JsonSerializer.Serialize(session);
            var plainBytes = Encoding.UTF8.GetBytes(json);

            // 使用DPAPI加密
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                _entropy,
                DataProtectionScope.CurrentUser);

            // 写入文件
            await File.WriteAllBytesAsync(_tokenFilePath, encryptedBytes);

            _logger.LogInformation("Token已加密保存: {FilePath}", _tokenFilePath);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "DPAPI加密失败，降级为明文存储");

            // 降级策略：明文存储 + 警告
            var json = JsonSerializer.Serialize(session);
            await File.WriteAllTextAsync(_tokenFilePath, json);

            _logger.LogWarning("警告: Token使用明文存储。建议检查系统DPAPI配置。");
        }
    }

    /// <summary>
    /// 加载Token（DPAPI解密）
    /// </summary>
    public async Task<UserSession?> LoadTokenAsync()
    {
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                _logger.LogInformation("Token文件不存在");
                return null;
            }

            var encryptedBytes = await File.ReadAllBytesAsync(_tokenFilePath);

            // 尝试DPAPI解密
            try
            {
                var plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    _entropy,
                    DataProtectionScope.CurrentUser);

                var json = Encoding.UTF8.GetString(plainBytes);
                var session = JsonSerializer.Deserialize<UserSession>(json);

                _logger.LogInformation("Token已从加密文件加载");
                return session;
            }
            catch (CryptographicException)
            {
                // 可能是明文存储的旧Token，尝试直接读取
                var json = await File.ReadAllTextAsync(_tokenFilePath);
                var session = JsonSerializer.Deserialize<UserSession>(json);

                _logger.LogWarning("加载的Token为明文格式");
                return session;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载Token失败");
            return null;
        }
    }

    /// <summary>
    /// 清除Token
    /// </summary>
    public Task ClearTokenAsync()
    {
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
                _logger.LogInformation("Token已清除");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除Token失败");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Token存储接口
/// </summary>
public interface ITokenStorage
{
    Task SaveTokenAsync(UserSession session);
    Task<UserSession?> LoadTokenAsync();
    Task ClearTokenAsync();
}
```

---

### 2.3 AuthenticationService重构

#### 修改清单
```csharp
namespace LYBT.Desktop.Foundation.Security;

public class AuthenticationService : IAuthenticationService
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStorage _tokenStorage;
    private readonly LocalTokenValidator _tokenValidator; // 新增
    private readonly ILogger<AuthenticationService> _logger;

    // 移除：不再调用Server的ValidateTokenAsync API

    /// <summary>
    /// 验证并恢复会话（应用启动时调用）
    /// </summary>
    public async Task<ServiceResult<UserSession>> ValidateAndRestoreSessionAsync()
    {
        try
        {
            // 1. 加载本地Token
            var session = await _tokenStorage.LoadTokenAsync();

            if (session == null)
            {
                _logger.LogInformation("本地无Token，需要登录");
                return ServiceResult<UserSession>.Failure("未登录");
            }

            // 2. 本地验证AccessToken
            var validationResult = _tokenValidator.ValidateToken(session.AccessToken);

            if (validationResult.IsValid)
            {
                _logger.LogInformation("Token验证成功，会话已恢复: {UserName}",
                    validationResult.Session!.UserName);
                return ServiceResult<UserSession>.Success(validationResult.Session);
            }

            // 3. AccessToken过期，尝试RefreshToken
            if (session.RefreshTokenExpiry > DateTime.UtcNow)
            {
                _logger.LogInformation("AccessToken过期，尝试刷新");
                var refreshResult = await RefreshTokenAsync(session.RefreshToken);

                if (refreshResult.IsSuccess)
                {
                    return ServiceResult<UserSession>.Success(refreshResult.Data!);
                }
            }

            // 4. RefreshToken也过期或刷新失败，清除Token
            _logger.LogWarning("Token无效或已过期，清除会话");
            await _tokenStorage.ClearTokenAsync();
            return ServiceResult<UserSession>.Failure("Token无效或已过期");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话恢复异常");
            return ServiceResult<UserSession>.Failure($"会话恢复失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<ServiceResult<UserSession>> LoginAsync(LoginRequest request)
    {
        try
        {
            // 调用Server API登录
            var apiResponse = await _authApi.LoginAsync(request);

            if (!apiResponse.Success || apiResponse.Data == null)
            {
                _logger.LogWarning("登录失败: {Message}", apiResponse.Message);
                return ServiceResult<UserSession>.Failure(apiResponse.Message ?? "登录失败");
            }

            // 验证返回的Token
            var validationResult = _tokenValidator.ValidateToken(apiResponse.Data.Token);

            if (!validationResult.IsValid)
            {
                _logger.LogError("Server返回的Token无效: {Error}", validationResult.ErrorMessage);
                return ServiceResult<UserSession>.Failure("Token验证失败");
            }

            // 填充完整会话信息
            var session = validationResult.Session! with
            {
                RefreshToken = apiResponse.Data.RefreshToken,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7) // 从配置读取
            };

            // 加密保存Token
            await _tokenStorage.SaveTokenAsync(session);

            _logger.LogInformation("登录成功: {UserName}", session.UserName);
            return ServiceResult<UserSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录异常");
            return ServiceResult<UserSession>.Failure($"登录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 刷新Token
    /// </summary>
    private async Task<ServiceResult<UserSession>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            var apiResponse = await _authApi.RefreshTokenAsync(request);

            if (!apiResponse.Success || apiResponse.Data == null)
            {
                _logger.LogWarning("RefreshToken失败: {Message}", apiResponse.Message);
                return ServiceResult<UserSession>.Failure(apiResponse.Message ?? "刷新失败");
            }

            // 验证新Token
            var validationResult = _tokenValidator.ValidateToken(apiResponse.Data.Token);

            if (!validationResult.IsValid)
            {
                return ServiceResult<UserSession>.Failure("新Token验证失败");
            }

            // 更新会话
            var session = validationResult.Session! with
            {
                RefreshToken = apiResponse.Data.RefreshToken,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };

            await _tokenStorage.SaveTokenAsync(session);

            _logger.LogInformation("Token刷新成功");
            return ServiceResult<UserSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token刷新异常");
            return ServiceResult<UserSession>.Failure($"刷新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public async Task<ServiceResult> LogoutAsync()
    {
        try
        {
            // 调用Server API登出
            await _authApi.LogoutAsync(new LogoutRequest());

            // 清除本地Token
            await _tokenStorage.ClearTokenAsync();

            _logger.LogInformation("登出成功");
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出异常");
            // 即使Server调用失败，也清除本地Token
            await _tokenStorage.ClearTokenAsync();
            return ServiceResult.Failure($"登出失败: {ex.Message}");
        }
    }
}
```

---

### 2.4 应用启动流程

#### App.xaml.cs修改
```csharp
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. 清除所有旧Token（Token迁移策略）
        await ClearLegacyTokensAsync();

        // 2. 初始化依赖注入
        ConfigureServices();

        // 3. 尝试恢复会话
        var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();
        var sessionResult = await authService.ValidateAndRestoreSessionAsync();

        // 4. 根据会话状态导航
        if (sessionResult.IsSuccess)
        {
            // 自动登录成功
            NavigateToMainWindow(sessionResult.Data!);
        }
        else
        {
            // 需要登录
            NavigateToLoginWindow();
        }
    }

    private async Task ClearLegacyTokensAsync()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "LYBTZYZS");
            var tokenFile = Path.Combine(appFolder, "tokens.dat");

            if (File.Exists(tokenFile))
            {
                File.Delete(tokenFile);
                Logger.LogInformation("已清除旧Token文件（系统安全升级）");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "清除旧Token失败");
        }
    }
}
```

---

## 三、Server端设计

### 3.1 数据库Schema

#### RefreshTokens表修改
```sql
-- 新增字段
ALTER TABLE RefreshTokens ADD IsRevoked BIT NOT NULL DEFAULT 0;
ALTER TABLE RefreshTokens ADD RevokedAt DATETIME2 NULL;
ALTER TABLE RefreshTokens ADD RevokeReason NVARCHAR(500) NULL;

-- 索引优化
CREATE INDEX IX_RefreshTokens_IsRevoked_Token
ON RefreshTokens(IsRevoked, Token)
INCLUDE (UserId, UserType, ExpiresAt);
```

#### SecurityAuditLogs表创建
```sql
CREATE TABLE SecurityAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EventType NVARCHAR(50) NOT NULL,           -- Login, Logout, RefreshToken, TokenRevoked, LoginFailed
    UserId UNIQUEIDENTIFIER NULL,
    UserType NVARCHAR(50) NULL,                -- superadmin, user
    UserName NVARCHAR(256) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(500) NULL,
    Metadata NVARCHAR(MAX) NULL,               -- JSON: 额外信息
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IX_SecurityAuditLogs_EventType_CreatedAt (EventType, CreatedAt DESC),
    INDEX IX_SecurityAuditLogs_UserId_CreatedAt (UserId, CreatedAt DESC),
    INDEX IX_SecurityAuditLogs_CreatedAt (CreatedAt DESC)
);
```

#### EF Core实体
```csharp
namespace LYBT.Entities.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserType { get; set; } = "user"; // Issue #1861
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? FamilyId { get; set; }

    // 新增字段
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
}

public class SecurityAuditLog
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserType { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### 3.2 TokenRevocationService

```csharp
namespace LYBT.Module.Auth.Services;

/// <summary>
/// Token撤销服务
/// </summary>
public class TokenRevocationService : ITokenRevocationService
{
    private readonly AppDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<TokenRevocationService> _logger;

    public TokenRevocationService(
        AppDbContext dbContext,
        ISecurityAuditService auditService,
        ILogger<TokenRevocationService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 撤销单个RefreshToken
    /// </summary>
    public async Task<ServiceResult> RevokeTokenAsync(
        string refreshToken,
        string? reason = null)
    {
        try
        {
            var tokenRecord = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (tokenRecord == null)
            {
                return ServiceResult.Failure("RefreshToken不存在");
            }

            if (tokenRecord.IsRevoked)
            {
                _logger.LogWarning("Token已被撤销: {Token}", refreshToken);
                return ServiceResult.Success("Token已被撤销");
            }

            // 标记为已撤销
            tokenRecord.IsRevoked = true;
            tokenRecord.RevokedAt = DateTime.UtcNow;
            tokenRecord.RevokeReason = reason ?? "手动撤销";

            await _dbContext.SaveChangesAsync();

            // 记录审计日志
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "TokenRevoked",
                UserId = tokenRecord.UserId,
                UserType = tokenRecord.UserType,
                Success = true,
                Metadata = new { RefreshToken = refreshToken, Reason = reason }
            });

            _logger.LogInformation("Token已撤销: UserId={UserId}, Reason={Reason}",
                tokenRecord.UserId, reason);

            return ServiceResult.Success("Token已撤销");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销Token失败");
            return ServiceResult.Failure($"撤销失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 撤销用户的所有RefreshToken（强制下线）
    /// </summary>
    public async Task<ServiceResult> RevokeAllUserTokensAsync(
        Guid userId,
        string? reason = null)
    {
        try
        {
            var tokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            if (!tokens.Any())
            {
                _logger.LogWarning("用户无有效Token: {UserId}", userId);
                return ServiceResult.Success("用户无有效Token");
            }

            // 批量撤销
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokeReason = reason ?? "批量撤销（强制下线）";
            }

            await _dbContext.SaveChangesAsync();

            // 记录审计日志
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "AllTokensRevoked",
                UserId = userId,
                Success = true,
                Metadata = new { TokenCount = tokens.Count, Reason = reason }
            });

            _logger.LogInformation("用户所有Token已撤销: UserId={UserId}, Count={Count}",
                userId, tokens.Count);

            return ServiceResult.Success($"已撤销{tokens.Count}个Token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量撤销Token失败: UserId={UserId}", userId);
            return ServiceResult.Failure($"撤销失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查RefreshToken是否已撤销
    /// </summary>
    public async Task<bool> IsRevokedAsync(string refreshToken)
    {
        var tokenRecord = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        return tokenRecord?.IsRevoked ?? false;
    }
}

public interface ITokenRevocationService
{
    Task<ServiceResult> RevokeTokenAsync(string refreshToken, string? reason = null);
    Task<ServiceResult> RevokeAllUserTokensAsync(Guid userId, string? reason = null);
    Task<bool> IsRevokedAsync(string refreshToken);
}
```

---

### 3.3 SecurityAuditService

```csharp
namespace LYBT.Module.Auth.Services;

/// <summary>
/// 安全审计日志服务
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityAuditService(
        AppDbContext dbContext,
        ILogger<SecurityAuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 记录安全审计事件（异步）
    /// </summary>
    public async Task LogAsync(SecurityAuditEvent @event)
    {
        try
        {
            // 提取HTTP上下文信息
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

            var log = new SecurityAuditLog
            {
                EventType = @event.EventType,
                UserId = @event.UserId,
                UserType = @event.UserType,
                UserName = @event.UserName,
                IpAddress = MaskIpAddress(ipAddress), // 脱敏
                UserAgent = TruncateUserAgent(userAgent), // 截断
                Success = @event.Success,
                ErrorMessage = @event.ErrorMessage,
                Metadata = @event.Metadata != null
                    ? JsonSerializer.Serialize(@event.Metadata)
                    : null
            };

            _dbContext.SecurityAuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("安全审计日志已记录: {EventType}, {UserName}",
                @event.EventType, @event.UserName);
        }
        catch (Exception ex)
        {
            // 审计日志失败不应阻塞业务流程
            _logger.LogError(ex, "记录安全审计日志失败: {EventType}", @event.EventType);
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress(HttpContext? context)
    {
        if (context == null) return null;

        // 支持反向代理
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// 脱敏IP地址（保留前3段）
    /// </summary>
    private string? MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return null;

        var parts = ipAddress.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
        }

        return ipAddress; // IPv6或其他格式不处理
    }

    /// <summary>
    /// 截断UserAgent（最多100字符）
    /// </summary>
    private string? TruncateUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;

        return userAgent.Length > 100
            ? userAgent.Substring(0, 100) + "..."
            : userAgent;
    }

    /// <summary>
    /// 清理过期日志（30天前）
    /// </summary>
    public async Task CleanupOldLogsAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            var oldLogs = await _dbContext.SecurityAuditLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _dbContext.SecurityAuditLogs.RemoveRange(oldLogs);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("已清理{Count}条过期审计日志", oldLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理审计日志失败");
        }
    }
}

/// <summary>
/// 安全审计事件
/// </summary>
public class SecurityAuditEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserType { get; set; }
    public string? UserName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Metadata { get; set; }
}

public interface ISecurityAuditService
{
    Task LogAsync(SecurityAuditEvent @event);
    Task CleanupOldLogsAsync();
}
```

---

### 3.4 AuthService修改

#### RefreshTokenAsync集成撤销检查
```csharp
public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(
    string refreshToken,
    CancellationToken cancellationToken = default)
{
    try
    {
        // 1. 查询RefreshToken记录
        var tokenRecord = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (tokenRecord == null)
        {
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "RefreshTokenRejected",
                Success = false,
                ErrorMessage = "RefreshToken不存在"
            });
            return ServiceResult<LoginResponse>.Failure("RefreshToken无效");
        }

        // 2. 检查撤销状态（新增）
        if (tokenRecord.IsRevoked)
        {
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "RefreshTokenRejected",
                UserId = tokenRecord.UserId,
                UserType = tokenRecord.UserType,
                Success = false,
                ErrorMessage = $"RefreshToken已撤销: {tokenRecord.RevokeReason}"
            });

            _logger.LogWarning("尝试使用已撤销的RefreshToken: UserId={UserId}",
                tokenRecord.UserId);
            return ServiceResult<LoginResponse>.Failure("RefreshToken已撤销");
        }

        // 3. 检查过期
        if (tokenRecord.ExpiresAt < DateTime.UtcNow)
        {
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "RefreshTokenRejected",
                UserId = tokenRecord.UserId,
                UserType = tokenRecord.UserType,
                Success = false,
                ErrorMessage = "RefreshToken已过期"
            });
            return ServiceResult<LoginResponse>.Failure("RefreshToken已过期");
        }

        // 4. 根据UserType路由获取用户信息
        UserDto userDto;
        string userType;

        if (tokenRecord.UserType == "superadmin")
        {
            // SuperAdmin路由
            userDto = new UserDto
            {
                Id = tokenRecord.UserId,
                UserName = _configuration["Lybt:SystemAdmin:Username"] ?? "admin",
                RealName = "系统超级管理员",
                Role = UserRole.Admin,
                Email = _configuration["Lybt:SystemAdmin:Email"] ?? "admin@lybt.com"
            };
            userType = "superadmin";
        }
        else
        {
            // User路由
            var userEntity = await _userRepository.GetByIdAsync(tokenRecord.UserId);
            if (userEntity == null)
            {
                return ServiceResult<LoginResponse>.Failure("用户不存在");
            }
            userDto = _mapper.Map<UserDto>(userEntity);
            userType = "user";
        }

        // 5. 撤销旧Token（Token轮换）
        tokenRecord.IsRevoked = true;
        tokenRecord.RevokedAt = DateTime.UtcNow;
        tokenRecord.RevokeReason = "Token轮换（自动撤销）";

        // 6. 生成新Token（统一策略）
        var newAccessToken = _jwtService.GenerateToken(
            userDto.Id.ToString(),
            userDto.UserName,
            userDto.Role,
            new Dictionary<string, string>(),
            userType);

        var newRefreshToken = GenerateRefreshToken();
        var refreshTokenExpireDays = _configuration.GetValue<int?>("Lybt:Jwt:RefreshTokenExpirationDays") ?? 7;

        // 7. 存储新RefreshToken
        var newTokenRecord = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = userDto.Id,
            UserType = userType,
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
            FamilyId = tokenRecord.FamilyId // 保持同一家族
        };
        _dbContext.RefreshTokens.Add(newTokenRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 8. 记录审计日志
        await _auditService.LogAsync(new SecurityAuditEvent
        {
            EventType = "RefreshToken",
            UserId = userDto.Id,
            UserType = userType,
            UserName = userDto.UserName,
            Success = true
        });

        _logger.LogInformation("Token刷新成功: UserId={UserId}, UserType={UserType}",
            userDto.Id, userType);

        return ServiceResult<LoginResponse>.Success(new LoginResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            User = userDto
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "RefreshToken异常");
        return ServiceResult<LoginResponse>.Failure($"刷新失败: {ex.Message}");
    }
}
```

#### LoginAsync集成审计日志
```csharp
public async Task<ServiceResult<LoginResponse>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
{
    try
    {
        // 1. 验证凭据
        var credentialsResult = await VerifyCredentialsAsync(request, cancellationToken);

        if (!credentialsResult.IsSuccess)
        {
            // 记录登录失败
            await _auditService.LogAsync(new SecurityAuditEvent
            {
                EventType = "LoginFailed",
                UserName = request.UserName,
                Success = false,
                ErrorMessage = credentialsResult.Message
            });

            return ServiceResult<LoginResponse>.Failure(credentialsResult.Message ?? "凭据验证失败");
        }

        // 2. 识别用户类型并生成Token
        // ... (与Issue #1861的实现一致)

        // 3. 记录登录成功
        await _auditService.LogAsync(new SecurityAuditEvent
        {
            EventType = "Login",
            UserId = userDto.Id,
            UserType = userType,
            UserName = userDto.UserName,
            Success = true
        });

        return ServiceResult<LoginResponse>.Success(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "登录异常");
        return ServiceResult<LoginResponse>.Failure($"登录失败: {ex.Message}");
    }
}
```

---

### 3.5 API端点设计

#### AuthController新增端点
```csharp
namespace LYBT.Server.Services.LYBT.WebAPI.Controllers;

[Route("api/v1/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenRevocationService _revocationService;

    // 移除: POST /api/v1/auth/validate
    // 原因: Client端本地验证，无需Server API

    /// <summary>
    /// 撤销RefreshToken
    /// </summary>
    /// <param name="request">撤销请求</param>
    [HttpPost("revoke-token")]
    [Authorize] // 需要认证
    public async Task<ActionResult<ApiResponse>> RevokeTokenAsync(
        [FromBody] RevokeTokenRequest request)
    {
        var result = await _revocationService.RevokeTokenAsync(
            request.RefreshToken,
            request.Reason);

        return HandleServiceResult(result);
    }

    /// <summary>
    /// 撤销用户所有Token（强制下线）
    /// </summary>
    /// <param name="request">撤销请求</param>
    [HttpPost("revoke-all-user-tokens")]
    [Authorize(Roles = "Admin")] // 仅管理员
    public async Task<ActionResult<ApiResponse>> RevokeAllUserTokensAsync(
        [FromBody] RevokeAllTokensRequest request)
    {
        var result = await _revocationService.RevokeAllUserTokensAsync(
            request.UserId,
            request.Reason);

        return HandleServiceResult(result);
    }
}

// DTOs
public class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class RevokeAllTokensRequest
{
    [Required]
    public Guid UserId { get; set; }

    public string? Reason { get; set; }
}
```

---

### 3.6 后台Job：清理审计日志

```csharp
namespace LYBT.Server.Services.LYBT.WebAPI.BackgroundServices;

/// <summary>
/// 定时清理过期审计日志（每日凌晨3点）
/// </summary>
public class SecurityAuditCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityAuditCleanupService> _logger;

    public SecurityAuditCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SecurityAuditCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("审计日志清理服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 计算下次执行时间（凌晨3点）
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(3);
                var delay = nextRun - now;

                _logger.LogInformation("下次审计日志清理时间: {NextRun}", nextRun);

                await Task.Delay(delay, stoppingToken);

                // 执行清理
                using var scope = _serviceProvider.CreateScope();
                var auditService = scope.ServiceProvider
                    .GetRequiredService<ISecurityAuditService>();

                await auditService.CleanupOldLogsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审计日志清理异常");
                // 等待1小时后重试
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

// 注册服务（Program.cs）
builder.Services.AddHostedService<SecurityAuditCleanupService>();
```

---

## 四、Phase拆分与实施计划

### Phase 1: Client端重构（Day 1-2）

#### Day 1: Token加密存储
- [ ] 实现`SecureTokenStorage`类
- [ ] 实现DPAPI加密/解密逻辑
- [ ] 实现降级策略（明文+警告）
- [ ] 单元测试：加密、解密、降级
- [ ] 集成到`App.xaml.cs`启动流程
- [ ] 实现Token清理逻辑（迁移策略）

#### Day 2: JWT自验证
- [ ] 实现`LocalTokenValidator`类
- [ ] 配置JWT验证参数（appsettings.json）
- [ ] 单元测试：验证成功、过期、签名无效
- [ ] 重构`AuthenticationService.ValidateAndRestoreSessionAsync`
- [ ] 移除对Server `POST /api/v1/auth/validate`的调用
- [ ] 集成测试：登录→加密存储→重启→本地验证

### Phase 2: Server端重构（Day 3-4）

#### Day 3: Token撤销机制
- [ ] 创建EF Core迁移（RefreshTokens新增字段）
- [ ] 实现`TokenRevocationService`类
- [ ] 实现`RevokeTokenAsync` API
- [ ] 实现`RevokeAllUserTokensAsync` API
- [ ] 修改`AuthService.RefreshTokenAsync`集成撤销检查
- [ ] 单元测试：撤销单个、撤销所有、刷新检查

#### Day 4: 安全审计日志
- [ ] 创建EF Core迁移（SecurityAuditLogs表）
- [ ] 实现`SecurityAuditService`类
- [ ] 集成到`LoginAsync`、`RefreshTokenAsync`、`LogoutAsync`
- [ ] 实现后台清理Job
- [ ] 单元测试：记录日志、脱敏、清理

### Phase 3: 集成测试与验收（Day 5-7）

#### Day 5: 功能集成测试
- [ ] 端到端测试：登录→存储→重启→恢复
- [ ] Token刷新测试：AccessToken过期→自动刷新
- [ ] 撤销测试：撤销Token→刷新失败
- [ ] 审计日志验证：所有事件正确记录

#### Day 6: 安全测试
- [ ] DPAPI加密验证（无法明文读取）
- [ ] Token签名验证（篡改检测）
- [ ] 撤销响应速度测试（< 1秒生效）
- [ ] 敏感信息脱敏检查

#### Day 7: 文档与部署
- [ ] 更新API文档（Swagger）
- [ ] 创建ADR文档
- [ ] 更新Issue #1861（标记替代）
- [ ] 准备发布说明（用户通知）

---

## 五、测试策略

### 5.1 单元测试

#### Client端测试
```csharp
[Fact]
public async Task SecureTokenStorage_Encrypt_Decrypt_Success()
{
    // Arrange
    var storage = new SecureTokenStorage(Mock.Of<ILogger<SecureTokenStorage>>());
    var session = new UserSession
    {
        UserId = Guid.NewGuid(),
        UserName = "testuser",
        AccessToken = "test.token.here",
        RefreshToken = "refresh.token.here"
    };

    // Act
    await storage.SaveTokenAsync(session);
    var loaded = await storage.LoadTokenAsync();

    // Assert
    loaded.Should().NotBeNull();
    loaded!.UserName.Should().Be("testuser");
    loaded.AccessToken.Should().Be("test.token.here");
}

[Fact]
public void LocalTokenValidator_ValidToken_ReturnsSuccess()
{
    // Arrange
    var config = CreateTestConfiguration();
    var validator = new LocalTokenValidator(config, Mock.Of<ILogger<LocalTokenValidator>>());
    var token = GenerateTestToken(); // 使用真实JwtSecurityTokenHandler生成

    // Act
    var result = validator.ValidateToken(token);

    // Assert
    result.IsValid.Should().BeTrue();
    result.Session.Should().NotBeNull();
    result.Session!.UserName.Should().Be("testuser");
}

[Fact]
public void LocalTokenValidator_ExpiredToken_ReturnsFailed()
{
    // Arrange
    var validator = new LocalTokenValidator(CreateTestConfiguration(), Mock.Of<ILogger<LocalTokenValidator>>());
    var expiredToken = GenerateExpiredToken();

    // Act
    var result = validator.ValidateToken(expiredToken);

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("已过期");
}
```

#### Server端测试
```csharp
[Fact]
public async Task TokenRevocationService_RevokeToken_Success()
{
    // Arrange
    var dbContext = CreateTestDbContext();
    var refreshToken = new RefreshToken
    {
        Token = "test.refresh.token",
        UserId = Guid.NewGuid(),
        UserType = "user",
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
    dbContext.RefreshTokens.Add(refreshToken);
    await dbContext.SaveChangesAsync();

    var service = new TokenRevocationService(
        dbContext,
        Mock.Of<ISecurityAuditService>(),
        Mock.Of<ILogger<TokenRevocationService>>());

    // Act
    var result = await service.RevokeTokenAsync("test.refresh.token", "测试撤销");

    // Assert
    result.IsSuccess.Should().BeTrue();
    var revokedToken = await dbContext.RefreshTokens.FindAsync(refreshToken.Id);
    revokedToken!.IsRevoked.Should().BeTrue();
    revokedToken.RevokeReason.Should().Be("测试撤销");
}

[Fact]
public async Task SecurityAuditService_LogAsync_Success()
{
    // Arrange
    var dbContext = CreateTestDbContext();
    var service = new SecurityAuditService(
        dbContext,
        Mock.Of<ILogger<SecurityAuditService>>(),
        Mock.Of<IHttpContextAccessor>());

    // Act
    await service.LogAsync(new SecurityAuditEvent
    {
        EventType = "Login",
        UserName = "testuser",
        Success = true
    });

    // Assert
    var logs = await dbContext.SecurityAuditLogs.ToListAsync();
    logs.Should().HaveCount(1);
    logs[0].EventType.Should().Be("Login");
    logs[0].UserName.Should().Be("testuser");
}
```

### 5.2 集成测试

#### 端到端认证流程
```csharp
[Fact]
public async Task EndToEnd_Login_Encrypt_Restart_Validate()
{
    // Arrange
    var client = CreateTestClient();
    var authService = client.Services.GetRequiredService<IAuthenticationService>();

    // Act 1: 登录
    var loginResult = await authService.LoginAsync(new LoginRequest
    {
        UserName = "testuser",
        Password = "Password123!"
    });

    // Assert 1: 登录成功
    loginResult.IsSuccess.Should().BeTrue();

    // Act 2: 模拟应用重启（重新创建AuthenticationService）
    var authService2 = CreateNewAuthenticationService();
    var restoreResult = await authService2.ValidateAndRestoreSessionAsync();

    // Assert 2: 会话恢复成功
    restoreResult.IsSuccess.Should().BeTrue();
    restoreResult.Data!.UserName.Should().Be("testuser");
}
```

### 5.3 性能测试

```csharp
[Fact]
public async Task Performance_LocalValidation_LessThan10ms()
{
    // Arrange
    var validator = new LocalTokenValidator(CreateTestConfiguration(), Mock.Of<ILogger<LocalTokenValidator>>());
    var token = GenerateTestToken();
    var stopwatch = Stopwatch.StartNew();

    // Act: 1000次验证
    for (int i = 0; i < 1000; i++)
    {
        var result = validator.ValidateToken(token);
    }
    stopwatch.Stop();

    // Assert: 平均 < 10ms
    var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;
    averageMs.Should().BeLessThan(10);
}
```

---

## 六、回滚方案

### 数据库回滚脚本
```sql
-- 回滚RefreshTokens表修改
ALTER TABLE RefreshTokens DROP COLUMN IF EXISTS IsRevoked;
ALTER TABLE RefreshTokens DROP COLUMN IF EXISTS RevokedAt;
ALTER TABLE RefreshTokens DROP COLUMN IF EXISTS RevokeReason;

-- 删除SecurityAuditLogs表
DROP TABLE IF EXISTS SecurityAuditLogs;

-- 删除索引
DROP INDEX IF EXISTS IX_RefreshTokens_IsRevoked_Token ON RefreshTokens;
```

### 代码回滚
1. Client端恢复调用Server `POST /api/v1/auth/validate` API
2. Server端恢复`AuthService.ValidateTokenWithDetailsAsync`方法
3. 移除`SecureTokenStorage`、`LocalTokenValidator`
4. 移除`TokenRevocationService`、`SecurityAuditService`

### 部署回滚
1. 数据库执行回滚脚本
2. 部署上一版本代码
3. 清除所有客户端加密Token文件
4. 通知用户重新登录

---

**设计完成**: ✅
**下一步**: 创建ADR文档
**版本**: 1.0

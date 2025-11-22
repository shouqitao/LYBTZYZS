# 安全架构指南

**基于凌隐宝堂中医诊所双轨认证系统的完整安全架构** - 深入理解中医诊所管理系统的安全防护体系

## 🔐 安全架构概览

### 安全架构分层图
```
                    ┌─────────────────────────────────────┐
                    │         Presentation Layer           │
                    │           (表示层安全)                 │
                    │  • JWT令牌验证 • 权限控制 • HTTPS      │
                    └─────────────────────────────────────┘
                                      │
                    ┌─────────────────────┼─────────────────────┐
                    │                     │                     │
        ┌───────────▼─────────┐   ┌─────▼─────┐   ┌─────────▼─────────┐
        │   Application     │   │ Business  │   │   Infrastructure  │
        │    Layer          │   │   Layer   │   │      Layer        │
        │ (应用层安全)        │   │(业务层安全) │   │   (基础设施安全)     │
        │ • 双轨认证          │   │ • 权限验证 │   │ • 数据库安全        │
        │ • 会话管理          │   │ • 业务审计 │   │ • 网络安全          │
        └─────────────────────┘   └───────────┘   └──────────────────┘
                                      │
                    ┌─────────────────────┼─────────────────────┐
                    │                     │                     │
        ┌───────────▼─────────┐   ┌─────▼─────┐   ┌─────────▼─────────┐
        │   Data Layer        │   │ Physical  │   │  Compliance       │
        │   (数据层安全)        │   │  Layer    │   │   (合规性安全)      │
        │ • 数据加密          │   │(物理安全) │   │ • 医疗数据保护      │
        │ • 访问控制          │   │ • 设备安全 │   │ • 隐私保护          │
        └─────────────────────┘   └───────────┘   └──────────────────┘
```

### 安全威胁模型
```
外部威胁：
├── 网络攻击 (DDoS, MITM, SQL注入)
├── 身份冒充 (暴力破解, 凭证重放)
├── 数据泄露 (未授权访问, 数据窃取)
└── 恶意软件 (病毒, 木马, 勒索软件)

内部威胁：
├── 权限滥用 (越权操作, 数据篡改)
├── 人为失误 (配置错误, 操作失误)
├── 数据滥用 (隐私泄露, 商业机密)
└── 恶意行为 (恶意删除, 破坏系统)

合规威胁：
├── 医疗数据保护 (HIPAA, GDPR)
├── 个人信息保护 (个人信息保护法)
├── 审计合规 (医疗行业监管)
└── 业务连续性 (数据备份, 恢复)
```

## 🛡️ 身份认证架构

### 1. 双轨认证系统设计

#### 认证架构图
```
                    ┌─────────────────────────────────────┐
                    │            User Portal             │
                    │              (用户门户)               │
                    └─────────────────────────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │          Authentication Service       │
                    │             (认证服务)                 │
                    └─────────────────────────────────────┘
                                      │
                            ┌─────────┴─────────┐
                            │                   │
                            ▼                   ▼
                ┌─────────────────────┐   ┌─────────────────────┐
                │    Regular Users     │   │   Super Admin       │
                │   (普通用户认证)       │   │   (超级管理员认证)   │
                │                     │   │                     │
                │ Users Table         │   │ AdminSecrets Table  │
                │ - 用户名密码验证      │   │ - 独立密码存储      │
                │ - JWT令牌生成        │   │ - 配置驱动用户名    │
                │ - 会话管理          │   │ - 物理隔离设计      │
                └─────────────────────┘   └─────────────────────┘
```

#### 双轨认证实现
```csharp
/// <summary>
/// 双轨认证服务实现
/// </summary>
public class DualTrackAuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminSecretRepository _adminSecretRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DualTrackAuthService> _logger;
    private readonly AuthSettings _authSettings;

    public DualTrackAuthService(
        IUserRepository userRepository,
        IAdminSecretRepository adminSecretRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<DualTrackAuthService> logger,
        IOptions<AuthSettings> authSettings)
    {
        _userRepository = userRepository;
        _adminSecretRepository = adminSecretRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _authSettings = authSettings.Value;
    }

    /// <summary>
    /// 双轨认证入口
    /// </summary>
    public async Task<ServiceResult<AuthResponse>> AuthenticateAsync(AuthRequest request)
    {
        try
        {
            // 输入验证
            if (!ValidateAuthRequest(request))
                return ServiceResult<AuthResponse>.Failure("输入参数无效");

            // 尝试超级管理员认证
            var adminResult = await AuthenticateSuperAdminAsync(request);
            if (adminResult.IsSuccess)
            {
                return adminResult;
            }

            // 普通用户认证
            var userResult = await AuthenticateRegularUserAsync(request);
            if (userResult.IsSuccess)
            {
                return userResult;
            }

            // 认证失败记录
            await LogAuthenticationFailure(request);
            return ServiceResult<AuthResponse>.Failure("用户名或密码错误");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "认证过程中发生异常");
            return ServiceResult<AuthResponse>.Failure("认证服务异常");
        }
    }

    /// <summary>
    /// 超级管理员认证
    /// </summary>
    private async Task<ServiceResult<AuthResponse>> AuthenticateSuperAdminAsync(AuthRequest request)
    {
        try
        {
            // 验证用户名是否匹配配置
            if (!string.Equals(request.Username, _authSettings.SuperAdmin.Username, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<AuthResponse>.Failure();
            }

            // 获取超级管理员密码哈希
            var adminSecret = await _adminSecretRepository.GetActiveAsync();
            if (adminSecret == null)
            {
                _logger.LogWarning("超级管理员未初始化");
                return ServiceResult<AuthResponse>.Failure();
            }

            // 验证密码
            if (!_passwordHasher.VerifyPassword(request.Password, adminSecret.PasswordHash))
            {
                _logger.LogWarning("超级管理员密码验证失败，用户名: {Username}", request.Username);
                return ServiceResult<AuthResponse>.Failure();
            }

            // 生成超级管理员JWT令牌
            var token = GenerateSuperAdminToken(request.Username);

            // 构建认证响应
            var response = new AuthResponse
            {
                Token = token,
                TokenType = "JWT",
                ExpiresIn = _authSettings.Jwt.AccessTokenExpiration.TotalSeconds,
                User = new UserDto
                {
                    Id = Guid.Empty, // 特殊ID表示超级管理员
                    UserName = _authSettings.SuperAdmin.Username,
                    RealName = "系统超级管理员",
                    Role = UserRole.SuperAdmin,
                    Email = _authSettings.SuperAdmin.Email
                },
                Permissions = GetAllPermissions(),
                SessionInfo = new SessionInfo
                {
                    AuthSource = "AdminSecrets",
                    LoginTime = DateTime.UtcNow,
                    IPAddress = request.IPAddress,
                    UserAgent = request.UserAgent
                }
            };

            // 记录成功日志
            await LogAuthenticationSuccess(request, "SuperAdmin");

            return ServiceResult<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "超级管理员认证失败");
            return ServiceResult<AuthResponse>.Failure();
        }
    }

    /// <summary>
    /// 普通用户认证
    /// </summary>
    private async Task<ServiceResult<AuthResponse>> AuthenticateRegularUserAsync(AuthRequest request)
    {
        try
        {
            // 查找用户
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return ServiceResult<AuthResponse>.Failure();
            }

            // 检查用户状态
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("用户账号已被禁用，用户名: {Username}", request.Username);
                return ServiceResult<AuthResponse>.Failure("账号已被禁用");
            }

            // 验证密码
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                // 更新失败登录次数
                await UpdateFailedLoginAttempts(user.Id);
                return ServiceResult<AuthResponse>.Failure();
            }

            // 生成用户JWT令牌
            var token = GenerateUserToken(user);

            // 构建认证响应
            var response = new AuthResponse
            {
                Token = token,
                TokenType = "JWT",
                ExpiresIn = _authSettings.Jwt.AccessTokenExpiration.TotalSeconds,
                User = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    RealName = user.RealName,
                    Role = user.Role,
                    Email = user.Email
                },
                Permissions = GetPermissionsByRole(user.Role),
                SessionInfo = new SessionInfo
                {
                    AuthSource = "Users",
                    LoginTime = DateTime.UtcNow,
                    IPAddress = request.IPAddress,
                    UserAgent = request.UserAgent
                }
            };

            // 更新登录信息
            await UpdateUserLoginInfo(user.Id, request);

            return ServiceResult<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "普通用户认证失败");
            return ServiceResult<AuthResponse>.Failure();
        }
    }

    /// <summary>
    /// 生成超级管理员令牌
    /// </summary>
    private string GenerateSuperAdminToken(string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000000"), // 特殊ID
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, UserRole.SuperAdmin.ToString()),
            new Claim("IsSuperAdmin", "true"),
            new Claim("AuthSource", "AdminSecrets"),
            new Claim("PermissionLevel", "Full")
        };

        return _jwtService.GenerateToken(claims, _authSettings.Jwt.AccessTokenExpiration);
    }

    /// <summary>
    /// 生成普通用户令牌
    /// </summary>
    private string GenerateUserToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("AuthSource", "Users")
        };

        // 添加角色权限
        var permissions = GetPermissionsByRole(user.Role);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        return _jwtService.GenerateToken(claims, _authSettings.Jwt.AccessTokenExpiration);
    }
}
```

### 2. 密码安全策略

#### 密码哈希实现
```csharp
/// <summary>
/// 密码哈希服务
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly ILogger<PasswordHasher> _logger;
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32; // 256 bit
    private const int Iterations = 10000; // PBKDF2 iterations

    public PasswordHasher(ILogger<PasswordHasher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 哈希密码
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("密码不能为空");

        try
        {
            // 生成随机盐值
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            // 使用PBKDF2生成哈希
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            var hash = pbkdf2.GetBytes(KeySize);

            // 组合盐值和哈希值
            var hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);

            // 转换为Base64字符串
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码哈希生成失败");
            throw;
        }
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            // 解码Base64字符串
            var hashBytes = Convert.FromBase64String(hashedPassword);
            
            if (hashBytes.Length != SaltSize + KeySize)
                return false;

            // 提取盐值
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // 提取哈希值
            var storedHash = new byte[KeySize];
            Array.Copy(hashBytes, SaltSize, storedHash, 0, KeySize);

            // 计算输入密码的哈希值
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            var computedHash = pbkdf2.GetBytes(KeySize);

            // 比较哈希值
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码验证失败");
            return false;
        }
    }

    /// <summary>
    /// 检查密码强度
    /// </summary>
    public PasswordStrength CheckPasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.Weak;

        int score = 0;

        // 长度检查
        if (password.Length >= 8)
            score++;
        if (password.Length >= 12)
            score++;

        // 复杂度检查
        if (Regex.IsMatch(password, @"[a-z]")) // 小写字母
            score++;
        if (Regex.IsMatch(password, @"[A-Z]")) // 大写字母
            score++;
        if (Regex.IsMatch(password, @"[0-9]")) // 数字
            score++;
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?\:{}\[\]<>]")) // 特殊字符
            score++;

        // 常见弱密码检查
        if (IsCommonWeakPassword(password))
            score--;

        return score switch
        {
            >= 4 => PasswordStrength.Strong,
            >= 2 => PasswordStrength.Medium,
            _ => PasswordStrength.Weak
        };
    }

    /// <summary>
    /// 生成强密码
    /// </summary>
    public string GenerateStrongPassword(int length = 16)
    {
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*(),.?{}[]<>";
        const string allChars = lowerCase + upperCase + digits + specialChars;

        using var rng = RandomNumberGenerator.Create();
        var result = new char[length];

        // 确保包含各种类型的字符
        result[0] = lowerCase[rng.Next(lowerCase.Length)];
        result[1] = upperCase[rng.Next(upperCase.Length)];
        result[2] = digits[rng.Next(digits.Length)];
        result[3] = specialChars[rng.Next(specialChars.Length)];

        // 填充剩余位置
        for (int i = 4; i < length; i++)
        {
            result[i] = allChars[rng.Next(allChars.Length)];
        }

        // 打乱字符顺序
        for (int i = 0; i < result.Length; i++)
        {
            int j = rng.Next(i, result.Length);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return new string(result);
    }

    private bool IsCommonWeakPassword(string password)
    {
        var commonPasswords = new[]
        {
            "password", "123456", "12345678", "qwerty", "abc123",
            "password123", "admin", "root", "user", "test"
        };

        return commonPasswords.Contains(password.ToLowerInvariant());
    }
}

/// <summary>
/// 密码强度枚举
/// </summary>
public enum PasswordStrength
{
    Weak,
    Medium,
    Strong
}
```

### 3. 会话管理

#### JWT令牌管理
```csharp
/// <summary>
/// JWT令牌服务
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;

        // 创建签名凭证
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 创建令牌验证参数
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    public string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiration)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.Add(expiration),
                NotBefore = now,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = _signingCredentials,
                IssuedAt = now
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT令牌生成失败");
            throw;
        }
    }

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
            
            // 检查令牌是否在撤销列表中
            if (await IsTokenRevoked(token))
            {
                _logger.LogWarning("令牌已被撤销: {Token}", token.Substring(0, Math.Min(50, token.Length)));
                return null;
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("JWT令牌已过期");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("JWT令牌签名无效");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT令牌验证失败");
            return null;
        }
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // 验证刷新令牌
            var principal = ValidateRefreshToken(refreshToken);
            if (principal == null)
            {
                _logger.LogWarning("刷新令牌验证失败");
                return null;
            }

            // 检查刷新令牌是否过期
            var expClaim = principal.FindFirst("exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("刷新令牌已过期");
                    return null;
                }
            }

            // 生成新的访问令牌
            var newAccessToken = GenerateToken(principal.Claims, _jwtSettings.AccessTokenExpiration);
            
            return newAccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            return null;
        }
    }

    /// <summary>
    /// 撤销令牌
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            // 解析令牌获取JTI
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var jti = jsonToken.Id;

            // 将JTI添加到撤销列表
            await AddToRevocationList(jti, DateTime.UtcNow);
            
            _logger.LogInformation("令牌已撤销: {Jti}", jti);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销令牌失败");
            return false;
        }
    }

    /// <summary>
    /// 检查令牌是否被撤销
    /// </summary>
    private async Task<bool> IsTokenRevoked(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var jti = jsonToken.Id;

            // 检查撤销列表
            return await IsInRevocationList(jti);
        }
        catch
        {
            return true; // 出错时认为已撤销
        }
    }

    private async Task AddToRevocationList(string jti, DateTime revokedAt)
    {
        // 实现撤销列表存储（Redis、数据库等）
        // 这里使用内存存储作为示例
        RevokedTokens[jti] = revokedAt;
    }

    private async Task<bool> IsInRevocationList(string jti)
    {
        return RevokedTokens.ContainsKey(jti);
    }

    // 简化的撤销列表存储（生产环境应使用Redis等）
    private static readonly Dictionary<string, DateTime> RevokedTokens = new();
}
```

## 🚦 访问控制架构

### 1. 基于角色的访问控制 (RBAC)

#### 权限模型设计
```csharp
/// <summary>
/// 权限定义
/// </summary>
public static class Permissions
{
    // 患者管理权限
    public const string PatientRead = "patient:read";
    public const string PatientCreate = "patient:create";
    public const string PatientUpdate = "patient:update";
    public const string PatientDelete = "patient:delete";
    public const string PatientImport = "patient:import";
    public const string PatientExport = "patient:export";

    // 医案管理权限
    public const string MedicalCaseRead = "medicalcase:read";
    public const string MedicalCaseCreate = "medicalcase:create";
    public const string MedicalCaseUpdate = "medicalcase:update";
    public const string MedicalCaseDelete = "medicalcase:delete";
    public const string MedicalCaseArchive = "medicalcase:archive";

    // 处方管理权限
    public const string PrescriptionRead = "prescription:read";
    public const string PrescriptionCreate = "prescription:create";
    public const string PrescriptionUpdate = "prescription:update";
    public const string PrescriptionDelete = "prescription:delete";
    public const string PrescriptionDispense = "prescription:dispense";
    public const string PrescriptionPriceEdit = "prescription:price:edit";

    // 药材管理权限
    public const string HerbRead = "herb:read";
    public const string HerbCreate = "herb:create";
    public const string HerbUpdate = "herb:update";
    public const string HerbDelete = "herb:delete";
    public const string HerbImport = "herb:import";
    public const string HerbPriceEdit = "herb:price:edit";
    public const string HerbStockEdit = "herb:stock:edit";

    // 验方管理权限
    public const string FormulaRead = "formula:read";
    public const string FormulaCreate = "formula:create";
    public const string FormulaUpdate = "formula:update";
    public const string FormulaDelete = "formula:delete";
    public const string FormulaPublish = "formula:publish";

    // 用户管理权限
    public const string UserRead = "user:read";
    public const string UserCreate = "user:create";
    public const string UserUpdate = "user:update";
    public const string UserDelete = "user:delete";
    public const string UserRoleEdit = "user:role:edit";

    // 系统管理权限
    public const string SystemConfig = "system:config";
    public const string SystemBackup = "system:backup";
    public const string SystemAudit = "system:audit";
    public const string SystemMonitor = "system:monitor";

    // 超级管理员权限
    public const string SuperAdmin = "super:admin";
}

/// <summary>
/// 权限服务
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<UserRole, HashSet<string>> _rolePermissions;

    public PermissionService(ILogger<PermissionService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
        _rolePermissions = InitializeRolePermissions();
    }

    /// <summary>
    /// 获取用户权限
    /// </summary>
    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, UserRole role)
    {
        var cacheKey = $"user:permissions:{userId}:{role}";
        
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPermissions))
        {
            return cachedPermissions;
        }

        var permissions = await CalculateUserPermissionsAsync(userId, role);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };
        
        _cache.Set(cacheKey, permissions, cacheOptions);
        
        return permissions;
    }

    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, UserRole role, string permission)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, role);
        return userPermissions.Contains(permission) || userPermissions.Contains(Permissions.SuperAdmin);
    }

    /// <summary>
    /// 检查用户是否有任意权限
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(Guid userId, UserRole role, params string[] permissions)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, role);
        return permissions.Any(p => userPermissions.Contains(p)) || userPermissions.Contains(Permissions.SuperAdmin);
    }

    /// <summary>
    /// 计算用户权限
    /// </summary>
    private async Task<HashSet<string>> CalculateUserPermissionsAsync(Guid userId, UserRole role)
    {
        var permissions = new HashSet<string>();

        // 添加角色基础权限
        if (_rolePermissions.TryGetValue(role, out var rolePerms))
        {
            foreach (var perm in rolePerms)
            {
                permissions.Add(perm);
            }
        }

        // 获取用户自定义权限（如果有）
        var userSpecificPermissions = await GetUserSpecificPermissionsAsync(userId);
        foreach (var perm in userSpecificPermissions)
        {
            permissions.Add(perm);
        }

        // 超级管理员拥有所有权限
        if (role == UserRole.SuperAdmin)
        {
            permissions.Add(Permissions.SuperAdmin);
        }

        return permissions;
    }

    /// <summary>
    /// 获取用户特定权限
    /// </summary>
    private async Task<IEnumerable<string>> GetUserSpecificPermissionsAsync(Guid userId)
    {
        // 从数据库获取用户特定权限
        // 这里返回空列表作为示例
        return await Task.FromResult(Enumerable.Empty<string>());
    }

    /// <summary>
    /// 初始化角色权限映射
    /// </summary>
    private Dictionary<UserRole, HashSet<string>> InitializeRolePermissions()
    {
        return new Dictionary<UserRole, HashSet<string>>
        {
            // 超级管理员权限
            [UserRole.SuperAdmin] = new HashSet<string>
            {
                Permissions.SuperAdmin,
                // 所有权限
                Permissions.PatientRead, Permissions.PatientCreate, Permissions.PatientUpdate, 
                Permissions.PatientDelete, Permissions.PatientImport, Permissions.PatientExport,
                Permissions.MedicalCaseRead, Permissions.MedicalCaseCreate, Permissions.MedicalCaseUpdate,
                Permissions.MedicalCaseDelete, Permissions.MedicalCaseArchive,
                Permissions.PrescriptionRead, Permissions.PrescriptionCreate, Permissions.PrescriptionUpdate,
                Permissions.PrescriptionDelete, Permissions.PrescriptionDispense, Permissions.PrescriptionPriceEdit,
                Permissions.HerbRead, Permissions.HerbCreate, Permissions.HerbUpdate, Permissions.HerbDelete,
                Permissions.HerbImport, Permissions.HerbPriceEdit, Permissions.HerbStockEdit,
                Permissions.FormulaRead, Permissions.FormulaCreate, Permissions.FormulaUpdate,
                Permissions.FormulaDelete, Permissions.FormulaPublish,
                Permissions.UserRead, Permissions.UserCreate, Permissions.UserUpdate,
                Permissions.UserDelete, Permissions.UserRoleEdit,
                Permissions.SystemConfig, Permissions.SystemBackup, Permissions.SystemAudit, Permissions.SystemMonitor
            },

            // 管理员权限
            [UserRole.Admin] = new HashSet<string>
            {
                // 患者管理
                Permissions.PatientRead, Permissions.PatientCreate, Permissions.PatientUpdate, Permissions.PatientDelete,
                // 医案管理
                Permissions.MedicalCaseRead, Permissions.MedicalCaseCreate, Permissions.MedicalCaseUpdate, Permissions.MedicalCaseArchive,
                // 处方管理
                Permissions.PrescriptionRead, Permissions.PrescriptionCreate, Permissions.PrescriptionUpdate, Permissions.PrescriptionDispense,
                // 药材管理
                Permissions.HerbRead, Permissions.HerbCreate, Permissions.HerbUpdate, Permissions.HerbImport,
                // 验方管理
                Permissions.FormulaRead, Permissions.FormulaCreate, Permissions.FormulaUpdate, Permissions.FormulaPublish,
                // 用户管理
                Permissions.UserRead, Permissions.UserCreate, Permissions.UserUpdate,
                // 系统监控
                Permissions.SystemMonitor
            },

            // 医生权限
            [UserRole.Doctor] = new HashSet<string>
            {
                // 患者管理
                Permissions.PatientRead, Permissions.PatientCreate, Permissions.PatientUpdate,
                // 医案管理
                Permissions.MedicalCaseRead, Permissions.MedicalCaseCreate, Permissions.MedicalCaseUpdate,
                // 处方管理
                Permissions.PrescriptionRead, Permissions.PrescriptionCreate, Permissions.PrescriptionUpdate,
                // 药材管理
                Permissions.HerbRead,
                // 验方管理
                Permissions.FormulaRead, Permissions.FormulaPublish
            }
        };
    }
}
```

### 2. 授权中间件

#### API授权中间件
```csharp
/// <summary>
/// JWT授权中间件
/// </summary>
public class JwtAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthorizationMiddleware> _logger;
    private readonly IPermissionService _permissionService;

    public JwtAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthorizationMiddleware> logger,
        IPermissionService permissionService)
    {
        _next = next;
        _logger = logger;
        _permissionService = permissionService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 跳过授权的端点
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await _next(context);
                return;
            }

            // 获取Authorization头
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await HandleUnauthorizedAsync(context, "缺少Authorization头");
                return;
            }

            // 提取令牌
            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
            {
                await HandleUnauthorizedAsync(context, "令牌不能为空");
                return;
            }

            // 验证令牌
            var jwtService = context.RequestServices.GetRequiredService<IJwtService>();
            var principal = jwtService.ValidateToken(token);
            if (principal == null)
            {
                await HandleUnauthorizedAsync(context, "无效的令牌");
                return;
            }

            // 设置用户上下文
            context.User = principal;

            // 检查权限
            await CheckPermissionsAsync(context, principal);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "授权中间件异常");
            await HandleInternalServerErrorAsync(context, "授权验证失败");
        }
    }

    /// <summary>
    /// 检查权限
    /// </summary>
    private async Task CheckPermissionsAsync(HttpContext context, ClaimsPrincipal principal)
    {
        // 获取当前端点信息
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return;

        // 获取权限要求
        var requiredPermissions = endpoint.Metadata.GetMetadata<RequiredPermissionAttribute>();
        if (requiredPermissions == null) return;

        // 提取用户信息
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = principal.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || roleClaim == null)
        {
            throw new UnauthorizedAccessException("用户信息不完整");
        }

        var userId = Guid.Parse(userIdClaim.Value);
        var role = Enum.Parse<UserRole>(roleClaim.Value);

        // 检查权限
        foreach (var permissionAttr in requiredPermissions)
        {
            var hasPermission = await _permissionService.HasPermissionAsync(userId, role, permissionAttr.Permission);
            if (!hasPermission)
            {
                _logger.LogWarning("用户 {UserId} 权限不足，需要权限: {Permission}", userId, permissionAttr.Permission);
                throw new UnauthorizedAccessException($"权限不足，需要权限: {permissionAttr.Permission}");
            }
        }
    }

    /// <summary>
    /// 处理未授权请求
    /// </summary>
    private async Task HandleUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        
        var response = new { 
            success = false, 
            message = message,
            code = "UNAUTHORIZED"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    /// <summary>
    /// 处理内部服务器错误
    /// </summary>
    private async Task HandleInternalServerErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        
        var response = new { 
            success = false, 
            message = "服务器内部错误",
            code = "INTERNAL_ERROR"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// 权限要求特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiredPermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequiredPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// 扩展方法：注册授权中间件
/// </summary>
public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthorizationMiddleware>();
    }
}
```

### 3. 数据级访问控制

#### 数据访问过滤器
```csharp
/// <summary>
/// 数据访问控制服务
/// </summary>
public class DataAccessControlService : IDataAccessControlService
{
    private readonly ILogger<DataAccessControlService> _logger;
    private readonly IPermissionService _permissionService;

    public DataAccessControlService(
        ILogger<DataAccessControlService> logger,
        IPermissionService permissionService)
    {
        _logger = logger;
        _permissionService = permissionService;
    }

    /// <summary>
    /// 应用数据访问过滤
    /// </summary>
    public async Task<IQueryable<T>> ApplyDataFilterAsync<T>(
        IQueryable<T> query, 
        Guid userId, 
        UserRole role) where T : class
    {
        // 根据实体类型应用不同的过滤规则
        if (typeof(T) == typeof(Patient))
        {
            return await ApplyPatientFilterAsync((IQueryable<Patient>)(object)query, userId, role);
        }
        else if (typeof(T) == typeof(MedicalCase))
        {
            return await ApplyMedicalCaseFilterAsync((IQueryable<MedicalCase>)(object)query, userId, role);
        }
        else if (typeof(T) == typeof(Prescription))
        {
            return await ApplyPrescriptionFilterAsync((IQueryable<Prescription>)(object)query, userId, role);
        }

        return query;
    }

    /// <summary>
    /// 应用患者数据过滤
    /// </summary>
    private async Task<IQueryable<Patient>> ApplyPatientFilterAsync(
        IQueryable<Patient> query, 
        Guid userId, 
        UserRole role)
    {
        // 超级管理员可以查看所有患者
        if (role == UserRole.SuperAdmin)
        {
            return query;
        }

        // 管理员可以查看所有患者
        if (role == UserRole.Admin)
        {
            return query;
        }

        // 医生只能查看自己接诊的患者
        if (role == UserRole.Doctor)
        {
            var accessiblePatientIds = await GetDoctorPatientIdsAsync(userId);
            query = query.Where(p => accessiblePatientIds.Contains(p.Id));
        }

        return query;
    }

    /// <summary>
    /// 应用医案数据过滤
    /// </summary>
    private async Task<IQueryable<MedicalCase>> ApplyMedicalCaseFilterAsync(
        IQueryable<MedicalCase> query, 
        Guid userId, 
        UserRole role)
    {
        // 超级管理员可以查看所有医案
        if (role == UserRole.SuperAdmin)
        {
            return query;
        }

        // 管理员可以查看所有医案
        if (role == UserRole.Admin)
        {
            return query;
        }

        // 医生只能查看自己创建的医案
        if (role == UserRole.Doctor)
        {
            query = query.Where(mc => mc.DoctorId == userId);
        }

        return query;
    }

    /// <summary>
    /// 应用处方数据过滤
    /// </summary>
    private async Task<IQueryable<Prescription>> ApplyPrescriptionFilterAsync(
        IQueryable<Prescription> query, 
        Guid userId, 
        UserRole role)
    {
        // 超级管理员可以查看所有处方
        if (role == UserRole.SuperAdmin)
        {
            return query;
        }

        // 管理员可以查看所有处方
        if (role == UserRole.Admin)
        {
            return query;
        }

        // 医生只能查看自己开出的处方
        if (role == UserRole.Doctor)
        {
            query = query.Where(p => p.DoctorId == userId);
        }

        return query;
    }

    /// <summary>
    /// 获取医生可访问的患者ID列表
    /// </summary>
    private async Task<List<Guid>> GetDoctorPatientIdsAsync(Guid doctorId)
    {
        // 从数据库获取医生接诊过的患者ID列表
        // 这里返回空列表作为示例
        return await Task.FromResult(new List<Guid>());
    }
}
```

## 🔒 数据安全架构

### 1. 数据加密策略

#### 敏感数据加密
```csharp
/// <summary>
/// 数据加密服务
/// </summary>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly ILogger<DataEncryptionService> _logger;
    private readonly AesEncryptionProvider _aesProvider;

    public DataEncryptionService(ILogger<DataEncryptionService> logger, AesEncryptionProvider aesProvider)
    {
        _logger = logger;
        _aesProvider = aesProvider;
    }

    /// <summary>
    /// 加密敏感字段
    /// </summary>
    public async Task<T> EncryptSensitiveFieldsAsync<T>(T entity) where T : class
    {
        try
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<EncryptAttribute>() != null);

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    var encryptedValue = await _aesProvider.EncryptAsync(stringValue);
                    property.SetValue(entity, encryptedValue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密敏感字段失败");
            throw;
        }
    }

    /// <summary>
    /// 解密敏感字段
    /// </summary>
    public async Task<T> DecryptSensitiveFieldsAsync<T>(T entity) where T : class
    {
        try
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<EncryptAttribute>() != null);

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                if (value is string encryptedValue && !string.IsNullOrEmpty(encryptedValue))
                {
                    var decryptedValue = await _aesProvider.DecryptAsync(encryptedValue);
                    property.SetValue(entity, decryptedValue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密敏感字段失败");
            throw;
        }
    }

    /// <summary>
    /// 批量加密患者数据
    /// </summary>
    public async Task<int> EncryptPatientDataAsync(IEnumerable<Patient> patients)
    {
        int encryptedCount = 0;
        
        foreach (var patient in patients)
        {
            try
            {
                await EncryptSensitiveFieldsAsync(patient);
                encryptedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加密患者数据失败，患者ID: {PatientId}", patient.Id);
            }
        }

        return encryptedCount;
    }
}

/// <summary>
/// 加密标记特性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EncryptAttribute : Attribute
{
    public EncryptAttribute()
    {
    }
}

/// <summary>
/// AES加密提供者
/// </summary>
public class AesEncryptionProvider
{
    private readonly ILogger<AesEncryptionProvider> _logger;
    private readonly EncryptionSettings _settings;

    public AesEncryptionProvider(ILogger<AesEncryptionProvider> logger, IOptions<EncryptionSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// 加密字符串
    /// </summary>
    public async Task<string> EncryptAsync(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_settings.AesKey);
            aes.IV = Convert.FromBase64String(_settings.AesIV);

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);
            
            await swEncrypt.WriteAsync(plainText);
            csEncrypt.FlushFinalBlock();
            
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES加密失败");
            throw;
        }
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    public async Task<string> DecryptAsync(string cipherText)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_settings.AesKey);
            aes.IV = Convert.FromBase64String(_settings.AesIV);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherBytes);
            
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return await srDecrypt.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AES解密失败");
            throw;
        }
    }
}
```

### 2. 数据脱敏策略

#### 数据脱敏实现
```csharp
/// <summary>
/// 数据脱敏服务
/// </summary>
public class DataMaskingService : IDataMaskingService
{
    private readonly ILogger<DataMaskingService> _logger;

    public DataMaskingService(ILogger<DataMaskingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 脱敏患者信息
    /// </summary>
    public PatientDto MaskPatientData(PatientDto patient, UserRole viewerRole, bool includeSensitiveInfo = false)
    {
        if (viewerRole == UserRole.SuperAdmin || includeSensitiveInfo)
        {
            return patient; // 超级管理员或明确需要敏感信息时不脱敏
        }

        var maskedPatient = new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            Gender = patient.Gender,
            Age = patient.Age,
            Status = patient.Status,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };

        // 脱敏身份证号（显示前6位，后4位，中间用*替代）
        if (!string.IsNullOrEmpty(patient.IdNumber))
        {
            maskedPatient.IdNumber = MaskIdNumber(patient.IdNumber);
        }

        // 脱敏手机号（显示前3位，后4位，中间用*替代）
        if (!string.IsNullOrEmpty(patient.PhoneNumber))
        {
            maskedPatient.PhoneNumber = MaskPhoneNumber(patient.PhoneNumber);
        }

        // 脱敏地址（只显示前20个字符）
        if (!string.IsNullOrEmpty(patient.Address))
        {
            maskedPatient.Address = MaskAddress(patient.Address);
        }

        return maskedPatient;
    }

    /// <summary>
    /// 脱敏身份证号
    /// </summary>
    private string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return "***";

        return idNumber.Substring(0, 6) + new string('*', idNumber.Length - 10) + idNumber.Substring(idNumber.Length - 4);
    }

    /// <summary>
    /// 脱敏手机号
    /// </summary>
    private string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
            return "***";

        return phoneNumber.Substring(0, 3) + new string('*', phoneNumber.Length - 7) + phoneNumber.Substring(phoneNumber.Length - 4);
    }

    /// <summary>
    /// 脱敏地址
    /// </summary>
    private string MaskAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return string.Empty;

        if (address.Length <= 20)
            return address;

        return address.Substring(0, 20) + "...";
    }

    /// <summary>
    /// 脱敏医案信息
    /// </summary>
    public MedicalCaseDto MaskMedicalCaseData(MedicalCaseDto medicalCase, UserRole viewerRole, bool includeSensitiveInfo = false)
    {
        if (viewerRole == UserRole.SuperAdmin || includeSensitiveInfo)
        {
            return medicalCase;
        }

        var maskedCase = new MedicalCaseDto
        {
            Id = medicalCase.Id,
            PatientId = medicalCase.PatientId,
            PatientName = medicalCase.PatientName,
            DoctorId = medicalCase.DoctorId,
            DoctorName = medicalCase.DoctorName,
            CaseNumber = medicalCase.CaseNumber,
            Title = medicalCase.Title,
            ChiefComplaint = medicalCase.ChiefComplaint,
            Status = medicalCase.Status,
            Priority = medicalCase.Priority,
            CreatedAt = medicalCase.CreatedAt,
            UpdatedAt = medicalCase.UpdatedAt
        };

        // 脱敏详细的病史信息
        if (!string.IsNullOrEmpty(medicalCase.PastHistory))
        {
            maskedCase.PastHistory = MaskMedicalText(medicalCase.PastHistory);
        }

        if (!string.IsNullOrEmpty(medicalCase.FamilyHistory))
        {
            maskedCase.FamilyHistory = MaskMedicalText(medicalCase.FamilyHistory);
        }

        return maskedCase;
    }

    /// <summary>
    /// 脱敏医疗文本信息
    /// </summary>
    private string MaskMedicalText(string medicalText)
    {
        if (string.IsNullOrEmpty(medicalText))
            return string.Empty;

        if (medicalText.Length <= 50)
            return medicalText;

        return medicalText.Substring(0, 50) + "...";
    }
}
```

### 3. 数据备份安全

#### 加密备份策略
```csharp
/// <summary>
/// 安全备份服务
/// </summary>
public class SecureBackupService : IBackupService
{
    private readonly ILogger<SecureBackupService> _logger;
    private readonly IDataEncryptionService _encryptionService;
    private readonly BackupSettings _backupSettings;

    public SecureBackupService(
        ILogger<SecureBackupService> logger,
        IDataEncryptionService encryptionService,
        IOptions<BackupSettings> backupSettings)
    {
        _logger = logger;
        _encryptionService = encryptionService;
        _backupSettings = backupSettings.Value;
    }

    /// <summary>
    /// 创建加密备份
    /// </summary>
    public async Task<BackupResult> CreateSecureBackupAsync(BackupRequest request)
    {
        try
        {
            _logger.LogInformation("开始创建安全备份: {BackupType}", request.BackupType);

            var backupId = Guid.NewGuid();
            var backupPath = GetBackupPath(backupId, request.BackupType);

            // 创建备份目录
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

            // 执行备份
            switch (request.BackupType)
            {
                case BackupType.Full:
                    return await CreateFullBackupAsync(backupPath, request);
                case BackupType.Differential:
                    return await CreateDifferentialBackupAsync(backupPath, request);
                case BackupType.Incremental:
                    return await CreateIncrementalBackupAsync(backupPath, request);
                default:
                    throw new ArgumentException($"不支持的备份类型: {request.BackupType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建安全备份失败");
            return new BackupResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 创建完整备份
    /// </summary>
    private async Task<BackupResult> CreateFullBackupAsync(string backupPath, BackupRequest request)
    {
        // 获取需要备份的数据
        var backupData = await GetBackupDataAsync(request.Tables);

        // 加密敏感数据
        await EncryptSensitiveDataAsync(backupData);

        // 序列化数据
        var jsonData = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });

        // 压缩数据
        var compressedData = await CompressDataAsync(jsonData);

        // 加密备份数据
        var encryptedData = await _encryptionService.EncryptSensitiveFieldsAsync(compressedData);

        // 写入文件
        await File.WriteAllBytesAsync(backupPath, encryptedData);

        // 生成备份元数据
        var metadata = new BackupMetadata
        {
            BackupId = Guid.NewGuid(),
            BackupType = BackupType.Full,
            BackupPath = backupPath,
            CreatedAt = DateTime.UtcNow,
            FileSize = new FileInfo(backupPath).Length,
            Checksum = CalculateChecksum(encryptedData),
            Encrypted = true,
            Compressed = true
        };

        // 保存元数据
        await SaveBackupMetadataAsync(metadata);

        // 验证备份
        var validationResult = await ValidateBackupAsync(backupPath, metadata);
        if (!validationResult)
        {
            File.Delete(backupPath);
            return new BackupResult { Success = false, ErrorMessage = "备份验证失败" };
        }

        _logger.LogInformation("完整备份创建成功: {BackupPath}", backupPath);
        return new BackupResult { Success = true, BackupPath = backupPath, Metadata = metadata };
    }

    /// <summary>
    /// 恢复备份
    /// </summary>
    public async Task<RestoreResult> RestoreFromBackupAsync(string backupPath, RestoreRequest request)
    {
        try
        {
            _logger.LogInformation("开始恢复备份: {BackupPath}", backupPath);

            // 验证备份文件存在
            if (!File.Exists(backupPath))
            {
                return new RestoreResult { Success = false, ErrorMessage = "备份文件不存在" };
            }

            // 读取备份文件
            var encryptedData = await File.ReadAllBytesAsync(backupPath);

            // 获取备份元数据
            var metadata = await GetBackupMetadataAsync(backupPath);
            if (metadata == null)
            {
                return new RestoreResult { Success = false, ErrorMessage = "备份元数据不存在" };
            }

            // 验证备份完整性
            var currentChecksum = CalculateChecksum(encryptedData);
            if (currentChecksum != metadata.Checksum)
            {
                return new RestoreResult { Success = false, ErrorMessage = "备份文件已损坏" };
            }

            // 解密数据
            var decryptedData = await _encryptionService.DecryptSensitiveFieldsAsync(encryptedData);

            // 解压缩数据
            var jsonData = await DecompressDataAsync(decryptedData);

            // 反序列化数据
            var backupData = JsonSerializer.Deserialize<BackupData>(jsonData);

            // 恢复数据
            await RestoreDataAsync(backupData, request);

            _logger.LogInformation("备份恢复成功: {BackupPath}", backupPath);
            return new RestoreResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复备份失败");
            return new RestoreResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// 计算文件校验和
    /// </summary>
    private string CalculateChecksum(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// 压缩数据
    /// </summary>
    private async Task<byte[]> CompressDataAsync(string data)
    {
        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionMode.Compress);
        
        using var writer = new StreamWriter(gzip);
        await writer.WriteAsync(data);
        await writer.FlushAsync();
        
        return output.ToArray();
    }

    /// <summary>
    /// 解压缩数据
    /// </summary>
    private async Task<string> DecompressDataAsync(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync();
    }
}
```

## 📊 安全监控与审计

### 1. 安全事件监控

#### 安全事件记录
```csharp
/// <summary>
/// 安全事件服务
/// </summary>
public class SecurityEventService : ISecurityEventService
{
    private readonly ILogger<SecurityEventService> _logger;
    private readonly ISecurityEventRepository _repository;

    public SecurityEventService(
        ILogger<SecurityEventService> logger,
        ISecurityEventRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 记录登录成功事件
    /// </summary>
    public async Task LogLoginSuccessAsync(LoginEvent loginEvent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.LoginSuccess,
            UserId = loginEvent.UserId,
            UserName = loginEvent.UserName,
            IPAddress = loginEvent.IPAddress,
            UserAgent = loginEvent.UserAgent,
            EventTime = DateTime.UtcNow,
            Description = $"用户 {loginEvent.UserName} 登录成功",
            Severity = SecurityEventSeverity.Info,
            Status = SecurityEventStatus.Success
        };

        await _repository.CreateAsync(securityEvent);
        _logger.LogInformation("用户登录成功: {UserId} - {UserName} - {IP}", 
            loginEvent.UserId, loginEvent.UserName, loginEvent.IPAddress);
    }

    /// <summary>
    /// 记录登录失败事件
    /// </summary>
    public async Task LogLoginFailureAsync(LoginFailureEvent loginFailureEvent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.LoginFailure,
            UserName = loginFailureEvent.UserName,
            IPAddress = loginFailureEvent.IPAddress,
            UserAgent = loginFailureEvent.UserAgent,
            EventTime = DateTime.UtcNow,
            Description = $"用户 {loginFailureEvent.UserName} 登录失败: {loginFailureEvent.FailureReason}",
            Severity = loginFailureEvent.FailureReason.Contains("密码") ? 
                SecurityEventSeverity.Warning : SecurityEventSeverity.High,
            Status = SecurityEventStatus.Failure,
            AdditionalData = new
            {
                FailureReason = loginFailureEvent.FailureReason,
                AttemptCount = loginFailureEvent.AttemptCount
            }
        };

        await _repository.CreateAsync(securityEvent);
        
        // 检查是否需要触发安全响应
        await CheckSecurityThresholdsAsync(loginFailureEvent.UserName, loginFailureEvent.IPAddress);
        
        _logger.LogWarning("用户登录失败: {UserName} - {Reason} - {IP} - 尝试次数: {Attempts}", 
            loginFailureEvent.UserName, loginFailureEvent.FailureReason, 
            loginFailureEvent.IPAddress, loginFailureEvent.AttemptCount);
    }

    /// <summary>
    /// 记录权限检查事件
    /// </summary>
    public async Task LogPermissionCheckAsync(PermissionCheckEvent permissionEvent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = permissionEvent.HasPermission ? 
                SecurityEventType.PermissionGranted : SecurityEventType.PermissionDenied,
            UserId = permissionEvent.UserId,
            UserName = permissionEvent.UserName,
            IPAddress = permissionEvent.IPAddress,
            EventTime = DateTime.UtcNow,
            Description = permissionEvent.HasPermission ? 
                $"权限检查通过: {permissionEvent.Permission}" : 
                $"权限检查失败: {permissionEvent.Permission}",
            Severity = permissionEvent.HasPermission ? 
                SecurityEventSeverity.Info : SecurityEventSeverity.Warning,
            Status = permissionEvent.HasPermission ? 
                SecurityEventStatus.Success : SecurityEventStatus.Failure,
            AdditionalData = new
            {
                Permission = permissionEvent.Permission,
                Resource = permissionEvent.Resource,
                Action = permissionEvent.Action
            }
        };

        await _repository.CreateAsync(securityEvent);
        
        if (!permissionEvent.HasPermission)
        {
            _logger.LogWarning("权限检查失败: {UserId} - {Permission} - {Resource}", 
                permissionEvent.UserId, permissionEvent.Permission, permissionEvent.Resource);
        }
    }

    /// <summary>
    /// 记录数据访问事件
    /// </summary>
    public async Task LogDataAccessAsync(DataAccessEvent dataAccessEvent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.DataAccess,
            UserId = dataAccessEvent.UserId,
            UserName = dataAccessEvent.UserName,
            IPAddress = dataAccessEvent.IPAddress,
            EventTime = DateTime.UtcNow,
            Description = $"数据访问: {dataAccessEvent.EntityType} - {dataAccessEvent.Action} - {dataAccessEvent.RecordId}",
            Severity = SecurityEventSeverity.Info,
            Status = SecurityEventStatus.Success,
            AdditionalData = new
            {
                EntityType = dataAccessEvent.EntityType,
                Action = dataAccessEvent.Action,
                RecordId = dataAccessEvent.RecordId,
                AffectedFields = dataAccessEvent.AffectedFields
            }
        };

        await _repository.CreateAsync(securityEvent);
        
        // 对于敏感数据访问，记录详细日志
        if (IsSensitiveEntityType(dataAccessEvent.EntityType))
        {
            _logger.LogInformation("敏感数据访问: {UserId} - {EntityType} - {Action} - {RecordId}", 
                dataAccessEvent.UserId, dataAccessEvent.EntityType, dataAccessEvent.Action, dataAccessEvent.RecordId);
        }
    }

    /// <summary>
    /// 记录异常行为事件
    /// </summary>
    public async Task LogSuspiciousActivityAsync(SuspiciousActivityEvent suspiciousEvent)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = SecurityEventType.SuspiciousActivity,
            UserId = suspiciousEvent.UserId,
            UserName = suspiciousEvent.UserName,
            IPAddress = suspiciousEvent.IPAddress,
            EventTime = DateTime.UtcNow,
            Description = $"检测到可疑行为: {suspiciousEvent.ActivityType} - {suspiciousEvent.Description}",
            Severity = SecurityEventSeverity.High,
            Status = SecurityEventStatus.Detected,
            AdditionalData = new
            {
                ActivityType = suspiciousEvent.ActivityType,
                Description = suspiciousEvent.Description,
                RiskScore = suspiciousEvent.RiskScore
            }
        };

        await _repository.CreateAsync(securityEvent);
        
        _logger.LogWarning("检测到可疑行为: {UserId} - {ActivityType} - 风险评分: {RiskScore}", 
            suspiciousEvent.UserId, suspiciousEvent.ActivityType, suspiciousEvent.RiskScore);

        // 触发安全响应
        await TriggerSecurityResponseAsync(suspiciousEvent);
    }

    /// <summary>
    /// 检查安全阈值
    /// </summary>
    private async Task CheckSecurityThresholdsAsync(string userName, string ipAddress)
    {
        var recentFailures = await _repository.GetRecentLoginFailuresAsync(
            userName, ipAddress, TimeSpan.FromMinutes(15));

        if (recentFailures.Count >= 5)
        {
            // 触发账户锁定
            await TriggerAccountLockoutAsync(userName, ipAddress);
        }
    }

    /// <summary>
    /// 触发安全响应
    /// </summary>
    private async Task TriggerSecurityResponseAsync(SuspiciousActivityEvent suspiciousEvent)
    {
        switch (suspiciousEvent.ActivityType)
        {
            case "BruteForceAttack":
                await TriggerAccountLockoutAsync(suspiciousEvent.UserName, suspiciousEvent.IPAddress);
                break;
            case "UnauthorizedAccess":
                await TriggerAlertAsync(suspiciousEvent);
                break;
            case "DataExfiltration":
                await TriggerEmergencyResponseAsync(suspiciousEvent);
                break;
            default:
                await TriggerAlertAsync(suspiciousEvent);
                break;
        }
    }

    private bool IsSensitiveEntityType(string entityType)
    {
        var sensitiveEntities = new[] { "Patient", "MedicalCase", "Prescription", "Users", "AdminSecrets" };
        return sensitiveEntities.Contains(entityType);
    }
}
```

### 2. 安全仪表板

#### 安全监控仪表板
```csharp
/// <summary>
/// 安全监控服务
/// </summary>
public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly ISecurityEventRepository _repository;
    private readonly IMemoryCache _cache;

    public SecurityMonitoringService(
        ILogger<SecurityMonitoringService> logger,
        ISecurityEventRepository repository,
        IMemoryCache cache)
    {
        _logger = logger;
        _repository = repository;
        _cache = cache;
    }

    /// <summary>
    /// 获取安全概览
    /// </summary>
    public async Task<SecurityOverview> GetSecurityOverviewAsync()
    {
        var cacheKey = "security:overview";
        
        if (_cache.TryGetValue(cacheKey, out SecurityOverview? cachedOverview))
        {
            return cachedOverview;
        }

        var now = DateTime.UtcNow;
        var last24Hours = now.AddDays(-1);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        var overview = new SecurityOverview
        {
            // 登录统计
            LoginStats = new LoginStatistics
            {
                TotalLogins = await _repository.GetEventCountAsync(SecurityEventType.LoginSuccess, last24Hours),
                SuccessfulLogins = await _repository.GetEventCountAsync(SecurityEventType.LoginSuccess, last24Hours),
                FailedLogins = await _repository.GetEventCountAsync(SecurityEventType.LoginFailure, last24Hours),
                FailedLoginsTrend = await GetEventTrendAsync(SecurityEventType.LoginFailure, last7Days)
            },

            // 权限检查统计
            PermissionStats = new PermissionStatistics
            {
                TotalChecks = await _repository.GetEventCountAsync(SecurityEventType.PermissionCheck, last24Hours),
                GrantedPermissions = await _repository.GetEventCountAsync(SecurityEventType.PermissionGranted, last24Hours),
                DeniedPermissions = await _repository.GetEventCountAsync(SecurityEventType.PermissionDenied, last24Hours)
            },

            // 可疑活动统计
            SuspiciousStats = new SuspiciousActivityStatistics
            {
                TotalActivities = await _repository.GetEventCountAsync(SecurityEventType.SuspiciousActivity, last24Hours),
                HighRiskActivities = await _repository.GetHighRiskEventCountAsync(last24Hours),
                ActivitiesTrend = await GetEventTrendAsync(SecurityEventType.SuspiciousActivity, last7Days)
            },

            // 数据访问统计
            DataAccessStats = new DataAccessStatistics
            {
                TotalAccesses = await _repository.GetEventCountAsync(SecurityEventType.DataAccess, last24Hours),
                SensitiveDataAccesses = await _repository.GetSensitiveDataAccessCountAsync(last24Hours),
                DataAccessTrend = await GetEventTrendAsync(SecurityEventType.DataAccess, last7Days)
            },

            // 系统状态
            SystemStatus = await GetSystemStatusAsync()
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
        
        _cache.Set(cacheKey, overview, cacheOptions);
        return overview;
    }

    /// <summary>
    /// 获取实时威胁检测
    /// </summary>
    public async Task<List<ThreatDetection>> GetRealTimeThreatsAsync()
    {
        var threats = new List<ThreatDetection>();

        // 检测暴力破解攻击
        var bruteForceThreats = await DetectBruteForceAttacksAsync();
        threats.AddRange(bruteForceThreats);

        // 检测异常登录模式
        var anomalousLoginThreats = await DetectAnomalousLoginsAsync();
        threats.AddRange(anomalousLoginThreats);

        // 检测权限滥用
        var permissionAbuseThreats = await DetectPermissionAbuseAsync();
        threats.AddRange(permissionAbuseThreats);

        // 检测数据访问异常
        var dataAccessThreats = await DetectDataAccessAnomaliesAsync();
        threats.AddRange(dataAccessThreats);

        return threats.OrderByDescending(t => t.RiskScore).ToList();
    }

    /// <summary>
    /// 检测暴力破解攻击
    /// </summary>
    private async Task<List<ThreatDetection>> DetectBruteForceAttacksAsync()
    {
        var threats = new List<ThreatDetection>();
        
        // 获取最近的登录失败事件
        var recentFailures = await _repository.GetRecentEventsAsync(
            SecurityEventType.LoginFailure, 
            TimeSpan.FromMinutes(15));

        // 按用户名分组
        var failuresByUser = recentFailures
            .Where(e => !string.IsNullOrEmpty(e.UserName))
            .GroupBy(e => e.UserName)
            .Select(g => new
            {
                UserName = g.Key,
                Count = g.Count(),
                IPAddresses = g.Select(e => e.IPAddress).Distinct().ToList(),
                LastFailure = g.Max(e => e.EventTime)
            })
            .Where(g => g.Count >= 5);

        foreach (var userFailure in failuresByUser)
        {
            var threat = new ThreatDetection
            {
                Id = Guid.NewGuid(),
                ThreatType = ThreatType.BruteForceAttack,
                Description = $"检测到暴力破解攻击: 用户 {userFailure.UserName} 在15分钟内登录失败 {userFailure.Count} 次",
                RiskScore = CalculateRiskScore(userFailure.Count),
                Severity = GetSeverityByRiskScore(CalculateRiskScore(userFailure.Count)),
                DetectedAt = DateTime.UtcNow,
                Target = userFailure.UserName,
                Source = userFailure.IPAddresses,
                Details = new
                {
                    UserName = userFailure.UserName,
                    FailureCount = userFailure.Count,
                    IPAddresses = userFailure.IPAddresses,
                    LastFailure = userFailure.LastFailure
                }
            };

            threats.Add(threat);
        }

        return threats;
    }

    /// <summary>
    /// 检测异常登录模式
    /// </summary>
    private async Task<List<ThreatDetection>> DetectAnomalousLoginsAsync()
    {
        var threats = new List<ThreatDetection>();

        // 检测来自异常地理位置的登录
        var geoAnomalies = await DetectGeographicalAnomaliesAsync();
        threats.AddRange(geoAnomalies);

        // 检测异常时间段的登录
        var timeAnomalies = await DetectTimeAnomaliesAsync();
        threats.AddRange(timeAnomalies);

        return threats;
    }

    /// <summary>
    /// 计算风险评分
    /// </summary>
    private int CalculateRiskScore(int failureCount)
    {
        // 基础评分：失败次数 * 10
        var baseScore = failureCount * 10;

        // 阈值调整：超过10次失败，评分大幅增加
        if (failureCount > 10)
        {
            baseScore += (failureCount - 10) * 20;
        }

        // 超过20次失败，评分达到最高级别
        if (failureCount > 20)
        {
            baseScore = 300;
        }

        return Math.Min(baseScore, 300);
    }

    /// <summary>
    /// 根据风险评分获取严重级别
    /// </summary>
    private ThreatSeverity GetSeverityByRiskScore(int riskScore)
    {
        return riskScore switch
        {
            >= 200 => ThreatSeverity.Critical,
            >= 100 => ThreatSeverity.High,
            >= 50 => ThreatSeverity.Medium,
            _ => ThreatSeverity.Low
        };
    }

    /// <summary>
    /// 获取系统状态
    /// </summary>
    private async Task<SystemSecurityStatus> GetSystemStatusAsync()
    {
        var lastHour = DateTime.UtcNow.AddHours(-1);
        
        var criticalEvents = await _repository.GetEventCountBySeverityAsync(
            SecurityEventSeverity.Critical, lastHour);
        
        var highEvents = await _repository.GetEventCountBySeverityAsync(
            SecurityEventSeverity.High, lastHour);

        var status = new SystemSecurityStatus
        {
            OverallStatus = GetOverallStatus(criticalEvents, highEvents),
            CriticalAlerts = criticalEvents,
            HighAlerts = highEvents,
            LastChecked = DateTime.UtcNow
        };

        return status;
    }

    private SecurityStatus GetOverallStatus(int critical, int high)
    {
        if (critical > 0) return SecurityStatus.Critical;
        if (high > 5) return SecurityStatus.Warning;
        if (high > 0) return SecurityStatus.Monitor;
        return SecurityStatus.Healthy;
    }
}
```

---

## 📚 安全最佳实践

### ✅ 推荐做法

1. **认证安全**
   - 实施多因素认证
   - 使用强密码策略
   - 定期更新密码
   - 监控异常登录

2. **授权控制**
   - 最小权限原则
   - 定期权限审查
   - 详细权限日志
   - 数据级访问控制

3. **数据保护**
   - 敏感数据加密
   - 数据脱敏处理
   - 安全备份策略
   - 数据传输加密

4. **监控审计**
   - 实时安全监控
   - 定期安全审计
   - 异常行为检测
   - 威胁响应机制

### ❌ 避免做法

1. **认证漏洞**
   - 明文存储密码
   - 弱密码策略
   - 会话劫持
   - 认证绕过

2. **授权漏洞**
   - 权限提升
   - 越权访问
   - 权限绕过
   - 横向访问

3. **数据泄露**
   - 未授权访问
   - 数据明文传输
   - 缺少数据加密
   - 不当的数据共享

4. **监控盲点**
   - 缺少日志记录
   - 无异常检测
   - 无实时监控
   - 响应延迟

---

*此安全架构指南基于凌隐宝堂中医诊所项目的双轨认证系统和安全需求编写，为系统安全提供全面的指导原则和实施方案。*
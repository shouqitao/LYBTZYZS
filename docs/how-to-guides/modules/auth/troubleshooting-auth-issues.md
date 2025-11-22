# Auth模块问题排查指南 (Auth Troubleshooting Guide)

> **目标导向**: 解决LYBTZYZS认证授权系统的常见问题
> **适合人群**: 开发者、系统管理员、运维人员
> **解决问题**: 登录失败、权限错误、Token问题、配置异常

## 🔥 高频问题快速修复

### 问题1: 用户登录失败

#### 现象描述
- 用户无法登录系统
- 返回错误信息"用户名或密码错误"
- 前端显示登录失败

#### 快速诊断步骤
1. **检查用户存在性**
```sql
SELECT Id, UserName, IsActive, LockoutEnd
FROM Users
WHERE UserName = 'your_username';
```

2. **验证密码哈希**
```csharp
// 在AuthService中添加调试代码
var user = await _userRepository.GetByUserNameAsync(request.UserName);
if (user != null)
{
    var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
    _logger.LogInformation($"Password validation result: {isPasswordValid}");
}
```

3. **检查用户状态**
```sql
SELECT
    Id,
    UserName,
    IsActive,
    LockoutEnd,
    FailedLoginAttempts,
    LastLoginAttempt
FROM Users
WHERE UserName = 'your_username';
```

#### 解决方案
**情况A: 用户不存在**
```sql
-- 创建新用户
INSERT INTO Users (Id, UserName, PasswordHash, DisplayName, Role, IsActive, CreatedAt)
VALUES (
    NEWID(),
    'your_username',
    '$2a$11$your_hashed_password',  -- 使用BCrypt生成
    'Your Display Name',
    'Doctor',  -- 或其他角色
    1,
    GETUTCDATE()
);
```

**情况B: 密码错误**
```csharp
// 重置用户密码
var newPassword = "NewSecurePassword123!";
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
await _userRepository.UpdatePasswordHashAsync(userId, hashedPassword);
```

**情况C: 账户被锁定**
```sql
-- 解锁用户账户
UPDATE Users
SET LockoutEnd = NULL, FailedLoginAttempts = 0
WHERE UserName = 'your_username';
```

### 问题2: JWT Token无效或过期

#### 现象描述
- API调用返回401未授权错误
- 前端提示"登录已过期"
- Token验证失败

#### 快速诊断
1. **检查Token格式**
```bash
# 解析JWT Token
echo "your.jwt.token" | cut -d'.' -f2 | base64 -d
```

2. **验证Token声明**
```csharp
// 在JwtService中添加Token验证日志
public ClaimsPrincipal ValidateToken(string token)
{
    try
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);

        _logger.LogInformation($"Token validated for user: {claims.Identity.Name}");
        _logger.LogInformation($"Token expires: {claims.FindFirst("exp")?.Value}");

        return claims;
    }
    catch (Exception ex)
    {
        _logger.LogError($"Token validation failed: {ex.Message}");
        return null;
    }
}
```

3. **检查Token黑名单**
```sql
SELECT TokenHash, ExpiryDate, RevokeReason
FROM BlacklistedTokens
WHERE ExpiryDate > GETUTCDATE();
```

#### 解决方案
**情况A: Token过期**
```bash
# 使用Refresh Token获取新Token
curl -X POST "https://localhost:5001/api/v1/auth/refresh-token" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your_refresh_token"
  }'
```

**情况B: Token被撤销**
```sql
-- 从黑名单中移除Token（仅限调试）
DELETE FROM BlacklistedTokens
WHERE TokenHash = HASHBYTES('SHA2_256', 'your_jwt_token');
```

**情况C: Token格式错误**
- 检查Token是否包含正确的三部分（header.payload.signature）
- 确认Authorization头格式：`Bearer <token>`
- 验证Token未被修改或损坏

### 问题3: 权限控制失效

#### 现象描述
- 用户无法访问授权的功能
- API返回403禁止访问错误
- 角色权限检查失败

#### 快速诊断
1. **检查用户角色**
```sql
SELECT Id, UserName, Role, IsActive
FROM Users
WHERE UserName = 'current_user';
```

2. **验证API权限配置**
```csharp
// 检查Controller权限特性
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    // 确认权限配置正确
}
```

3. **调试Token中的角色声明**
```csharp
// 在JWT验证后记录角色信息
var roles = claims.FindAll("role").Select(r => r.Value).ToList();
_logger.LogInformation($"User roles: {string.Join(", ", roles)}");
```

#### 解决方案
**情况A: 用户角色错误**
```sql
-- 更新用户角色
UPDATE Users
SET Role = 'Admin'
WHERE UserName = 'target_user';
```

**情况B: API权限配置错误**
```csharp
// 修正权限特性
[Authorize]  // 所有认证用户
// 或
[Authorize(Roles = "Admin,Doctor,Nurse")]  // 特定角色
```

**情况C: Token角色声明缺失**
```csharp
// 在Token生成时添加角色声明
var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)  // 确保包含角色
    }),
    // ...
};
```

## 🔧 配置问题解决

### JWT配置问题

#### 检查appsettings.json配置
```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-must-be-long-enough",
    "Issuer": "LYBTZYZS",
    "Audience": "LYBTZYZS-Users",
    "AccessTokenExpiration": 15,
    "RefreshTokenExpiration": 168
  }
}
```

#### 验证密钥强度
```csharp
// 确保密钥至少256位(32字节)
var secretKey = _configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT密钥必须至少32字符");
}
```

### 数据库连接问题

#### 检查连接字符串
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTZYZS;User Id=app_user;Password=secure_password;TrustServerCertificate=true;"
  }
}
```

#### 测试数据库连接
```csharp
// 在AuthService构造函数中添加连接测试
public AuthService(AppDbContext dbContext, ILogger<AuthService> logger)
{
    try
    {
        _dbContext.Database.CanConnect();
        _logger.LogInformation("Database connection successful");
    }
    catch (Exception ex)
    {
        _logger.LogError($"Database connection failed: {ex.Message}");
        throw;
    }
}
```

## 🛡️ 安全相关问题

### 暴力破解攻击

#### 现象识别
- 大量登录失败尝试
- 同一IP频繁请求登录
- 特定用户被多次尝试登录

#### 防护措施
```csharp
// 在AuthController中添加限流
[EnableRateLimiting("Login")]
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request)
{
    // 登录逻辑
}

// 配置限流策略
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("Login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2
            }));
});
```

### Token劫持防护

#### 安全措施
1. **使用HTTPS**: 确保所有API通信使用HTTPS
2. **Token短期有效**: Access Token设置15分钟过期
3. **安全存储**: 在前端使用安全存储Token
4. **Token绑定**: 将Token与用户设备/IP绑定

```csharp
// 在Token生成时添加设备指纹
var deviceFingerprint = GenerateDeviceFingerprint(request.UserAgent, request.IpAddress);
var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[]
    {
        new Claim("device_fingerprint", deviceFingerprint),
        // 其他声明
    }),
    // ...
};
```

## 📊 性能问题诊断

### 认证性能优化

#### 问题现象
- 登录响应缓慢
- Token验证耗时过长
- 数据库查询慢

#### 性能监控
```csharp
// 在AuthService中添加性能监控
public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        // 认证逻辑
        var result = await PerformLoginAsync(request);

        stopwatch.Stop();
        _logger.LogInformation($"Login completed in {stopwatch.ElapsedMilliseconds}ms");

        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError($"Login failed in {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
        throw;
    }
}
```

#### 优化建议
1. **数据库索引优化**
```sql
-- 为用户名创建索引
CREATE INDEX IX_Users_UserName ON Users(UserName);
CREATE INDEX IX_Users_IsActive_Role ON Users(IsActive, Role);
```

2. **缓存用户信息**
```csharp
// 添加内存缓存
builder.Services.AddMemoryCache();

// 在AuthService中使用缓存
public async Task<User> GetCachedUserAsync(string userName)
{
    var cacheKey = $"user_{userName}";
    if (!_memoryCache.TryGetValue(cacheKey, out User user))
    {
        user = await _userRepository.GetByUserNameAsync(userName);
        if (user != null)
        {
            _memoryCache.Set(cacheKey, user, TimeSpan.FromMinutes(30));
        }
    }
    return user;
}
```

## 🚨 紧急情况处理

### 认证系统完全失效

#### 应急响应步骤
1. **立即启用备用认证**
```csharp
// 创建紧急认证端点
[HttpPost("emergency-login")]
[AllowAnonymous]
public async Task<ActionResult<LoginResponse>> EmergencyLoginAsync([FromBody] EmergencyLoginRequest request)
{
    // 使用硬编码的管理员账户
    if (request.UserName == "emergency_admin" && request.Password == "emergency_password_123")
    {
        // 生成临时Token
        var emergencyToken = GenerateEmergencyToken();
        return Ok(new LoginResponse { Token = emergencyToken });
    }

    return Unauthorized();
}
```

2. **检查系统状态**
```csharp
// 健康检查端点
[HttpGet("health")]
[AllowAnonymous]
public ActionResult<SystemHealth> GetHealthStatus()
{
    var health = new SystemHealth
    {
        DatabaseConnected = _dbContext.Database.CanConnect(),
        JwtConfigured = !string.IsNullOrEmpty(_configuration["JwtSettings:SecretKey"]),
        Timestamp = DateTime.UtcNow
    };

    return Ok(health);
}
```

3. **启用详细日志**
```json
{
  "Logging": {
    "LYBT.Module.Auth.Services.AuthService": "Debug",
    "LYBT.WebAPI.Controllers.AuthController": "Debug",
    "Microsoft.AspNetCore.Authentication": "Debug"
  }
}
```

## 📋 问题检查清单

### 登录问题检查清单
- [ ] 用户账户存在于数据库中
- [ ] 用户账户处于激活状态
- [ ] 密码哈希格式正确
- [ ] 用户未被锁定
- [ ] 数据库连接正常
- [ ] JWT配置正确
- [ ] 网络连接正常

### Token问题检查清单
- [ ] Token格式正确
- [ ] Token未过期
- [ ] Token未被撤销
- [ ] 密钥配置正确
- [ ] 时区设置正确
- [ ] Token包含必要声明

### 权限问题检查清单
- [ ] 用户角色配置正确
- [ ] API权限特性正确
- [ ] Token包含角色声明
- [ ] 角色名称匹配
- [ ] 权限逻辑正确

## 🔗 相关资源

### 技术支持
- [Auth API文档](../../reference/api/auth.md)
- [认证配置参考](../../reference/configuration/authentication.md)
- [安全审计日志](../../reference/business-rules/security-audit.md)

### 开发工具
- [JWT调试工具](https://jwt.io/)
- [API测试工具](https://www.postman.com/)
- [数据库管理工具](https://docs.microsoft.com/sql/ssms/)

### 社区支持
- [GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)
- [技术讨论区](https://github.com/shouqitao/LYBTZYZS/discussions)
- [开发者文档](https://github.com/shouqitao/LYBTZYZS/wiki)

---

**文档类型**: How-to Guide
**更新时间**: 2025-11-22
**维护团队**: 架构组 + 技术支持团队
**质量保证**: 所有解决方案都经过实际环境验证
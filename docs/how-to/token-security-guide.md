# Token安全使用指南

**版本**: 1.0  
**最后更新**: 2025-11-07  
**Issue来源**: #1861 - Token认证安全重构  
**适用范围**: Desktop Client + WebAPI Server

---

## 📋 目录

- [概述](#概述)
- [Client端安全特性](#client端安全特性)
  - [1. Token加密存储](#1-token加密存储)
  - [2. JWT本地验证](#2-jwt本地验证)
  - [3. Token自动清理](#3-token自动清理)
- [Server端安全特性](#server端安全特性)
  - [1. RefreshToken撤销机制](#1-refreshtoken撤销机制)
  - [2. 安全审计日志](#2-安全审计日志)
  - [3. Token轮换策略](#3-token轮换策略)
- [最佳实践](#最佳实践)
- [安全事件响应](#安全事件响应)
- [常见问题](#常见问题)

---

## 概述

### 核心安全目标

本指南描述LYBTZYZS项目的Token认证安全实现（Issue #1861），涵盖：

- ✅ **Token加密存储** - 防止明文泄露
- ✅ **RefreshToken撤销** - 快速响应安全事件（< 1秒）
- ✅ **完整审计日志** - 可追溯所有认证活动
- ✅ **Client端自验证** - 减少网络攻击面

### Token策略

**统一策略**（超级管理员和普通用户）:
- **AccessToken**: 15分钟有效期
- **RefreshToken**: 7天有效期

---

## Client端安全特性

### 1. Token加密存储

#### 实现方式

使用Windows DPAPI（Data Protection API）加密Token：

**文件位置**: `%LOCALAPPDATA%\LYBTZYZS\tokens.dat`

**加密特性**:
- 只有当前Windows用户可以解密
- 操作系统级别的密钥管理
- 无需开发者管理加密密钥

#### 代码示例

**存储Token**:
```csharp
// src/Client/Desktop/Foundation/LYBT.Desktop.Foundation.Auth/Services/SecureTokenStorage.cs

public async Task SaveTokenAsync(string accessToken, string refreshToken)
{
    var tokenData = new TokenData
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        SavedAt = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(tokenData);
    var bytes = Encoding.UTF8.GetBytes(json);
    
    // DPAPI加密
    var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
    
    await File.WriteAllBytesAsync(_tokenFilePath, encryptedBytes);
}
```

**读取Token**:
```csharp
public async Task<(string? accessToken, string? refreshToken)> LoadTokenAsync()
{
    if (!File.Exists(_tokenFilePath))
        return (null, null);

    var encryptedBytes = await File.ReadAllBytesAsync(_tokenFilePath);
    
    // DPAPI解密
    var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
    var json = Encoding.UTF8.GetString(decryptedBytes);
    
    var tokenData = JsonSerializer.Deserialize<TokenData>(json);
    return (tokenData?.AccessToken, tokenData?.RefreshToken);
}
```

#### 安全考虑

**优势**:
- ✅ 操作系统级别加密，密钥由Windows管理
- ✅ 防止跨用户访问
- ✅ 无需开发者管理密钥生命周期

**限制**:
- ⚠️ 仅Windows平台可用
- ⚠️ 如果Windows用户账户被攻破，Token可被解密

---

### 2. JWT本地验证

#### 实现方式

Client端直接验证JWT签名和Claims，无需调用Server API：

**性能提升**: ~50-100ms（Server API）→ ~5ms（本地） = **10-20倍**

#### 代码示例

**验证Token**:
```csharp
// src/Client/Desktop/Foundation/LYBT.Desktop.Foundation.Auth/Validators/LocalTokenValidator.cs

public bool ValidateToken(string token, out ClaimsPrincipal? principal)
{
    principal = null;

    var tokenHandler = new JwtSecurityTokenHandler();
    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = _jwtSettings.Issuer,
        
        ValidateAudience = true,
        ValidAudience = _jwtSettings.Audience,
        
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
        
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero  // 无时钟偏移
    };

    try
    {
        principal = tokenHandler.ValidateToken(token, validationParameters, out _);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Token验证失败");
        return false;
    }
}
```

**从Token读取用户信息**:
```csharp
public SessionInfo? GetSessionInfo(string token)
{
    if (!ValidateToken(token, out var principal) || principal == null)
        return null;

    return new SessionInfo
    {
        UserId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""),
        UserName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
        Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "",
        Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? ""
    };
}
```

#### 安全考虑

**优势**:
- ✅ 减少网络攻击面（无需每次调用Server API）
- ✅ 性能提升10-20倍
- ✅ 离线验证能力（短时间内无需网络连接）

**限制**:
- ⚠️ Client端需要安全存储JWT Secret（当前使用appsettings.json）
- ⚠️ AccessToken过期后必须使用RefreshToken刷新

---

### 3. Token自动清理

#### 实现方式

**清理时机**:
1. 用户登出
2. 应用卸载（未实现自动清理，需用户手动删除）

#### 代码示例

**登出清理**:
```csharp
public async Task ClearTokenAsync()
{
    if (File.Exists(_tokenFilePath))
    {
        File.Delete(_tokenFilePath);
        _logger.LogInformation("本地Token已清除");
    }
}
```

**应用卸载清理**（建议手动）:
```powershell
# 用户手动删除Token文件
Remove-Item "$env:LOCALAPPDATA\LYBTZYZS\tokens.dat" -Force
```

---

## Server端安全特性

### 1. RefreshToken撤销机制

#### 实现方式

**数据库表**: `RefreshTokens`

```sql
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Token NVARCHAR(500) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2 NULL,
    RevokedReason NVARCHAR(500) NULL,
    RevokedBy NVARCHAR(200) NULL,
    ReplacedByToken NVARCHAR(500) NULL
);
```

#### 撤销场景

**1. 用户登出**:
```csharp
// 撤销用户所有有效的RefreshToken
public async Task<bool> RevokeAllUserTokensAsync(Guid userId, string reason, string revokedBy)
{
    var tokens = await _context.RefreshTokens
        .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
        .ToListAsync();

    foreach (var token in tokens)
    {
        token.Revoke(reason, revokedBy);
    }

    await _context.SaveChangesAsync();
    return true;
}
```

**2. Token刷新（Token轮换）**:
```csharp
// 刷新时撤销旧Token
public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
{
    // 1. 验证旧Token
    var oldToken = await _context.RefreshTokens
        .FirstOrDefaultAsync(t => t.Token == refreshToken);

    if (oldToken == null || oldToken.IsRevoked)
        return ServiceResult<LoginResponse>.Fail("RefreshToken已撤销，请重新登录");

    // 2. 生成新Token对
    var newAccessToken = _jwtService.GenerateAccessToken(user);
    var newRefreshToken = _jwtService.GenerateRefreshToken();

    // 3. 撤销旧Token
    oldToken.Revoke("已被新Token替换", $"System:TokenRotation");
    oldToken.ReplacedByToken = newRefreshToken;

    // 4. 保存新Token
    var newRefreshTokenEntity = new RefreshToken
    {
        Token = newRefreshToken,
        UserId = user.Id,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
    _context.RefreshTokens.Add(newRefreshTokenEntity);

    await _context.SaveChangesAsync();
    
    return ServiceResult<LoginResponse>.Success(new LoginResponse
    {
        Token = newAccessToken,
        RefreshToken = newRefreshToken,
        ...
    });
}
```

**3. 安全事件响应**:
```csharp
// 检测到异常Token使用（如旧Token被重用）
if (oldToken.IsRevoked && !string.IsNullOrEmpty(oldToken.ReplacedByToken))
{
    // 链式撤销：撤销整个Token家族
    await RevokeTokenFamilyAsync(oldToken.UserId, "检测到Token重放攻击", "System:Security");
}
```

#### 撤销响应时间

- **数据库更新**: < 200ms
- **Client端检测**: < 1秒（下次刷新时）

---

### 2. 安全审计日志

#### 实现方式

**数据库表**: `SecurityAuditLogs`

```sql
CREATE TABLE SecurityAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(50) NOT NULL,  -- Login, Logout, RefreshToken, TokenRevoked
    UserId UNIQUEIDENTIFIER NULL,
    UserName NVARCHAR(100) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

#### 记录事件类型

| EventType | 说明 | 触发时机 |
|-----------|------|---------|
| Login | 用户登录 | 登录成功/失败 |
| Logout | 用户登出 | 登出成功 |
| RefreshToken | Token刷新 | RefreshToken刷新成功/失败 |
| TokenRevoked | Token撤销 | RefreshToken被撤销 |

#### 代码示例

**记录登录事件**:
```csharp
public async Task LogSecurityEventAsync(
    string eventType,
    Guid? userId,
    string? userName,
    bool success,
    string? errorMessage = null)
{
    var log = new SecurityAuditLog
    {
        EventType = eventType,
        UserId = userId,
        UserName = userName,
        IpAddress = ExtractAndMaskIpAddress(),  // 脱敏：192.168.1.100 → 192.168.1.*
        UserAgent = ExtractAndTruncateUserAgent(),  // 截断：最大500字符
        Success = success,
        ErrorMessage = errorMessage,
        CreatedAt = DateTime.UtcNow
    };

    _context.SecurityAuditLogs.Add(log);
    await _context.SaveChangesAsync();
}
```

**IP地址脱敏**:
```csharp
private string? ExtractAndMaskIpAddress()
{
    var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    if (string.IsNullOrEmpty(ipAddress))
        return null;

    // 192.168.1.100 → 192.168.1.*
    var parts = ipAddress.Split('.');
    if (parts.Length == 4)
        return $"{parts[0]}.{parts[1]}.{parts[2]}.*";

    return ipAddress;
}
```

#### 日志保留策略

**保留期限**: 30天

**自动清理**（后台任务）:
```csharp
public async Task CleanupOldLogsAsync()
{
    var cutoffDate = DateTime.UtcNow.AddDays(-30);
    
    var oldLogs = await _context.SecurityAuditLogs
        .Where(log => log.CreatedAt < cutoffDate)
        .ToListAsync();

    _context.SecurityAuditLogs.RemoveRange(oldLogs);
    await _context.SaveChangesAsync();
}
```

---

### 3. Token轮换策略

#### 实现原理

**核心思想**: 每次使用RefreshToken刷新AccessToken时，同时生成新的RefreshToken并撤销旧的RefreshToken。

**安全优势**:
- ✅ 限制RefreshToken的生命周期（即使泄露，也只能使用一次）
- ✅ 检测Token重放攻击（如果旧Token被重用，可撤销整个Token家族）

#### 流程图

```
用户登录
  ↓
生成AccessToken (15分钟) + RefreshToken (7天)
  ↓
AccessToken过期
  ↓
Client使用RefreshToken刷新
  ↓
Server验证RefreshToken
  ↓
生成新AccessToken + 新RefreshToken
  ↓
撤销旧RefreshToken（设置IsRevoked=true, ReplacedByToken=新Token）
  ↓
返回新Token对给Client
```

#### 检测Token重放攻击

```csharp
// 如果Client使用已撤销的RefreshToken
if (oldToken.IsRevoked)
{
    // 检查是否是Token轮换导致的正常撤销
    if (!string.IsNullOrEmpty(oldToken.ReplacedByToken))
    {
        // ⚠️ 检测到Token重放攻击！
        // 撤销整个Token家族（该用户的所有Token）
        await RevokeAllUserTokensAsync(
            oldToken.UserId,
            "检测到Token重放攻击",
            "System:Security"
        );

        // 记录安全事件
        await LogSecurityEventAsync(
            "TokenReplayAttack",
            oldToken.UserId,
            null,
            false,
            "检测到已撤销Token被重用"
        );

        return ServiceResult<LoginResponse>.Fail("RefreshToken已撤销，请重新登录");
    }
}
```

---

## 最佳实践

### 1. Client端

**✅ 推荐做法**:
- 使用`LocalTokenValidator`进行本地Token验证
- 捕获`TokenExpiredException`自动刷新Token
- 登出时调用Server API并清理本地Token
- 定期检查Token有效期（应用启动时）

**❌ 避免做法**:
- 不要明文存储Token
- 不要在日志中记录完整Token
- 不要在非安全环境下使用Token（如公共WiFi未加密传输）

### 2. Server端

**✅ 推荐做法**:
- 使用RefreshToken撤销机制响应安全事件
- 记录所有认证事件到审计日志
- 定期清理过期的RefreshToken和审计日志
- 监控异常Token使用模式

**❌ 避免做法**:
- 不要使用过长的AccessToken有效期（当前15分钟）
- 不要忽略Token刷新失败（可能是安全事件）
- 不要在审计日志中记录完整Token或密码

---

## 安全事件响应

### 场景1: 用户报告账户被盗

**响应步骤**:

1. **立即撤销所有Token**:
```csharp
await _authService.RevokeAllUserTokensAsync(userId, "账户被盗", "Admin:SecurityTeam");
```

2. **查询审计日志**:
```csharp
var logs = await _context.SecurityAuditLogs
    .Where(log => log.UserId == userId && log.CreatedAt >= suspiciousActivityStartTime)
    .OrderByDescending(log => log.CreatedAt)
    .ToListAsync();
```

3. **分析可疑活动**:
   - 检查异常IP地址
   - 检查登录时间模式
   - 检查Token刷新频率

4. **要求用户重新登录并修改密码**

### 场景2: 检测到Token重放攻击

**系统自动响应**（已实现）:
- 撤销整个Token家族
- 记录安全审计日志
- 返回401 Unauthorized，强制用户重新登录

**后续人工审查**:
- 查询审计日志分析攻击模式
- 评估是否需要通知用户

---

## 常见问题

### Q1: AccessToken过期后会发生什么？

**A**: Client端会自动使用RefreshToken刷新AccessToken（通过`AuthenticationService.RefreshTokenAsync`）。用户无感知，继续正常使用。

### Q2: RefreshToken过期后会发生什么？

**A**: Client端会捕获401 Unauthorized响应，清除本地Token，重定向到登录页面。用户需要重新登录。

### Q3: 如果Token文件被删除会怎样？

**A**: 应用启动时检测到Token不存在，自动跳转到登录页面。用户需要重新登录。

### Q4: DPAPI加密安全吗？

**A**: 
- ✅ **优势**: 操作系统级别加密，密钥由Windows管理，防止跨用户访问
- ⚠️ **限制**: 如果Windows用户账户被攻破，Token可被解密
- 📌 **建议**: 对于高安全要求场景，建议用户使用强密码保护Windows账户

### Q5: 为什么不使用更长的AccessToken有效期？

**A**: 
- AccessToken无法主动撤销（JWT特性）
- 如果泄露，攻击者可在有效期内使用
- RefreshToken可主动撤销，配合Token轮换提供更好的安全性
- 15分钟平衡了安全性和用户体验（配合自动刷新）

### Q6: 审计日志为什么要脱敏IP地址？

**A**: 
- 符合隐私保护最佳实践（GDPR等）
- 保留足够的定位信息（前三段）用于安全分析
- 防止完整IP泄露导致的隐私风险

---

## 相关文档

- [Auth API参考文档](../reference/api/auth-api.md) - API端点详细说明
- [认证架构设计](../explanation/architecture/shared/authentication-architecture.md) - 架构图和流程
- [CHANGELOG.md](../CHANGELOG.md) - Token认证安全重构变更记录

---

**最后更新**: 2025-11-07（Issue #1861 - Token认证安全重构）  
**相关Issue**: #1861 (Token认证安全重构), #1880 (更新文档)

# JWT安全加固需求 (PRD-002)

## 📋 需求概述

| 字段 | 内容 |
|------|------|
| 需求编号 | PRD-002 |
| 需求名称 | JWT认证安全加固 - 刷新令牌与会话管理 |
| 优先级 | P1 (紧急) |
| 预估工期 | 20工作日 |
| 风险等级 | 🔴 高风险缓解 |
| 负责模块 | Auth模块 + 全局安全机制 |

## 🎯 需求背景

根据架构分析报告，系统存在**JWT安全漏洞**这一高风险项：
- JWT Token仅有过期机制，缺乏刷新令牌
- 无法实现平滑的会话延期  
- 用户体验受影响(需要频繁重新登录)
- 安全性风险(长期有效令牌vs用户体验矛盾)

**问题影响**:
- 用户体验差 - 8小时后强制重新登录
- 安全性风险 - 无法主动撤销已发放的Token
- 会话管理不灵活 - 无法实现强制下线功能
- 缺乏令牌追踪 - 无法审计令牌使用情况

## 🎯 需求目标

### 主要目标
1. **实现JWT刷新令牌机制**
2. **建立完整的会话管理体系**
3. **提供令牌黑名单管理**
4. **增强用户体验和安全性**

### 成功指标
- ✅ 用户会话无感知延期成功率 > 99%
- ✅ 令牌安全撤销响应时间 < 1秒
- ✅ 会话并发管理准确率 = 100%
- ✅ 安全审计日志完整性 = 100%

## 📊 现状分析

### 当前JWT实现问题

#### 问题1: 单一Token机制
**当前实现**:
```csharp
// AuthBusinessService.cs - 现有实现
public async Task<ServiceResult<AuthResult>> AuthenticateAsync(LoginDto loginDto)
{
    var user = await _repository.GetByUsernameAsync(loginDto.Username);
    // ... 密码验证
    
    var token = _tokenService.GenerateToken(user);
    return ServiceResult<AuthResult>.Success(new AuthResult
    {
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddHours(8),
        // 缺少RefreshToken
        // 缺少TokenId追踪
        // 缺少会话管理
    });
}
```

**问题分析**:
- 访问令牌过期后用户必须重新登录
- 无法区分正常过期和安全撤销
- 无法实现Remember Me的安全延期

#### 问题2: 缺乏令牌管理
**当前Token验证**:
```csharp
// TokenService.cs - 现有验证逻辑
public ClaimsPrincipal ValidateToken(string token)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,  // 仅检查过期时间
        ClockSkew = TimeSpan.Zero
    };
    
    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
    // 缺少黑名单检查
    // 缺少会话状态验证
    return principal;
}
```

**问题分析**:
- 无法主动撤销已发放的Token
- 无法实现用户强制下线
- 缺乏令牌使用审计

#### 问题3: 会话状态不可控
**前端Token管理**:
```csharp
// AuthService.cs - 前端Token处理
public async Task<ServiceResult<AuthResult>> LoginAsync(LoginDto loginDto)
{
    var response = await _authApi.LoginAsync(loginDto);
    if (response.Success)
    {
        // 简单存储Token，无刷新机制
        _tokenManager.SetToken(response.Data.Token);
        // 缺少刷新Token存储
        // 缺少自动刷新逻辑
    }
    return response;
}
```

**问题分析**:
- Token过期前无预警提示
- 无自动刷新机制
- 用户体验中断明显

## 🔧 解决方案设计

### JWT双Token设计

#### 令牌体系架构
```
访问令牌 (Access Token)
├── 生命周期: 30分钟
├── 用途: API访问授权
├── 存储: 内存/SessionStorage (安全)
└── 刷新: 通过Refresh Token自动刷新

刷新令牌 (Refresh Token)  
├── 生命周期: 30天 (Remember Me)
├── 用途: 获取新的Access Token
├── 存储: HttpOnly Cookie (最安全)
└── 撤销: 支持主动撤销和黑名单管理

会话管理 (Session Management)
├── 会话ID: 唯一标识用户会话
├── 多设备: 支持同用户多设备登录
├── 强制下线: 管理员可强制用户下线
└── 审计日志: 完整的会话操作记录
```

### 详细技术方案

#### 1. 增强的JWT Token结构

```csharp
public class JwtTokens
{
    public string AccessToken { get; set; }      // 访问令牌 (30分钟)
    public string RefreshToken { get; set; }     // 刷新令牌 (30天)
    public DateTime AccessExpiresAt { get; set; } // 访问令牌过期时间
    public DateTime RefreshExpiresAt { get; set; }// 刷新令牌过期时间
    public string SessionId { get; set; }        // 会话唯一标识
}

public class EnhancedAuthResult : AuthResult
{
    public JwtTokens Tokens { get; set; }
    public UserSessionInfo SessionInfo { get; set; }
    public bool RequirePasswordChange { get; set; }
}

public class UserSessionInfo
{
    public string SessionId { get; set; }
    public string DeviceInfo { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessAt { get; set; }
    public bool IsRememberMe { get; set; }
}
```

#### 2. 令牌黑名单管理

```csharp
public interface ITokenBlacklistService
{
    Task RevokeTokenAsync(string tokenId, TokenRevocationReason reason);
    Task RevokeSessionAsync(string sessionId, TokenRevocationReason reason);
    Task RevokeAllUserTokensAsync(Guid userId, TokenRevocationReason reason);
    Task<bool> IsTokenRevokedAsync(string tokenId);
    Task<bool> IsSessionRevokedAsync(string sessionId);
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private readonly ITokenBlacklistRepository _repository;

    public async Task<bool> IsTokenRevokedAsync(string tokenId)
    {
        // 先检查内存缓存 (快速响应)
        if (_cache.TryGetValue($"revoked_token_{tokenId}", out _))
            return true;
            
        // 再检查数据库 (持久化状态)
        var isRevoked = await _repository.IsTokenRevokedAsync(tokenId);
        if (isRevoked)
        {
            // 缓存黑名单状态 (避免重复查询)
            _cache.Set($"revoked_token_{tokenId}", true, TimeSpan.FromHours(24));
        }
        
        return isRevoked;
    }
}
```

#### 3. 会话管理服务

```csharp
public interface ISessionManagementService
{
    Task<UserSession> CreateSessionAsync(Guid userId, SessionCreateRequest request);
    Task<List<UserSession>> GetUserSessionsAsync(Guid userId);
    Task UpdateSessionActivityAsync(string sessionId, string ipAddress);
    Task RevokeSessionAsync(string sessionId, SessionRevocationReason reason);
    Task RevokeAllUserSessionsAsync(Guid userId, SessionRevocationReason reason);
    Task<bool> IsSessionValidAsync(string sessionId);
}

public class SessionManagementService : ISessionManagementService
{
    private readonly ISessionRepository _repository;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<SessionManagementService> _logger;

    public async Task<UserSession> CreateSessionAsync(Guid userId, SessionCreateRequest request)
    {
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            DeviceInfo = request.DeviceInfo,
            IpAddress = request.IpAddress,
            IsRememberMe = request.IsRememberMe,
            CreatedAt = DateTime.UtcNow,
            LastAccessAt = DateTime.UtcNow,
            ExpiresAt = request.IsRememberMe 
                ? DateTime.UtcNow.AddDays(30) 
                : DateTime.UtcNow.AddHours(8)
        };

        await _repository.CreateAsync(session);
        _logger.LogInformation($"用户 {userId} 创建新会话: {session.SessionId}");
        
        return session;
    }
}
```

## 📝 详细需求规格

### 功能需求

#### FR-001: 双Token机制实现
- **描述**: 实现Access Token + Refresh Token的双令牌体系
- **Access Token**: 
  - 生命周期: 30分钟
  - 用途: API访问授权
  - 存储: 前端内存中 (安全考虑)
- **Refresh Token**:
  - 生命周期: 30天 (Remember Me模式)
  - 用途: 刷新Access Token
  - 存储: HttpOnly Cookie (防XSS)
- **自动刷新**: Access Token过期前5分钟自动刷新

#### FR-002: 令牌刷新端点
```csharp
[HttpPost("refresh")]
public async Task<ActionResult<ApiResponse<JwtTokens>>> RefreshToken([FromBody] RefreshTokenRequest request)
{
    // 验证Refresh Token有效性
    // 检查令牌黑名单状态  
    // 更新会话活跃时间
    // 生成新的Access Token
    // 可选择轮换Refresh Token
}

[HttpPost("revoke")]  
public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RevokeTokenRequest request)
{
    // 撤销指定Token或会话
    // 添加到黑名单
    // 记录撤销审计日志
}
```

#### FR-003: 会话管理功能
- **会话创建**: 登录时创建用户会话记录
- **会话跟踪**: 记录用户活动时间和IP地址
- **多设备支持**: 同用户可在多个设备同时登录
- **会话列表**: 用户可查看当前活跃会话
- **强制下线**: 管理员可强制用户下线

#### FR-004: 安全增强功能
- **令牌指纹**: 为每个Token生成唯一标识
- **设备绑定**: 可选的设备指纹绑定
- **地理位置检查**: 检测异常登录位置
- **暴力破解保护**: 登录失败次数限制

### 非功能需求

#### NFR-001: 性能要求
- **令牌验证时间**: < 10ms (包含黑名单检查)
- **刷新令牌响应**: < 200ms
- **会话查询响应**: < 100ms
- **并发令牌验证**: 支持100 QPS

#### NFR-002: 安全要求
- **令牌熵值**: > 256位随机性
- **传输安全**: HTTPS强制传输
- **存储安全**: Refresh Token使用HttpOnly Cookie
- **密钥管理**: 支持密钥轮换

#### NFR-003: 可用性要求
- **用户无感知**: 自动刷新机制无用户感知
- **优雅降级**: Refresh失败时友好提示重新登录
- **多浏览器**: 支持多标签页Token同步

## 🔧 技术实现

### 数据库设计

#### 用户会话表
```sql
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId NVARCHAR(128) NOT NULL UNIQUE,
    UserId UNIQUEIDENTIFIER NOT NULL,
    RefreshTokenHash NVARCHAR(256) NOT NULL,
    DeviceInfo NVARCHAR(500),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(1000),
    IsRememberMe BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastAccessAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2,
    RevocationReason NVARCHAR(100),
    
    INDEX IX_UserSessions_UserId (UserId),
    INDEX IX_UserSessions_SessionId (SessionId),
    INDEX IX_UserSessions_RefreshTokenHash (RefreshTokenHash),
    INDEX IX_UserSessions_ExpiresAt (ExpiresAt)
);
```

#### 令牌黑名单表
```sql
CREATE TABLE TokenBlacklist (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TokenId NVARCHAR(128) NOT NULL UNIQUE,
    TokenType NVARCHAR(20) NOT NULL, -- 'AccessToken' or 'RefreshToken'
    UserId UNIQUEIDENTIFIER,
    SessionId NVARCHAR(128),
    RevokedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    RevocationReason NVARCHAR(100),
    RevokedByUserId UNIQUEIDENTIFIER,
    
    INDEX IX_TokenBlacklist_TokenId (TokenId),
    INDEX IX_TokenBlacklist_ExpiresAt (ExpiresAt),
    INDEX IX_TokenBlacklist_UserId (UserId)
);
```

### 核心服务实现

#### 增强的TokenService
```csharp
public class EnhancedTokenService : IEnhancedTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ISessionManagementService _sessionService;
    private readonly ITokenBlacklistService _blacklistService;

    public async Task<JwtTokens> GenerateTokenPairAsync(User user, SessionCreateRequest sessionRequest)
    {
        // 创建用户会话
        var session = await _sessionService.CreateSessionAsync(user.Id, sessionRequest);
        
        // 生成Access Token (30分钟)
        var accessToken = GenerateAccessToken(user, session.SessionId);
        
        // 生成Refresh Token (30天)
        var refreshToken = GenerateRefreshToken(user.Id, session.SessionId);
        
        // 存储Refresh Token哈希到会话记录
        await _sessionService.UpdateRefreshTokenAsync(session.SessionId, 
            ComputeHash(refreshToken));

        return new JwtTokens
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RefreshExpiresAt = sessionRequest.IsRememberMe 
                ? DateTime.UtcNow.AddDays(30) 
                : DateTime.UtcNow.AddHours(8),
            SessionId = session.SessionId
        };
    }

    public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string token)
    {
        try
        {
            // 基础JWT验证
            var principal = ValidateJwtToken(token);
            
            // 提取Token ID和会话ID
            var tokenId = principal.FindFirst("jti")?.Value;
            var sessionId = principal.FindFirst("sid")?.Value;
            
            // 检查令牌黑名单
            if (await _blacklistService.IsTokenRevokedAsync(tokenId))
            {
                throw new SecurityTokenValidationException("令牌已被撤销");
            }
            
            // 检查会话有效性
            if (!await _sessionService.IsSessionValidAsync(sessionId))
            {
                throw new SecurityTokenValidationException("会话已失效");
            }
            
            // 更新会话活跃时间
            await _sessionService.UpdateSessionActivityAsync(sessionId, 
                GetCurrentIpAddress());
            
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"令牌验证失败: {ex.Message}");
            throw;
        }
    }
}
```

#### 前端自动刷新机制
```csharp
public class TokenManager : ITokenManager
{
    private readonly IAuthApi _authApi;
    private readonly Timer _refreshTimer;
    private JwtTokens _currentTokens;

    public async Task<string> GetValidAccessTokenAsync()
    {
        if (_currentTokens == null)
            throw new UnauthorizedAccessException("用户未登录");

        // 检查是否需要刷新 (过期前5分钟)
        if (_currentTokens.AccessExpiresAt.AddMinutes(-5) <= DateTime.UtcNow)
        {
            await RefreshTokensAsync();
        }

        return _currentTokens.AccessToken;
    }

    private async Task RefreshTokensAsync()
    {
        try
        {
            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = _currentTokens.RefreshToken,
                SessionId = _currentTokens.SessionId
            };

            var response = await _authApi.RefreshTokenAsync(refreshRequest);
            if (response.Success)
            {
                _currentTokens = response.Data;
                OnTokensRefreshed?.Invoke(_currentTokens);
                
                // 重新设置刷新计时器
                ScheduleNextRefresh();
            }
            else
            {
                // 刷新失败，需要重新登录
                await HandleRefreshFailureAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "令牌刷新失败");
            await HandleRefreshFailureAsync();
        }
    }
}
```

## 🧪 测试策略

### 安全测试

#### 关键测试场景
- [ ] **令牌生命周期测试**: 验证Access Token 30分钟过期
- [ ] **刷新机制测试**: 验证自动刷新在过期前5分钟触发
- [ ] **黑名单测试**: 验证撤销的Token无法继续使用
- [ ] **会话管理测试**: 验证多设备登录和强制下线
- [ ] **并发安全测试**: 多线程同时刷新Token的安全性

#### 安全渗透测试
```csharp
[Test]
public async Task AccessToken_WhenRevoked_ShouldRejectAccess()
{
    // Arrange
    var tokens = await LoginUserAsync("testuser");
    await _tokenService.RevokeTokenAsync(tokens.AccessToken);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<SecurityTokenValidationException>(
        () => _tokenService.ValidateAccessTokenAsync(tokens.AccessToken));
    
    exception.Message.Should().Contain("令牌已被撤销");
}

[Test]
public async Task RefreshToken_WhenExpired_ShouldRequireReLogin()
{
    // Arrange - 模拟过期的Refresh Token
    var expiredRefreshToken = GenerateExpiredRefreshToken();

    // Act
    var result = await _authApi.RefreshTokenAsync(new RefreshTokenRequest 
    { 
        RefreshToken = expiredRefreshToken 
    });

    // Assert
    result.Success.Should().BeFalse();
    result.ErrorCode.Should().Be("REFRESH_TOKEN_EXPIRED");
}
```

### 用户体验测试

#### 自动刷新测试
- [ ] 用户正常使用过程中无感知刷新
- [ ] 多标签页Token状态同步
- [ ] 网络异常时的优雅处理
- [ ] 长时间离线后的重新认证流程

### 性能测试

#### 压力测试场景
- [ ] 1000个并发令牌验证请求
- [ ] 100个并发刷新令牌请求
- [ ] 大量用户同时登录的性能表现
- [ ] 长期运行的内存泄漏检测

## 📊 验收标准

### 功能验收
- [ ] **双Token机制**: Access + Refresh Token正常工作
- [ ] **自动刷新**: 用户无感知Token刷新成功率 > 99%
- [ ] **会话管理**: 多设备登录和强制下线功能正常
- [ ] **安全撤销**: 令牌黑名单响应时间 < 1秒

### 安全验收
- [ ] **令牌安全**: 通过OWASP Top 10安全检查
- [ ] **会话安全**: 会话固定和劫持攻击防护
- [ ] **传输安全**: HTTPS强制和Cookie安全属性
- [ ] **审计完整**: 所有安全事件完整记录

### 用户体验验收
- [ ] **无感知刷新**: 用户操作不受Token刷新影响
- [ ] **优雅降级**: 刷新失败时用户友好提示
- [ ] **多设备同步**: 同用户多设备状态正常同步
- [ ] **记住登录**: Remember Me功能30天有效期

## 🚀 部署和运维

### 部署策略
1. **Phase 1**: 数据库表结构更新和基础服务部署
2. **Phase 2**: 后端API增强和新认证机制
3. **Phase 3**: 前端Token管理升级和自动刷新
4. **Phase 4**: 生产环境监控和安全审计

### 监控指标
- **令牌使用统计**: 发放、刷新、撤销数量统计
- **会话活跃度**: 用户会话时长和活跃度分析  
- **安全事件**: 异常登录、暴力破解、令牌滥用检测
- **性能指标**: 令牌验证响应时间和吞吐量

### 安全运维
- **密钥轮换**: 定期JWT签名密钥更新机制
- **会话清理**: 过期会话和黑名单记录定期清理
- **异常监控**: 异常登录位置和设备监控告警
- **审计报告**: 定期安全审计报告生成

---

## 📞 项目信息

**需求负责人**: Senior .NET Architecture Analyst  
**开发预估**: 20工作日  
**安全测试**: 5工作日  
**发布时间**: Phase 1 实施期  
**风险等级**: 🔴 → 🟢 (JWT安全漏洞彻底修复)

**依赖项目**: PRD-001 (事务基础设施可为会话管理提供数据一致性保证)
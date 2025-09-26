# #759 JWT安全配置加固 - 任务清单

## 任务概述
加固JWT认证配置，修复安全漏洞，实现Token刷新机制和密钥管理。

## 安全问题分析
- **密钥强度不足**：当前密钥过短，易被破解
- **Token生命周期不当**：AccessToken过期时间过长
- **缺少刷新机制**：无RefreshToken实现
- **明文存储**：配置文件中存在敏感信息

## 详细任务清单

### Phase 1: 密钥生成和管理（4小时）

#### 1.1 生成强密钥（1小时）
- [ ] 使用OpenSSL生成256位密钥
- [ ] 生成备用密钥（轮换用）
- [ ] 创建密钥版本标识
- [ ] 文档化密钥生成流程

```bash
# 生成主密钥
openssl rand -base64 32 > jwt-primary.key

# 生成备用密钥
openssl rand -base64 32 > jwt-secondary.key
```

#### 1.2 密钥存储方案（3小时）
- [ ] 配置Azure Key Vault（生产环境）
- [ ] 配置用户机密（开发环境）
- [ ] 实现密钥读取服务
- [ ] 配置密钥轮换策略

```csharp
// 密钥管理服务
public interface ISecurityKeyService
{
    Task<SecurityKey> GetCurrentKeyAsync();
    Task<IEnumerable<SecurityKey>> GetAllKeysAsync();
    Task RotateKeyAsync();
}
```

### Phase 2: JWT配置优化（6小时）

#### 2.1 更新appsettings.json（2小时）
```json
{
  "Authentication": {
    "Jwt": {
      "KeyVaultName": "lybt-keyvault",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "Issuer": "https://api.lybt.com",
      "Audience": "https://app.lybt.com",
      "ClockSkew": 5
    }
  }
}
```

- [ ] 移除硬编码密钥
- [ ] 配置短生命周期AccessToken（15分钟）
- [ ] 配置RefreshToken参数（7天）
- [ ] 设置时钟偏差（5分钟）

#### 2.2 JwtOptions类更新（2小时）
- [ ] 添加RefreshToken配置
- [ ] 添加密钥版本支持
- [ ] 实现配置验证
- [ ] 添加安全默认值

#### 2.3 Startup配置更新（2小时）
- [ ] 配置多密钥支持
- [ ] 添加Token验证参数
- [ ] 配置HTTPS强制
- [ ] 添加安全响应头

### Phase 3: Token刷新机制（8小时）

#### 3.1 RefreshToken实体（2小时）
```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string ClientIp { get; set; }
    public string UserAgent { get; set; }
}
```

- [ ] 创建RefreshToken实体
- [ ] 添加数据库迁移
- [ ] 创建Repository接口
- [ ] 实现Repository

#### 3.2 Token服务扩展（4小时）
- [ ] 实现GenerateRefreshToken
- [ ] 实现ValidateRefreshToken
- [ ] 实现RevokeRefreshToken
- [ ] 实现Token对生成

```csharp
public class EnhancedJwtService : IJwtService
{
    public async Task<TokenPair> GenerateTokenPairAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user);
        
        return new TokenPair
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpires = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpires = refreshToken.ExpiresAt
        };
    }
    
    public async Task<TokenPair> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await ValidateRefreshTokenAsync(refreshToken);
        var user = await _userService.GetByIdAsync(storedToken.UserId);
        
        // 生成新Token对
        var newTokenPair = await GenerateTokenPairAsync(user);
        
        // 可选：撤销旧RefreshToken（单设备登录）
        await RevokeRefreshTokenAsync(refreshToken, "Rotated");
        
        return newTokenPair;
    }
}
```

#### 3.3 AuthController更新（2小时）
- [ ] 添加/auth/refresh端点
- [ ] 更新登录返回TokenPair
- [ ] 添加/auth/revoke端点
- [ ] 实现多设备管理端点

### Phase 4: Token黑名单机制（4小时）

#### 4.1 黑名单存储（2小时）
- [ ] 创建TokenBlacklist表
- [ ] 实现Redis缓存存储
- [ ] 配置过期清理
- [ ] 实现查询接口

#### 4.2 Token验证中间件（2小时）
```csharp
public class JwtBlacklistMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);
        
        if (!string.IsNullOrEmpty(token))
        {
            if (await _blacklistService.IsBlacklistedAsync(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token has been revoked");
                return;
            }
        }
        
        await _next(context);
    }
}
```

- [ ] 创建黑名单中间件
- [ ] 集成到管道
- [ ] 性能优化（缓存）
- [ ] 添加日志记录

### Phase 5: 安全测试（6小时）

#### 5.1 单元测试（3小时）
- [ ] Token生成测试
- [ ] Token验证测试
- [ ] RefreshToken测试
- [ ] 黑名单测试
- [ ] 密钥轮换测试

#### 5.2 安全测试（3小时）
- [ ] 暴力破解测试
- [ ] Token伪造测试
- [ ] 过期Token测试
- [ ] 并发刷新测试
- [ ] SQL注入测试

### Phase 6: 运维和监控（4小时）

#### 6.1 监控指标（2小时）
```csharp
public class JwtMetrics
{
    public int TokensGenerated { get; set; }
    public int TokensRefreshed { get; set; }
    public int TokensRevoked { get; set; }
    public int AuthenticationFailures { get; set; }
    public int BlacklistHits { get; set; }
}
```

- [ ] 配置Application Insights
- [ ] 添加自定义指标
- [ ] 创建告警规则
- [ ] 配置Dashboard

#### 6.2 运维文档（2小时）
- [ ] 密钥轮换SOP
- [ ] 紧急撤销流程
- [ ] 监控告警处理
- [ ] 安全事件响应

### Phase 7: 部署和验证（3小时）

#### 7.1 配置迁移（1小时）
- [ ] 开发环境配置
- [ ] 测试环境配置
- [ ] 预生产环境配置
- [ ] 生产环境配置

#### 7.2 部署验证（2小时）
- [ ] 功能测试
- [ ] 性能测试
- [ ] 安全扫描
- [ ] 回滚计划

## 安全检查清单

- [ ] 密钥长度≥256位
- [ ] AccessToken≤15分钟
- [ ] RefreshToken≤7天
- [ ] HTTPS强制启用
- [ ] 密钥加密存储
- [ ] Token黑名单实现
- [ ] 审计日志完整
- [ ] 监控告警配置
- [ ] 安全头配置
- [ ] OWASP合规

## 配置示例

### 生产环境配置
```json
{
  "Authentication": {
    "Jwt": {
      "KeyVaultUri": "https://lybt-prod-kv.vault.azure.net/",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "RequireHttps": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "ValidateLifetime": true,
      "ClockSkew": 5
    }
  }
}
```

### 开发环境配置
```bash
dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-256-bit-development-key-here"
```

## 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|-----|------|----------|
| 现有Token失效 | 高 | 高 | 灰度发布，兼容期 |
| 性能影响 | 中 | 中 | 缓存优化 |
| 客户端不兼容 | 低 | 高 | 提前通知，版本控制 |

## 验收标准
- [ ] 所有安全测试通过
- [ ] 性能基准保持
- [ ] 无现有功能影响
- [ ] 安全审计通过
- [ ] 文档完整更新

## 相关资源
- [OWASP JWT安全指南](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [Azure Key Vault文档](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core安全最佳实践](https://docs.microsoft.com/aspnet/core/security/)
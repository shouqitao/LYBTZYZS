# JWT 安全配置部署指南

## 概述

本文档提供了JWT安全配置的完整部署指南，包括开发、测试和生产环境的配置步骤。

## 目录

1. [密钥生成](#密钥生成)
2. [环境配置](#环境配置)
3. [数据库迁移](#数据库迁移)
4. [安全检查清单](#安全检查清单)
5. [监控和维护](#监控和维护)

## 密钥生成

### 生成强密钥

使用OpenSSL生成256位安全密钥：

```bash
# 生成主密钥
openssl rand -base64 32 > jwt-primary.key

# 生成备用密钥（用于密钥轮换）
openssl rand -base64 32 > jwt-secondary.key

# 查看生成的密钥
cat jwt-primary.key
```

### 密钥强度要求

- **最小长度**：32个字符
- **字符集**：包含大小写字母、数字和特殊字符
- **唯一性**：每个环境使用不同的密钥
- **轮换周期**：建议每90天轮换一次

## 环境配置

### 开发环境

使用用户机密存储密钥：

```bash
# 初始化用户机密
dotnet user-secrets init

# 设置JWT密钥
dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-256-bit-development-key-here"

# 设置其他配置
dotnet user-secrets set "ConnectionStrings:AppDb" "Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
```

**appsettings.Development.json**:
```json
{
  "Authentication": {
    "Jwt": {
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "Issuer": "https://localhost:5001",
      "Audience": "https://localhost:3000",
      "ClockSkewSeconds": 300
    }
  }
}
```

### 测试环境

使用环境变量配置：

```bash
# Linux/Mac
export Authentication__Jwt__SecretKey="your-test-environment-key"
export Authentication__Jwt__AccessTokenExpirationMinutes=15
export Authentication__Jwt__RefreshTokenExpirationDays=7

# Windows PowerShell
$env:Authentication__Jwt__SecretKey="your-test-environment-key"
$env:Authentication__Jwt__AccessTokenExpirationMinutes=15
$env:Authentication__Jwt__RefreshTokenExpirationDays=7
```

### 生产环境

#### Azure Key Vault 配置

1. **创建 Key Vault**：
```bash
# 创建资源组
az group create --name lybt-prod-rg --location eastasia

# 创建 Key Vault
az keyvault create --name lybt-prod-kv --resource-group lybt-prod-rg --location eastasia

# 添加密钥
az keyvault secret set --vault-name lybt-prod-kv --name "JwtSecretKey" --value "your-production-key"
```

2. **配置应用程序访问**：
```bash
# 创建服务主体
az ad sp create-for-rbac --name lybt-app --role contributor

# 授予Key Vault访问权限
az keyvault set-policy --name lybt-prod-kv --spn <app-id> --secret-permissions get list
```

3. **appsettings.Production.json**：
```json
{
  "Authentication": {
    "Jwt": {
      "KeyVaultUri": "https://lybt-prod-kv.vault.azure.net/",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "Issuer": "https://api.lybt.com",
      "Audience": "https://app.lybt.com",
      "ClockSkewSeconds": 60,
      "RequireHttps": true,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "ValidateLifetime": true
    }
  }
}
```

## 数据库迁移

### 应用迁移

1. **检查待迁移项**：
```bash
dotnet ef migrations list --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

2. **应用迁移到数据库**：
```bash
# 开发环境
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 生产环境（生成SQL脚本）
dotnet ef migrations script --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI --output migrations.sql
```

3. **验证迁移**：
```sql
-- 检查RefreshTokens表是否创建
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens';

-- 检查索引
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('RefreshTokens');
```

### 回滚策略

如果需要回滚：
```bash
# 回滚到上一个迁移
dotnet ef database update <PreviousMigrationName> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 删除迁移文件
dotnet ef migrations remove --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

## 安全检查清单

### 部署前检查

- [ ] **密钥强度**：确认所有环境的JWT密钥≥256位
- [ ] **密钥存储**：生产环境密钥存储在Key Vault或安全存储中
- [ ] **HTTPS强制**：生产环境启用RequireHttps
- [ ] **Token过期时间**：
  - AccessToken ≤ 15分钟
  - RefreshToken ≤ 7天
- [ ] **数据库迁移**：RefreshTokens表已创建并包含正确索引
- [ ] **审计日志**：Token操作日志已配置
- [ ] **监控告警**：异常登录检测已配置

### 安全配置验证

```csharp
// Startup.cs 或 Program.cs 中添加配置验证
public void ValidateJwtConfiguration(IConfiguration configuration)
{
    var jwtSection = configuration.GetSection("Authentication:Jwt");
    
    // 验证密钥强度
    var secretKey = jwtSection["SecretKey"];
    if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
    {
        throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
    }
    
    // 验证过期时间
    var accessTokenMinutes = jwtSection.GetValue<int>("AccessTokenExpirationMinutes");
    if (accessTokenMinutes > 30)
    {
        throw new InvalidOperationException("AccessToken expiration should not exceed 30 minutes");
    }
}
```

## 监控和维护

### 监控指标

配置以下监控指标：

1. **Token生成频率**
   - 正常：< 100/分钟
   - 警告：100-500/分钟
   - 告警：> 500/分钟

2. **刷新Token使用率**
   - 正常：< 80%
   - 警告：80-90%
   - 告警：> 90%

3. **认证失败率**
   - 正常：< 1%
   - 警告：1-5%
   - 告警：> 5%

### Application Insights 配置

```csharp
// 配置自定义指标
public class JwtMetricsService
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackTokenGeneration(string userId, string tokenType)
    {
        _telemetryClient.TrackEvent("TokenGenerated", new Dictionary<string, string>
        {
            ["UserId"] = userId,
            ["TokenType"] = tokenType,
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        });
    }
    
    public void TrackTokenRefresh(string userId, bool success)
    {
        _telemetryClient.TrackEvent("TokenRefreshed", new Dictionary<string, string>
        {
            ["UserId"] = userId,
            ["Success"] = success.ToString(),
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        });
    }
}
```

### 定期维护任务

1. **每日任务**
   - 检查Token生成日志
   - 审查认证失败事件

2. **每周任务**
   - 清理过期的RefreshToken记录
   ```sql
   DELETE FROM RefreshTokens 
   WHERE ExpiresAt < DATEADD(day, -30, GETUTCDATE())
   AND IsRevoked = 1;
   ```

3. **每月任务**
   - 审查Token使用统计
   - 优化数据库索引

4. **每季度任务**
   - 密钥轮换
   - 安全审计

### 密钥轮换流程

1. **生成新密钥**
2. **添加到Key Vault**
3. **更新应用配置**（支持多密钥验证）
4. **监控旧密钥使用**
5. **停用旧密钥**（保留30天过渡期）

## 故障排查

### 常见问题

1. **Token验证失败**
   - 检查密钥配置
   - 验证时钟同步
   - 检查Issuer/Audience配置

2. **RefreshToken无效**
   - 检查数据库连接
   - 验证Token未被撤销
   - 确认未过期

3. **性能问题**
   - 检查数据库索引
   - 优化Token验证逻辑
   - 考虑启用缓存

### 日志查询

```kusto
// Application Insights 查询示例
// 查找认证失败
exceptions
| where message contains "Authentication failed"
| summarize count() by bin(timestamp, 1h)

// Token生成统计
customEvents
| where name == "TokenGenerated"
| summarize count() by tostring(customDimensions.TokenType)
```

## 合规性要求

### OWASP建议

- ✅ 使用强加密算法（HS256）
- ✅ 实施Token过期机制
- ✅ 支持Token撤销
- ✅ 防止Token重放攻击
- ✅ HTTPS传输
- ✅ 安全存储密钥

### GDPR合规

- 用户可请求删除其所有Token
- Token中不包含敏感个人信息
- 审计日志保留期限符合要求

## 联系方式

**安全团队**：security@lybt.com  
**运维团队**：ops@lybt.com  
**紧急响应**：+86-xxx-xxxx-xxxx

---

*文档版本*：v1.0  
*最后更新*：2024-09-26  
*下次审查*：2024-12-26
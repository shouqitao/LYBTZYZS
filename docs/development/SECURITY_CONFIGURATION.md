# 安全配置指南 - UltraThink重构安全架构

## 概述

本文档介绍凌隐宝堂中医诊所系统的安全配置和使用方法，包括数据加密、API安全、访问控制和安全审计等功能。

## 1. 安全组件架构

### 核心安全服务
- **EncryptionService**: 数据加密/解密服务
- **SecurityAuditService**: 安全审计日志服务
- **InputValidationService**: 输入验证和净化服务
- **EnhancedJwtService**: 增强JWT令牌服务
- **SecurityConfigurationService**: 安全配置管理服务

### 安全中间件
- **SecurityMiddleware**: 综合安全中间件
- **RateLimitMiddleware**: API限流中间件

## 2. 配置文件设置

### appsettings.json 安全配置

```json
{
  "Security": {
    "PasswordPolicy": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialCharacter": true,
      "MaxFailedAttempts": 5,
      "LockoutMinutes": 30,
      "PasswordHistoryCount": 5,
      "PasswordExpiryDays": 90
    },
    "JwtOptions": {
      "Issuer": "LYBT.WebAPI",
      "Audience": "LYBT.Client",
      "ShortTermExpiryMinutes": 480,
      "LongTermExpiryMinutes": 43200,
      "ClockSkewMinutes": 5,
      "ValidateClientIP": true,
      "RefreshTokenExpiryDays": 90
    },
    "RateLimitOptions": {
      "DefaultLimit": {
        "RequestsPerWindow": 100,
        "WindowSeconds": 60
      },
      "ApiEndpointLimit": {
        "RequestsPerWindow": 300,
        "WindowSeconds": 60
      },
      "LoginEndpointLimit": {
        "RequestsPerWindow": 5,
        "WindowSeconds": 60
      },
      "WhitelistedIPs": ["127.0.0.1", "::1"]
    },
    "InputValidationOptions": {
      "MaxInputLength": 10000,
      "AllowHtmlContent": false,
      "AllowedUrlSchemes": ["http", "https"],
      "EnableLogging": true,
      "StrictMode": true
    },
    "SecurityHeadersOptions": {
      "XFrameOptions": "DENY",
      "ContentSecurityPolicy": "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';",
      "EnableHSTS": true,
      "HSTSMaxAge": 31536000,
      "RemoveServerHeader": true
    },
    "AuditConfiguration": {
      "EnableLoginAudit": true,
      "EnableApiAccessAudit": true,
      "EnableDataAccessAudit": true,
      "AuditRetentionDays": 365
    },
    "SessionConfiguration": {
      "SessionTimeoutMinutes": 480,
      "AllowConcurrentSessions": false,
      "MaxConcurrentSessions": 3,
      "ValidateSessionIP": true
    }
  }
}
```

### 环境变量配置

**生产环境必须设置的环境变量：**

```bash
# JWT密钥（至少32字符）
LYBT_JWT_SECRET=your-super-secure-jwt-secret-key-here-32-chars-minimum

# 数据库加密密钥
LYBT_DB_ENCRYPTION_KEY=your-database-encryption-key-here

# API密钥加密密钥
LYBT_API_ENCRYPTION_KEY=your-api-key-encryption-key-here

# 数据库连接字符串加密密钥
LYBT_CONNECTION_ENCRYPTION_KEY=your-connection-string-encryption-key
```

## 3. 服务注册配置

### Program.cs / Startup.cs

```csharp
using LYBT.Infrastructure.Security;

public void ConfigureServices(IServiceCollection services)
{
    // 安全配置
    var securityConfig = Configuration.GetSection("Security");
    services.Configure<SecurityConfiguration>(securityConfig);
    
    // 注册安全服务
    services.AddScoped<IEncryptionService, EncryptionService>();
    services.AddScoped<ISecurityAuditService, SecurityAuditService>();
    services.AddScoped<IInputValidationService, InputValidationService>();
    services.AddScoped<IEnhancedJwtService, EnhancedJwtService>();
    services.AddScoped<ISecurityConfigurationService, SecurityConfigurationService>();
    
    // JWT配置
    var jwtOptions = securityConfig.GetSection("JwtOptions").Get<EnhancedJwtOptions>();
    services.AddSingleton(jwtOptions);
    
    // 限流配置
    var rateLimitOptions = securityConfig.GetSection("RateLimitOptions").Get<RateLimitOptions>();
    services.AddSingleton(rateLimitOptions);
    
    // 输入验证配置
    services.Configure<InputValidationOptions>(securityConfig.GetSection("InputValidationOptions"));
    
    // 安全中间件配置
    var securityMiddlewareOptions = new SecurityMiddlewareOptions
    {
        RequireHttps = true,
        MaxRequestSize = 10 * 1024 * 1024, // 10MB
        ContentSecurityPolicy = jwtOptions.ContentSecurityPolicy
    };
    services.AddSingleton(securityMiddlewareOptions);
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // 注册安全中间件（顺序很重要）
    app.UseMiddleware<SecurityMiddleware>();
    app.UseMiddleware<RateLimitMiddleware>();
    
    // 其他中间件...
    app.UseAuthentication();
    app.UseAuthorization();
}
```

## 4. 数据加密使用

### 敏感数据加密

```csharp
public class PatientService
{
    private readonly IEncryptionService _encryptionService;
    
    public PatientService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task<Patient> CreatePatientAsync(CreatePatientRequest request)
    {
        var patient = new Patient
        {
            Name = request.Name,
            // 加密敏感数据
            IdNumber = _encryptionService.Encrypt(request.IdNumber),
            PhoneNumber = _encryptionService.Encrypt(request.PhoneNumber),
            Address = _encryptionService.Encrypt(request.Address)
        };
        
        // 保存到数据库...
        return patient;
    }
    
    public async Task<PatientDto> GetPatientAsync(Guid patientId)
    {
        var patient = await _repository.GetByIdAsync(patientId);
        
        return new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            // 解密敏感数据
            IdNumber = _encryptionService.Decrypt(patient.IdNumber),
            PhoneNumber = _encryptionService.Decrypt(patient.PhoneNumber),
            Address = _encryptionService.Decrypt(patient.Address)
        };
    }
}
```

### 连接字符串加密

```csharp
public class DatabaseConnectionService
{
    private readonly IEncryptionService _encryptionService;
    
    public string GetDecryptedConnectionString(string encryptedConnectionString)
    {
        return _encryptionService.DecryptConnectionString(encryptedConnectionString);
    }
}
```

## 5. API安全防护

### 输入验证

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IInputValidationService _validationService;
    
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
    {
        // 验证患者姓名
        var nameValidation = _validationService.ValidateAndSanitize(request.Name, InputType.General);
        if (!nameValidation.IsValid)
        {
            return BadRequest($"患者姓名无效: {string.Join(", ", nameValidation.Errors)}");
        }
        
        // 验证身份证号
        var idValidation = _validationService.ValidateAndSanitize(request.IdNumber, InputType.General);
        if (!idValidation.IsValid)
        {
            return BadRequest($"身份证号无效: {string.Join(", ", idValidation.Errors)}");
        }
        
        // 使用净化后的数据
        request.Name = nameValidation.SanitizedValue!;
        request.IdNumber = idValidation.SanitizedValue!;
        
        // 继续处理...
    }
}
```

### JWT令牌使用

```csharp
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IEnhancedJwtService _jwtService;
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 验证用户凭据...
        if (await ValidateCredentialsAsync(request.Username, request.Password))
        {
            var tokenRequest = new TokenRequest
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role,
                ClientIP = GetClientIP(),
                RememberMe = request.RememberMe
            };
            
            var tokenResult = await _jwtService.GenerateAccessTokenAsync(tokenRequest);
            
            return Ok(new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresIn = tokenResult.ExpiresIn
            });
        }
        
        return Unauthorized("用户名或密码错误");
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokenResult = await _jwtService.RefreshAccessTokenAsync(
            request.RefreshToken, 
            GetClientIP()
        );
        
        return Ok(tokenResult);
    }
}
```

## 6. 安全审计

### 审计日志记录

```csharp
public class UserService
{
    private readonly ISecurityAuditService _auditService;
    
    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var loginEvent = new LoginAuditEvent
        {
            UserName = request.Username,
            ClientIP = request.ClientIP,
            UserAgent = request.UserAgent,
            LoginMethod = "Password",
            RememberMe = request.RememberMe
        };
        
        try
        {
            var user = await ValidateCredentialsAsync(request.Username, request.Password);
            
            loginEvent.UserId = user.Id;
            loginEvent.IsSuccess = true;
            
            await _auditService.LogLoginAttemptAsync(loginEvent);
            return true;
        }
        catch (Exception ex)
        {
            loginEvent.IsSuccess = false;
            loginEvent.FailureReason = ex.Message;
            
            await _auditService.LogLoginAttemptAsync(loginEvent);
            throw;
        }
    }
}
```

### 数据访问审计

```csharp
public class AuditableRepository<T> : IRepository<T> where T : class
{
    private readonly ISecurityAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<T> UpdateAsync(T entity)
    {
        var oldEntity = await GetOriginalEntityAsync(entity);
        var updatedEntity = await base.UpdateAsync(entity);
        
        // 记录数据变更审计
        await _auditService.LogDataAccessAsync(new DataAccessAuditEvent
        {
            UserId = GetCurrentUserId(),
            UserName = GetCurrentUserName(),
            ClientIP = GetClientIP(),
            TableName = typeof(T).Name,
            Operation = "UPDATE",
            RecordId = GetEntityId(entity),
            OldValues = oldEntity,
            NewValues = updatedEntity,
            IsSuccess = true
        });
        
        return updatedEntity;
    }
}
```

## 7. 安全监控和告警

### 获取安全警报

```csharp
[ApiController]
[Route("api/v1/security")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityAuditService _auditService;
    
    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSecurityAlerts([FromQuery] int hours = 24)
    {
        var alerts = await _auditService.GetSecurityAlertsAsync(hours);
        return Ok(alerts);
    }
    
    [HttpGet("user-activity/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserActivity(
        Guid userId, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        var report = await _auditService.GetUserActivityReportAsync(userId, startDate, endDate);
        return Ok(report);
    }
}
```

## 8. 密钥管理

### 加密密钥轮换

```csharp
public class KeyManagementService
{
    private readonly ISecurityConfigurationService _securityConfig;
    
    [HttpPost("rotate-key/{keyName}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RotateEncryptionKey(string keyName)
    {
        try
        {
            var newKey = await _securityConfig.RotateEncryptionKeyAsync(keyName);
            
            return Ok(new { 
                Message = $"密钥 {keyName} 已成功轮换",
                NewKeyId = newKey[..8] + "..." // 只返回密钥前8位用于确认
            });
        }
        catch (SecurityException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
```

## 9. 部署和运维

### Docker部署安全配置

```dockerfile
# 使用非root用户运行
USER lybtapp

# 设置安全相关环境变量
ENV ASPNETCORE_HTTPS_PORT=7001
ENV ASPNETCORE_ENVIRONMENT=Production
ENV LYBT_SECURITY_STRICT_MODE=true

# 安全头部配置
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

### Nginx安全配置

```nginx
# SSL/TLS配置
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;
ssl_prefer_server_ciphers off;

# 安全头部
add_header X-Frame-Options "DENY" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

# 限流配置
limit_req_zone $binary_remote_addr zone=api:10m rate=10r/m;
limit_req_zone $binary_remote_addr zone=auth:10m rate=5r/m;
```

## 10. 安全检查清单

### 部署前安全检查

- [ ] 所有敏感配置已通过环境变量设置
- [ ] JWT密钥长度至少32字符
- [ ] 数据库连接字符串已加密
- [ ] HTTPS强制启用
- [ ] 安全头部正确配置
- [ ] 限流策略已启用
- [ ] 审计日志功能正常
- [ ] 密码策略符合要求
- [ ] 输入验证覆盖所有端点

### 定期安全维护

- [ ] 每季度轮换加密密钥
- [ ] 每月检查安全告警
- [ ] 定期审查用户权限
- [ ] 清理过期审计日志
- [ ] 更新安全组件依赖
- [ ] 执行安全渗透测试

## 11. 故障排除

### 常见问题

**问题1: JWT令牌验证失败**
```
解决方案：
1. 检查JWT密钥配置是否正确
2. 验证时钟偏移设置
3. 确认发行者和受众配置
```

**问题2: 限流误报**
```
解决方案：
1. 检查白名单IP配置
2. 调整限流窗口大小
3. 查看Redis连接状态
```

**问题3: 审计日志丢失**
```
解决方案：
1. 检查数据库连接
2. 验证日志服务注册
3. 查看异常日志记录
```

## 12. 性能考虑

### 优化建议

1. **加密性能**: 对于频繁访问的数据，考虑使用缓存
2. **审计性能**: 使用异步记录避免影响主流程
3. **验证性能**: 缓存验证结果减少重复计算
4. **令牌性能**: 合理设置令牌过期时间

### 监控指标

- 加密/解密操作耗时
- 审计日志写入速度
- 令牌验证成功率
- 安全异常发生频率
- API限流触发次数

通过以上配置和使用指南，可以确保凌隐宝堂系统具备企业级的安全防护能力。
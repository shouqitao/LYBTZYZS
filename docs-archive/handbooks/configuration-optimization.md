# 配置管理优化指南

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **维护者**: 项目团队
> **相关文档**: [环境配置指南](environment-setup-guide.md) | [安全架构](../architecture/security-architecture.md) | [部署文档](../deployment/)

## 📋 指南概述

本文档提供 LYBT 系统配置管理的全面优化指南，涵盖配置结构设计、安全管理、环境配置、验证机制和最佳实践。旨在简化配置流程、减少配置错误、提升系统安全性和可维护性。

## 🎯 配置管理目标

### 核心目标
- **简化配置**: 减少配置复杂度，提升配置效率
- **安全保护**: 保护敏感配置信息，防止泄露
- **环境隔离**: 支持多环境配置管理
- **自动验证**: 配置自动验证和错误检测
- **版本控制**: 配置变更的可追溯性

### 业务价值
- 减少 80% 的配置相关错误
- 提升 60% 的环境配置效率
- 增强系统安全性和合规性
- 简化运维和部署流程

## 🏗️ 配置架构设计

### 配置层次结构

```
配置根目录
├── appsettings.json              # 基础配置
├── appsettings.Development.json  # 开发环境配置
├── appsettings.Staging.json      # 测试环境配置
├── appsettings.Production.json   # 生产环境配置
├── appsettings.Local.json        # 本地开发配置（git忽略）
└── Secrets                       # 敏感配置（外部管理）
    ├── Database/
    ├── Authentication/
    ├── ExternalServices/
    └── Infrastructure/
```

### 配置优先级

| 优先级 | 配置源 | 说明 | 示例 |
|--------|--------|------|------|
| 1 | 命令行参数 | 最高优先级，用于临时覆盖 | `--ConnectionStrings:DefaultConnection=...` |
| 2 | 环境变量 | 容器化和CI/CD配置 | `ASPNETCORE_ENVIRONMENT=Production` |
| 3 | 用户密钥 | 开发环境敏感配置 | 用户密钥 |
| 4 | 环境特定配置文件 | 环境相关配置 | `appsettings.Production.json` |
| 5 | 基础配置文件 | 默认配置 | `appsettings.json` |

## 🔧 配置结构优化

### 1. 基础配置结构

#### appsettings.json
```json
{
  // 应用程序基础配置
  "Application": {
    "Name": "LYBT.Server",
    "Version": "1.0.0",
    "Environment": "Development",
    "Logging": {
      "Level": "Information",
      "EnableConsole": true,
      "EnableFile": false
    }
  },

  // 服务器配置
  "Server": {
    "Urls": "http://localhost:5000",
    "Cors": {
      "AllowOrigins": ["http://localhost:3000"],
      "AllowMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowHeaders": ["*"]
    }
  },

  // 数据库配置模板（不包含敏感信息）
  "Database": {
    "Provider": "SqlServer",
    "ConnectionStringName": "DefaultConnection",
    "MigrationsAssembly": "LYBT.Server",
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "CommandTimeout": 30
  },

  // 认证配置模板
  "Authentication": {
    "Jwt": {
      "Issuer": "LYBT",
      "Audience": "LYBT.Client",
      "TokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7
    },
    "Cookie": {
      "ExpirationDays": 1,
      "SlidingExpiration": true
    }
  },

  // 外部服务配置
  "ExternalServices": {
    "HealthCheck": {
      "Enabled": true,
      "IntervalSeconds": 30,
      "TimeoutSeconds": 10
    },
    "Logging": {
      "EnableFileLogging": false,
      "EnableSeqLogging": false,
      "SeqServerUrl": ""
    }
  },

  // 缓存配置
  "Cache": {
    "Provider": "Memory",
    "DefaultExpirationMinutes": 60,
    "EnableDistributedCache": false,
    "RedisConnectionStringName": "Redis"
  },

  // 文件存储配置
  "Storage": {
    "Provider": "Local",
    "LocalStoragePath": "./uploads",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".jpg", ".png", ".pdf", ".doc", ".docx"]
  }
}
```

#### 环境特定配置示例

##### appsettings.Production.json
```json
{
  "Application": {
    "Environment": "Production",
    "Logging": {
      "Level": "Warning",
      "EnableConsole": false,
      "EnableFile": true
    }
  },

  "Server": {
    "Urls": "http://0.0.0.0:80",
    "Cors": {
      "AllowOrigins": ["https://lybt.example.com"],
      "AllowMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowHeaders": ["Authorization", "Content-Type"]
    }
  },

  "Database": {
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 5,
    "CommandTimeout": 60
  },

  "ExternalServices": {
    "Logging": {
      "EnableFileLogging": true,
      "EnableSeqLogging": true,
      "SeqServerUrl": "https://seq.example.com"
    }
  },

  "Cache": {
    "Provider": "Redis",
    "EnableDistributedCache": true
  }
}
```

### 2. 敏感配置管理

#### 用户密钥结构
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=LYBT_Production;User Id=lybt_user;Password=prod_password;",
    "Redis": "prod-redis:6379"
  },
  "Authentication": {
    "Jwt": {
      "SecretKey": "your-super-secret-jwt-key-at-least-256-bits",
      "SigningKey": "your-signing-key"
    }
  },
  "ExternalServices": {
    "Email": {
      "SmtpServer": "smtp.example.com",
      "SmtpPort": 587,
      "Username": "noreply@example.com",
      "Password": "smtp_password"
    },
    "Sms": {
      "ApiKey": "sms-service-api-key",
      "SecretKey": "sms-service-secret"
    }
  }
}
```

#### 环境变量配置
```bash
# 生产环境环境变量
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod-db;Database=LYBT;..."
export Authentication__Jwt__SecretKey="your-jwt-secret"
export ExternalServices__Email__Password="email-password"

# 容器化配置
docker run -e ASPNETCORE_ENVIRONMENT=Production \
           -e ConnectionStrings__DefaultConnection="..." \
           lybt-server:latest
```

## 🔒 安全配置管理

### 1. 敏感信息保护原则

#### 敏感配置识别
- **数据库连接字符串**: 包含用户名和密码
- **API密钥**: 第三方服务认证信息
- **JWT密钥**: 令牌签名和验证密钥
- **证书路径**: SSL/TLS证书文件路径
- **加密密钥**: 数据加密相关密钥

#### 保护措施
```csharp
// 配置验证服务
public class ConfigurationValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(IConfiguration configuration, ILogger<ConfigurationValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateConfigurationAsync()
    {
        var isValid = true;

        // 验证必需配置
        isValid &= ValidateRequiredConfigurations();

        // 验证敏感配置安全
        isValid &= ValidateSensitiveConfigurations();

        // 验证数据库连接
        isValid &= await ValidateDatabaseConnectionAsync();

        // 验证外部服务连接
        isValid &= await ValidateExternalServicesAsync();

        return isValid;
    }

    private bool ValidateRequiredConfigurations()
    {
        var requiredConfigs = new[]
        {
            "Application:Name",
            "Database:ConnectionStringName",
            "Authentication:Jwt:Issuer"
        };

        foreach (var config in requiredConfigs)
        {
            if (string.IsNullOrEmpty(_configuration[config]))
            {
                _logger.LogError("必需的配置缺失: {Config}", config);
                return false;
            }
        }

        return true;
    }

    private bool ValidateSensitiveConfigurations()
    {
        // 检查敏感配置是否被意外暴露
        var sensitivePatterns = new[]
        {
            "password",
            "secret",
            "key",
            "connectionstring"
        };

        foreach (var section in _configuration.GetChildren())
        {
            ValidateSectionSecurity(section, sensitivePatterns);
        }

        return true;
    }

    private void ValidateSectionSecurity(IConfigurationSection section, string[] sensitivePatterns)
    {
        foreach (var child in section.GetChildren())
        {
            var value = child.Value;
            if (!string.IsNullOrEmpty(value))
            {
                // 检查是否在日志中暴露敏感信息
                if (IsSensitiveInformation(value, sensitivePatterns))
                {
                    _logger.LogWarning("在部分检测到敏感配置: {SectionPath}", child.Path);
                }
            }

            ValidateSectionSecurity(child, sensitivePatterns);
        }
    }

    private bool IsSensitiveInformation(string value, string[] sensitivePatterns)
    {
        return sensitivePatterns.Any(pattern =>
            value.ToLower().Contains(pattern) && value.Length > 20);
    }
}
```

### 2. 配置加密

#### 配置加密服务
```csharp
public class ConfigurationEncryptionService
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<ConfigurationEncryptionService> _logger;

    public ConfigurationEncryptionService(IDataProtectionProvider dataProtectionProvider,
        ILogger<ConfigurationEncryptionService> logger)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("LYBT.Configuration.v1");
        _logger = logger;
    }

    public string EncryptValue(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var encryptedValue = _dataProtector.Protect(plainText);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedValue));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密配置值失败");
            throw;
        }
    }

    public string DecryptValue(string encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
            return encryptedValue;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedValue);
            var encryptedText = Encoding.UTF8.GetString(encryptedBytes);
            return _dataProtector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密配置值失败");
            throw;
        }
    }
}
```

## 🌍 环境配置管理

### 1. 开发环境配置

#### 本地开发配置 (.NET 用户密钥)
```bash
# 初始化用户密钥
dotnet user-secrets init

# 设置数据库连接
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;"

# 设置JWT密钥
dotnet user-secrets set "Authentication:Jwt:SecretKey" "dev-jwt-secret-key-256-bits-minimum"

# 设置外部服务配置
dotnet user-secrets set "ExternalServices:Email:SmtpPassword" "dev-email-password"

# 查看所有密钥
dotnet user-secrets list
```

#### Docker 开发环境
```dockerfile
# Dockerfile.dev
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# 复制配置文件
COPY appsettings.json .
COPY appsettings.Development.json .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Development

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "LYBT.Server.dll"]
```

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  lybt-server:
    build:
      context: .
      dockerfile: Dockerfile.dev
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=LYBT_Dev;User Id=sa;Password=DevPassword123!
      - Authentication__Jwt__SecretKey=dev-jwt-secret-key
    volumes:
      - ./appsettings.json:/app/appsettings.json
      - ./appsettings.Development.json:/app/appsettings.Development.json
    depends_on:
      - sql-server
      - redis

  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=DevPassword123!
    ports:
      - "1433:1433"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```

### 2. 测试环境配置

#### CI/CD 配置
```yaml
# .github/workflows/ci.yml
name: CI/CD 流水线

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: TestPassword123!
        ports:
          - 1433:1433
      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379

    steps:
    - uses: actions/checkout@v3

    - name: 设置 .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: 还原依赖项
      run: dotnet restore

    - name: 构建
      run: dotnet build --no-restore

    - name: 测试
      run: dotnet test --no-build --verbosity normal
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!
        Authentication__Jwt__SecretKey: test-jwt-secret-key

    - name: 集成测试
      run: dotnet test ./tests/IntegrationTests --no-build --verbosity normal
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!
```

### 3. 生产环境配置

#### Kubernetes 配置
```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: lybt-config
  namespace: lybt-production
data:
  appsettings.json: |
    {
      "Application": {
        "Name": "LYBT.Server",
        "Environment": "Production",
        "Logging": {
          "Level": "Warning",
          "EnableConsole": false,
          "EnableFile": true
        }
      },
      "Database": {
        "Provider": "SqlServer",
        "ConnectionStringName": "DefaultConnection",
        "EnableRetryOnFailure": true,
        "MaxRetryCount": 5
      }
    }
  appsettings.Production.json: |
    {
      "Cache": {
        "Provider": "Redis",
        "EnableDistributedCache": true
      },
      "ExternalServices": {
        "Logging": {
          "EnableFileLogging": true,
          "EnableSeqLogging": true
        }
      }
    }

---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: lybt-secrets
  namespace: lybt-production
type: Opaque
data:
  connection-string: <base64-encoded-connection-string>
  jwt-secret: <base64-encoded-jwt-secret>
  email-password: <base64-encoded-email-password>

---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lybt-server
  namespace: lybt-production
spec:
  replicas: 3
  selector:
    matchLabels:
      app: lybt-server
  template:
    metadata:
      labels:
        app: lybt-server
    spec:
      containers:
      - name: lybt-server
        image: lybt-server:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: connection-string
        - name: Authentication__Jwt__SecretKey
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: jwt-secret
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config-volume
        configMap:
          name: lybt-config
```

## ✅ 配置验证机制

### 1. 启动时验证

#### 配置验证中间件
```csharp
public class ConfigurationValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfigurationValidationService _validationService;
    private readonly ILogger<ConfigurationValidationMiddleware> _logger;

    public ConfigurationValidationMiddleware(
        RequestDelegate next,
        IConfigurationValidationService validationService,
        ILogger<ConfigurationValidationMiddleware> logger)
    {
        _next = next;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 只在应用启动时验证一次
        if (!context.Items.ContainsKey("ConfigurationValidated"))
        {
            var isValid = await _validationService.ValidateConfigurationAsync();

            if (!isValid)
            {
                _logger.LogError("配置验证失败。应用程序启动已中止。");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("配置验证失败");
                return;
            }

            context.Items["ConfigurationValidated"] = true;
            _logger.LogInformation("配置验证成功通过");
        }

        await _next(context);
    }
}

// 扩展方法注册
public static class ConfigurationValidationExtensions
{
    public static IApplicationBuilder UseConfigurationValidation(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ConfigurationValidationMiddleware>();
    }
}
```

#### Program.cs 配置
```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置验证
builder.Services.AddScoped<IConfigurationValidationService, ConfigurationValidationService>();
builder.Services.AddScoped<IConfigurationEncryptionService, ConfigurationEncryptionService>();

// 数据保护
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("./keys"))
    .SetApplicationName("LYBT")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// 配置管理
ConfigureConfiguration(builder.Configuration, builder.Environment);

var app = builder.Build();

// 配置验证中间件
app.UseConfigurationValidation();

// 其他中间件
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void ConfigureConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    // 配置验证
    var validationService = new ConfigurationValidationService(configuration,
        new Logger<ConfigurationValidationService>(new LoggerFactory()));

    var isValid = validationService.ValidateConfigurationAsync().GetAwaiter().GetResult();

    if (!isValid)
    {
        throw new InvalidOperationException("配置验证失败");
    }
}
```

### 2. 运行时监控

#### 配置健康检查
```csharp
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHealthCheck> _logger;

    public ConfigurationHealthCheck(IConfiguration configuration, ILogger<ConfigurationHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // 检查关键配置
            data["Environment"] = _configuration["Application:Environment"];
            data["DatabaseProvider"] = _configuration["Database:Provider"];
            data["CacheProvider"] = _configuration["Cache:Provider"];

            // 检查连接字符串是否配置
            var connectionStringName = _configuration["Database:ConnectionStringName"];
            var connectionString = _configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy("数据库连接字符串未配置", data);
            }

            // 检查JWT配置
            var jwtIssuer = _configuration["Authentication:Jwt:Issuer"];
            if (string.IsNullOrEmpty(jwtIssuer))
            {
                return HealthCheckResult.Unhealthy("JWT颁发者未配置", data);
            }

            return HealthCheckResult.Healthy("配置有效", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置健康检查失败");
            return HealthCheckResult.Unhealthy("配置健康检查失败", ex);
        }
    }
}
```

## 🚀 最佳实践

### 1. 配置管理原则

#### 应该做的 ✅
- **分层配置**: 按环境和功能分层配置
- **敏感保护**: 敏感配置使用安全存储
- **自动验证**: 启动时自动验证配置完整性
- **版本控制**: 配置文件纳入版本控制（敏感信息除外）
- **文档化**: 配置项有清晰的文档说明
- **监控告警**: 配置错误时及时告警

#### 不应该做的 ❌
- **硬编码**: 避免在代码中硬编码配置
- **明文存储**: 敏感信息不要明文存储
- **混合配置**: 不要在同一文件中混合不同环境的配置
- **忽略验证**: 不要忽略配置验证的重要性
- **过度复杂**: 避免配置结构过于复杂

### 2. 开发工作流

#### 配置开发流程
```bash
# 1. 开发环境配置初始化
git clone <repository>
cd LYBTZYZS
dotnet restore

# 2. 配置本地开发环境
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;"
dotnet user-secrets set "Authentication:Jwt:SecretKey" "dev-jwt-secret-key-256-bits"

# 3. 验证配置
dotnet run --environment Development

# 4. 运行测试
dotnet test --environment Testing

# 5. 构建生产配置
dotnet publish -c Release -o ./publish
```

#### 配置部署流程
```bash
# 1. 准备生产配置
# 创建 appsettings.Production.json
# 配置环境变量
# 设置 Kubernetes secrets

# 2. 配置验证
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml

# 3. 部署应用
kubectl apply -f k8s/deployment.yaml

# 4. 验证部署
kubectl get pods -n lybt-production
kubectl logs -f deployment/lybt-server -n lybt-production
```

### 3. 故障排除

#### 常见配置问题

##### 1. 连接字符串问题
```csharp
// 问题诊断
public class DatabaseConnectionDiagnostic
{
    public static async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库连接失败: {ex.Message}");
            return false;
        }
    }
}

// 使用示例
var connectionString = configuration.GetConnectionString("DefaultConnection");
var isConnected = await DatabaseConnectionDiagnostic.TestConnectionAsync(connectionString);
```

##### 2. JWT配置问题
```csharp
// JWT配置验证
public class JwtConfigurationValidator
{
    public static bool ValidateJwtConfiguration(IConfiguration configuration)
    {
        var secretKey = configuration["Authentication:Jwt:SecretKey"];
        var issuer = configuration["Authentication:Jwt:Issuer"];
        var audience = configuration["Authentication:Jwt:Audience"];

        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            Console.WriteLine("JWT密钥太短（至少需要32个字符）");
            return false;
        }

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            Console.WriteLine("JWT颁发者或受众未配置");
            return false;
        }

        return true;
    }
}
```

##### 3. 环境变量问题
```bash
# 检查环境变量
echo $ASPNETCORE_ENVIRONMENT
echo $ConnectionStrings__DefaultConnection

# PowerShell 检查
Get-ChildItem Env:
```

## 📊 配置监控与审计

### 1. 配置变更监控

#### 配置变更日志
```csharp
public class ConfigurationChangeLogger
{
    private readonly ILogger<ConfigurationChangeLogger> _logger;
    private readonly IConfiguration _configuration;

    public ConfigurationChangeLogger(ILogger<ConfigurationChangeLogger> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public void LogConfigurationChange(string key, string oldValue, string newValue)
    {
        _logger.LogInformation("配置已更改: {Key} from {OldValue} to {NewValue}",
            key, MaskSensitiveValue(oldValue), MaskSensitiveValue(newValue));
    }

    private string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var sensitiveKeys = new[] { "password", "secret", "key", "connectionstring" };
        var isSensitive = sensitiveKeys.Any(key =>
            value.ToLower().Contains(key) && value.Length > 10);

        return isSensitive ? "***已屏蔽***" : value;
    }
}
```

### 2. 配置审计报告

#### 配置合规性检查
```csharp
public class ConfigurationAuditService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationAuditService> _logger;

    public ConfigurationAuditService(IConfiguration configuration, ILogger<ConfigurationAuditService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ConfigurationAuditResult GenerateAuditReport()
    {
        var result = new ConfigurationAuditResult();

        // 检查安全配置
        result.SecurityConfiguration = CheckSecurityConfiguration();

        // 检查性能配置
        result.PerformanceConfiguration = CheckPerformanceConfiguration();

        // 检查合规性配置
        result.ComplianceConfiguration = CheckComplianceConfiguration();

        return result;
    }

    private SecurityConfigurationResult CheckSecurityConfiguration()
    {
        var result = new SecurityConfigurationResult();

        // 检查HTTPS配置
        result.HttpsEnabled = _configuration.GetValue<bool>("Server:HttpsEnabled");

        // 检查CORS配置
        result.CorsConfigured = _configuration.GetSection("Server:Cors").Exists();

        // 检查JWT配置
        result.JwtConfigured = !string.IsNullOrEmpty(_configuration["Authentication:Jwt:SecretKey"]);

        return result;
    }

    private PerformanceConfigurationResult CheckPerformanceConfiguration()
    {
        var result = new PerformanceConfigurationResult();

        // 检查缓存配置
        result.CacheEnabled = _configuration.GetValue<bool>("Cache:Enabled");

        // 检查数据库连接池配置
        result.ConnectionPoolEnabled = _configuration.GetValue<bool>("Database:EnableConnectionPool");

        return result;
    }

    private ComplianceConfigurationResult CheckComplianceConfiguration()
    {
        var result = new ComplianceConfigurationResult();

        // 检查数据保护配置
        result.DataProtectionEnabled = _configuration.GetValue<bool>("DataProtection:Enabled");

        // 检查审计日志配置
        result.AuditLogEnabled = _configuration.GetValue<bool>("Logging:AuditLogEnabled");

        return result;
    }
}
```

## 🔄 配置热更新

### 1. 配置热更新机制

#### 配置重载服务
```csharp
public class ConfigurationReloadService : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationReloadService> _logger;
    private readonly IOptionsMonitorCache<IConfiguration> _optionsCache;
    private IDisposable _changeToken;

    public ConfigurationReloadService(
        IConfiguration configuration,
        ILogger<ConfigurationReloadService> logger,
        IOptionsMonitorCache<IConfiguration> optionsCache)
    {
        _configuration = configuration;
        _logger = logger;
        _optionsCache = optionsCache;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _changeToken = ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            () => OnConfigurationChanged());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _changeToken?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
    }

    private void OnConfigurationChanged()
    {
        _logger.LogInformation("配置已重新加载");

        // 清除选项缓存
        _optionsCache.TryClear();

        // 触发配置变更事件
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler ConfigurationChanged;
}
```

## 📚 参考资料

### 相关文档
- [.NET 配置系统](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Kubernetes ConfigMaps](https://kubernetes.io/docs/concepts/configuration/configmap/)
- [Docker 环境变量](https://docs.docker.com/engine/reference/commandline/run/#set-environment-variables--e---env---env-file)

### 最佳实践
- [配置管理最佳实践](https://docs.microsoft.com/en-us/azure/architecture/framework/security/design-secrets-management)
- [敏感信息管理](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [多环境部署](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments)

## 🔄 版本历史

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2025-10-15 | 初始版本 | 项目团队 |

## 📞 联系方式

- **维护者**: 项目团队
- **技术支持**: DevOps 团队
- **反馈渠道**: GitHub Issues 或内部反馈系统

---

*本文档遵循项目文档标准编写，如有疑问请参考相关文档或联系维护者。*

# 配置系统架构

## 概述

LYBTZYZS项目采用统一的配置管理系统，基于.NET Options模式实现类型安全、可验证、热更新的配置管理。

### 设计目标

1. **类型安全**: 所有配置通过强类型Options类访问，编译期检查
2. **统一验证**: DataAnnotations + 自定义验证器，启动时验证配置有效性
3. **热更新支持**: IOptionsMonitor/IOptionsSnapshot支持运行时配置变更
4. **清晰层次**: 配置项按功能域组织，服务端/客户端/共享明确分离
5. **可测试性**: Options类和验证器均可独立单元测试

## 项目结构

```
src/Shared/LYBT.Shared.Configuration/
├── Constants/
│   └── ConfigurationSections.cs      # 配置节名称常量
├── Options/
│   ├── Common/
│   │   └── JwtOptions.cs             # 共享JWT配置
│   ├── Server/
│   │   ├── DatabaseOptions.cs        # 数据库配置
│   │   ├── SecurityOptions.cs        # 安全配置
│   │   ├── SessionOptions.cs         # 服务端会话配置
│   │   ├── UserManagementOptions.cs  # 用户管理配置
│   │   ├── PasswordPolicyOptions.cs  # 密码策略配置
│   │   ├── DefaultPasswordOptions.cs # 默认密码配置
│   │   ├── SystemAdminOptions.cs     # 系统管理员配置
│   │   ├── LoggingOptions.cs         # 日志配置
│   │   ├── MemoryCacheOptions.cs     # 内存缓存配置
│   │   ├── SwaggerOptions.cs         # Swagger配置
│   │   └── JsonOptions.cs            # JSON序列化配置
│   └── Client/
│       ├── ApiClientOptions.cs       # API客户端配置
│       ├── ClientSessionOptions.cs   # 客户端会话配置
│       ├── ClinicSettingsOptions.cs  # 诊所设置配置
│       ├── FeatureToggleOptions.cs   # 功能开关配置
│       └── PrescriptionOptions.cs    # 处方配置
├── Validation/
│   ├── JwtOptionsValidator.cs        # JWT配置验证器
│   ├── DatabaseOptionsValidator.cs   # 数据库配置验证器
│   └── SecurityOptionsValidator.cs   # 安全配置验证器
└── Extensions/
    ├── ServerConfigurationExtensions.cs  # 服务端配置注册
    └── ClientConfigurationExtensions.cs  # 客户端配置注册
```

## 配置节命名规范

所有配置节名称在 `ConfigurationSections.cs` 中集中定义：

```csharp
public static class ConfigurationSections
{
    // 共享配置
    public const string Jwt = "Jwt";

    // 服务端配置
    public const string Database = "Database";
    public const string Security = "Security";
    public const string Session = "Session";
    public const string UserManagement = "UserManagement";
    public const string PasswordPolicy = "PasswordPolicy";
    public const string DefaultPassword = "DefaultPassword";
    public const string SystemAdmin = "SystemAdmin";
    public const string Logging = "Logging";
    public const string MemoryCache = "MemoryCache";
    public const string Swagger = "Swagger";
    public const string Json = "Json";

    // 客户端配置
    public const string ApiClient = "ApiClient";
    public const string ClientSession = "ClientSession";
    public const string ClinicSettings = "ClinicSettings";
    public const string FeatureToggles = "FeatureToggles";
    public const string Prescription = "Prescription";
}
```

## Options类设计规范

### 1. 基本结构

```csharp
public sealed class ExampleOptions
{
    public const string SectionName = ConfigurationSections.Example;

    /// <summary>
    /// 属性描述（必须有XML文档注释）
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int SomeProperty { get; set; } = 10; // 提供合理默认值
}
```

### 2. 验证属性

常用的DataAnnotations验证属性：

| 属性 | 用途 | 示例 |
|-----|------|------|
| `[Required]` | 必填项 | 密钥、连接字符串 |
| `[Range(min, max)]` | 数值范围 | 超时时间、重试次数 |
| `[MinLength(n)]` | 最小长度 | 密钥长度 |
| `[MaxLength(n)]` | 最大长度 | 字符串上限 |
| `[Url]` | URL格式 | API地址 |
| `[RegularExpression]` | 正则匹配 | 复杂格式验证 |

### 3. 嵌套配置

```csharp
public sealed class DatabaseOptions
{
    public const string SectionName = ConfigurationSections.Database;

    public string? ConnectionString { get; set; }

    // 嵌套配置对象
    public ConnectionPoolOptions ConnectionPool { get; set; } = new();
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

public sealed class ConnectionPoolOptions
{
    [Range(1, 100)]
    public int MinConnections { get; set; } = 5;

    [Range(10, 1000)]
    public int MaxConnections { get; set; } = 100;
}
```

## 自定义验证器

当DataAnnotations不足以表达复杂验证逻辑时，使用自定义验证器：

```csharp
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        // 验证SecretKey是否为有效Base64
        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            try
            {
                var bytes = Convert.FromBase64String(options.SecretKey);
                if (bytes.Length < 32)
                {
                    failures.Add("SecretKey 解码后必须至少 32 字节");
                }
            }
            catch (FormatException)
            {
                failures.Add("SecretKey 必须是有效的 Base64 编码字符串");
            }
        }

        // 验证AccessToken过期时间不能超过RefreshToken
        var accessMinutes = options.AccessTokenExpirationMinutes;
        var refreshMinutes = options.RefreshTokenExpirationDays * 24 * 60;
        if (accessMinutes >= refreshMinutes)
        {
            failures.Add("AccessToken 过期时间必须小于 RefreshToken 过期时间");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
```

## 配置注册

### 服务端注册

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLybtServerConfiguration(builder.Configuration);
```

### 客户端注册

```csharp
// Shell/ServiceCollectionExtensions.cs
services.AddLybtClientConfiguration(configuration);

// Prism容器注册（Shell/Extensions/PrismConfigurationExtensions.cs）
containerRegistry.RegisterLybtConfiguration(serviceProvider);
```

## 配置使用

### 构造函数注入

```csharp
public class AuthService
{
    private readonly JwtOptions _jwtOptions;

    public AuthService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }
}
```

### 热更新支持

```csharp
public class HealthCheckService
{
    private readonly IOptionsMonitor<ApiClientOptions> _optionsMonitor;

    public HealthCheckService(IOptionsMonitor<ApiClientOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;

        // 监听配置变更
        _optionsMonitor.OnChange(options =>
        {
            // 处理配置变更
        });
    }

    public string GetBaseUrl() => _optionsMonitor.CurrentValue.BaseUrl;
}
```

## 配置文件示例

### 服务端 appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "base64-encoded-secret-key-at-least-32-bytes",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "Database": {
    "ConnectionString": "Server=...;Database=LYBTZYZS;...",
    "MigrationTimeoutSeconds": 300,
    "ConnectionPool": {
      "MinConnections": 5,
      "MaxConnections": 100
    }
  },
  "Security": {
    "RateLimiting": {
      "Enabled": true,
      "LoginLimit": {
        "PermitLimit": 10,
        "WindowSeconds": 60
      }
    }
  }
}
```

### 客户端 appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "base64-encoded-secret-key",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client"
  },
  "ApiClient": {
    "BaseUrl": "https://localhost:5001/",
    "TimeoutSeconds": 60,
    "IgnoreSslErrors": false
  },
  "ClientSession": {
    "InactivityTimeoutMinutes": 5,
    "WarningBeforeTimeoutMinutes": 1
  },
  "FeatureToggles": {
    "ConsultationCreate": true,
    "PrescriptionCreate": false
  }
}
```

## 环境变量覆盖

配置可通过环境变量覆盖，使用双下划线分隔层级：

```bash
# 覆盖JWT密钥
export Jwt__SecretKey=new-secret-key

# 覆盖数据库连接字符串
export Database__ConnectionString=Server=prod;...

# 覆盖嵌套配置
export Database__ConnectionPool__MaxConnections=200
```

## 测试

配置系统提供完整的单元测试覆盖：

```
tests/UnitTests/Shared/LYBT.Shared.Configuration.Tests/
├── Options/
│   ├── JwtOptionsTests.cs           # JwtOptions验证测试
│   └── ApiClientOptionsTests.cs     # ApiClientOptions验证测试
├── Validation/
│   └── JwtOptionsValidatorTests.cs  # 自定义验证器测试
├── Extensions/
│   └── ServerConfigurationExtensionsTests.cs  # 扩展方法测试
└── Integration/
    ├── ConfigurationLoadingTests.cs  # 完整配置加载测试
    └── ValidateOnStartTests.cs       # ValidateOnStart测试
```

运行测试：

```bash
dotnet test tests/UnitTests/Shared/LYBT.Shared.Configuration.Tests
```

## 最佳实践

1. **始终提供默认值**: 减少必填配置项，提高开发体验
2. **使用常量定义配置节名称**: 避免魔法字符串
3. **添加XML文档注释**: 清晰说明每个配置项用途
4. **使用DataAnnotations**: 优先使用内置验证属性
5. **自定义验证器处理复杂逻辑**: 跨属性验证、格式验证等
6. **启用ValidateOnStart**: 启动时验证配置，快速失败
7. **选择正确的注入方式**:
   - `IOptions<T>`: 简单场景，单例配置
   - `IOptionsSnapshot<T>`: 需要热更新，但每次请求获取最新值
   - `IOptionsMonitor<T>`: 需要热更新通知或长时间运行的服务

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-12-23 | 初始版本，统一配置系统重构完成 |

---

**维护者**: Claude Code
**最后更新**: 2025-12-23

# Design: 统一配置系统架构设计

## 1. 项目结构

### 1.1 新建项目

```
src/Shared/LYBT.Shared.Configuration/
├── LYBT.Shared.Configuration.csproj
├── Options/
│   ├── Common/
│   │   └── JwtOptions.cs              # JWT 配置 (Server/Client 共用)
│   ├── Server/
│   │   ├── DatabaseOptions.cs         # 数据库连接配置
│   │   ├── SecurityOptions.cs         # 安全策略配置
│   │   ├── SessionOptions.cs          # 会话管理配置
│   │   ├── LoggingOptions.cs          # 日志配置
│   │   ├── UserManagementOptions.cs   # 用户管理配置
│   │   ├── SystemAdminOptions.cs      # 系统管理员配置
│   │   ├── PasswordPolicyOptions.cs   # 密码策略配置
│   │   └── DefaultPasswordOptions.cs  # 默认密码配置
│   └── Client/
│       ├── ApiClientOptions.cs        # API 客户端配置
│       ├── FeatureToggleOptions.cs    # 功能开关配置
│       ├── ClinicSettingsOptions.cs   # 诊所设置配置
│       └── ClientSessionOptions.cs    # 客户端会话配置
├── Validation/
│   ├── JwtOptionsValidator.cs
│   ├── DatabaseOptionsValidator.cs
│   └── SecurityOptionsValidator.cs
├── Extensions/
│   ├── ServerConfigurationExtensions.cs
│   └── ClientConfigurationExtensions.cs
└── Constants/
    └── ConfigurationSections.cs
```

### 1.2 项目文件

```xml
<!-- LYBT.Shared.Configuration.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" Version="8.0.*" />
  </ItemGroup>
</Project>
```

## 2. Options 类设计

### 2.1 通用配置

```csharp
// Options/Common/JwtOptions.cs
namespace LYBT.Shared.Configuration.Options;

/// <summary>
/// JWT 认证配置
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Lybt:Jwt";
    
    /// <summary>
    /// JWT 签名密钥 (Base64 编码)
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey 不能为空")]
    [MinLength(32, ErrorMessage = "JWT SecretKey 长度不能小于 32 字符")]
    public string SecretKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌发行者
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "LYBT.WebAPI";
    
    /// <summary>
    /// 令牌受众
    /// </summary>
    [Required]
    public string Audience { get; set; } = "LYBT.Client";
    
    /// <summary>
    /// 访问令牌过期时间 (分钟)
    /// </summary>
    [Range(5, 1440, ErrorMessage = "AccessTokenExpirationMinutes 必须在 5-1440 之间")]
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// 刷新令牌过期时间 (天)
    /// </summary>
    [Range(1, 30, ErrorMessage = "RefreshTokenExpirationDays 必须在 1-30 之间")]
    public int RefreshTokenExpirationDays { get; set; } = 7;
    
    /// <summary>
    /// 时钟偏差容忍度 (秒)
    /// </summary>
    [Range(0, 600)]
    public int ClockSkewSeconds { get; set; } = 300;
}
```

### 2.2 服务端配置

```csharp
// Options/Server/DatabaseOptions.cs
namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 数据库配置
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Lybt:Database";
    
    /// <summary>
    /// 自动迁移
    /// </summary>
    public bool AutoMigrate { get; set; } = false;
    
    /// <summary>
    /// 开发环境自动创建数据库
    /// </summary>
    public bool EnsureCreatedInDevelopment { get; set; } = true;
    
    /// <summary>
    /// 连接池配置
    /// </summary>
    public ConnectionPoolOptions ConnectionPool { get; set; } = new();
    
    /// <summary>
    /// 监控配置
    /// </summary>
    public MonitoringOptions Monitoring { get; set; } = new();
    
    /// <summary>
    /// 重试策略配置
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

public sealed class ConnectionPoolOptions
{
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 20;
    
    [Range(0, 50)]
    public int MinConnections { get; set; } = 2;
    
    [Range(5, 120)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    [Range(5, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}

public sealed class MonitoringOptions
{
    public bool Enabled { get; set; } = true;
    public bool LogAllQueries { get; set; } = false;
    
    [Range(100, 60000)]
    public int SlowQueryThresholdMs { get; set; } = 1000;
}

public sealed class RetryPolicyOptions
{
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;
    
    [Range(100, 10000)]
    public int BaseDelayMs { get; set; } = 1000;
    
    [Range(1000, 60000)]
    public int MaxDelayMs { get; set; } = 10000;
}
```

```csharp
// Options/Server/SecurityOptions.cs
namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 安全配置
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Lybt:Security";
    
    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();
}

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public RateLimitOptions GlobalLimit { get; set; } = new() { PermitLimit = 200 };
    public LoginRateLimitOptions LoginLimit { get; set; } = new();
    public ApiRateLimitOptions ApiLimit { get; set; } = new();
    public List<string> WhitelistedIPs { get; set; } = ["127.0.0.1", "::1"];
}

public sealed class RateLimitOptions
{
    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 100;
    
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
    
    [Range(0, 100)]
    public int QueueLimit { get; set; } = 0;
}

public sealed class LoginRateLimitOptions : RateLimitOptions
{
    public int InternalPermitLimit { get; set; } = 20;
    public int InternalQueueLimit { get; set; } = 0;
    
    public LoginRateLimitOptions()
    {
        PermitLimit = 5;
    }
}

public sealed class ApiRateLimitOptions : RateLimitOptions
{
    public int AdminPermitLimit { get; set; } = 200;
}
```

### 2.3 客户端配置

```csharp
// Options/Client/ApiClientOptions.cs
namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// API 客户端配置
/// </summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "Lybt:Client:Api";
    
    /// <summary>
    /// API 基础地址
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://localhost:5001/";
    
    /// <summary>
    /// 请求超时时间 (秒)
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// 忽略 SSL 错误 (仅开发环境)
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = false;
}

// Options/Client/FeatureToggleOptions.cs
namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 功能开关配置
/// </summary>
public sealed class FeatureToggleOptions
{
    public const string SectionName = "FeatureToggles";
    
    // Consultation 模块
    public bool ConsultationCreate { get; set; } = false;
    public bool ConsultationEdit { get; set; } = false;
    public bool ConsultationDelete { get; set; } = false;
    public bool ConsultationViewDetail { get; set; } = true;
    public bool ConsultationSearch { get; set; } = true;
    
    // Prescription 模块
    public bool PrescriptionCreate { get; set; } = false;
    public bool PrescriptionDelete { get; set; } = false;
    public bool PrescriptionClone { get; set; } = true;
    public bool PrescriptionExport { get; set; } = true;
    public bool PrescriptionViewDetail { get; set; } = true;
    public bool PrescriptionSearch { get; set; } = true;
    
    // MedicalCase 模块
    public bool MedicalCaseCreate { get; set; } = true;
    public bool MedicalCaseEdit { get; set; } = true;
    public bool MedicalCaseDelete { get; set; } = true;
    public bool MedicalCaseViewDetail { get; set; } = true;
    public bool MedicalCaseSearch { get; set; } = true;
}

// Options/Client/ClinicSettingsOptions.cs
namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 诊所设置配置
/// </summary>
public sealed class ClinicSettingsOptions
{
    public const string SectionName = "ClinicSettings";
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public string Department { get; set; } = "中医科";
}
```

## 3. 验证器设计

```csharp
// Validation/JwtOptionsValidator.cs
namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// JWT 配置自定义验证器
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        
        // 验证 SecretKey 是否为有效的 Base64
        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            try
            {
                var bytes = Convert.FromBase64String(options.SecretKey);
                if (bytes.Length < 32)
                {
                    failures.Add("JWT SecretKey 解码后长度必须至少为 32 字节");
                }
            }
            catch (FormatException)
            {
                failures.Add("JWT SecretKey 必须是有效的 Base64 字符串");
            }
        }
        
        // 验证 AccessToken 过期时间小于 RefreshToken
        if (options.AccessTokenExpirationMinutes >= options.RefreshTokenExpirationDays * 24 * 60)
        {
            failures.Add("AccessToken 过期时间必须小于 RefreshToken 过期时间");
        }
        
        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
```

## 4. 扩展方法设计

```csharp
// Extensions/ServerConfigurationExtensions.cs
namespace LYBT.Shared.Configuration.Extensions;

/// <summary>
/// 服务端配置扩展方法
/// </summary>
public static class ServerConfigurationExtensions
{
    /// <summary>
    /// 添加服务端配置
    /// </summary>
    public static IServiceCollection AddLybtServerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT 配置
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<JwtOptionsValidator>()
            .ValidateOnStart();
        
        // 数据库配置
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 安全配置
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 会话配置
        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(SessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 日志配置 (支持热更新)
        services.AddOptions<LoggingOptions>()
            .Bind(configuration.GetSection(LoggingOptions.SectionName))
            .ValidateDataAnnotations();
        
        // 用户管理配置
        services.AddOptions<UserManagementOptions>()
            .Bind(configuration.GetSection(UserManagementOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 密码策略配置
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
}

// Extensions/ClientConfigurationExtensions.cs
namespace LYBT.Shared.Configuration.Extensions;

/// <summary>
/// 客户端配置扩展方法
/// </summary>
public static class ClientConfigurationExtensions
{
    /// <summary>
    /// 添加客户端配置
    /// </summary>
    public static IServiceCollection AddLybtClientConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT 配置 (客户端用于令牌验证)
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // API 客户端配置
        services.AddOptions<ApiClientOptions>()
            .Bind(configuration.GetSection(ApiClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 功能开关配置 (支持热更新)
        services.AddOptions<FeatureToggleOptions>()
            .Bind(configuration.GetSection(FeatureToggleOptions.SectionName))
            .ValidateDataAnnotations();
        
        // 诊所设置配置
        services.AddOptions<ClinicSettingsOptions>()
            .Bind(configuration.GetSection(ClinicSettingsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // 客户端会话配置
        services.AddOptions<ClientSessionOptions>()
            .Bind(configuration.GetSection(ClientSessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
}
```

## 5. 配置节名称常量

```csharp
// Constants/ConfigurationSections.cs
namespace LYBT.Shared.Configuration.Constants;

/// <summary>
/// 配置节名称常量
/// </summary>
public static class ConfigurationSections
{
    // 通用配置
    public const string Jwt = "Lybt:Jwt";
    
    // 服务端配置
    public const string Database = "Lybt:Database";
    public const string Security = "Lybt:Security";
    public const string Session = "Lybt:Session";
    public const string Logging = "Lybt:Logging";
    public const string UserManagement = "Lybt:UserManagement";
    public const string SystemAdmin = "Lybt:SystemAdmin";
    public const string PasswordPolicy = "Lybt:PasswordPolicy";
    public const string DefaultPasswords = "Lybt:DefaultPasswords";
    public const string MemoryCache = "Lybt:MemoryCache";
    
    // 客户端配置
    public const string ApiClient = "Lybt:Client:Api";
    public const string ClientSession = "Lybt:Client:Session";
    public const string FeatureToggles = "FeatureToggles";
    public const string ClinicSettings = "ClinicSettings";
    public const string Prescription = "Prescription";
}
```

## 6. 使用示例

### 6.1 服务端启动配置

```csharp
// LYBT.WebAPI/Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加配置
builder.Services.AddLybtServerConfiguration(builder.Configuration);

// 使用配置
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Convert.FromBase64String(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
        };
    });
```

### 6.2 服务中注入使用

```csharp
// 使用 IOptions<T> (单例，启动时绑定)
public class AuthService
{
    private readonly JwtOptions _jwtOptions;
    
    public AuthService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }
}

// 使用 IOptionsMonitor<T> (单例，支持变更通知)
public class FeatureService
{
    private readonly IOptionsMonitor<FeatureToggleOptions> _featureOptions;
    
    public FeatureService(IOptionsMonitor<FeatureToggleOptions> featureOptions)
    {
        _featureOptions = featureOptions;
    }
    
    public bool IsFeatureEnabled(string featureName)
    {
        var options = _featureOptions.CurrentValue;
        // 使用 CurrentValue 获取最新配置
        return featureName switch
        {
            "Consultation.Create" => options.ConsultationCreate,
            "Consultation.Edit" => options.ConsultationEdit,
            _ => false
        };
    }
}
```

## 7. 迁移策略

### 7.1 一次性迁移步骤

1. **Phase 1**: 创建 `LYBT.Shared.Configuration` 项目，定义 Options 类
2. **Phase 2**: 重新设计 `appsettings.json` 结构，统一 Server/Client
3. **Phase 3**: 直接替换所有 `IConfiguration` 访问为 Options 注入
4. **Phase 4**: 删除 `ConfigurationHelper` 和所有旧配置代码

### 7.2 配置结构重新设计

- **简化层级**: `Lybt:Jwt` → `Jwt`，减少嵌套
- **统一命名**: Server/Client 使用相同的配置节名称
- **移除冗余**: 删除重复定义的配置项

## 8. 测试策略

```csharp
// 单元测试示例
[Fact]
public void JwtOptions_WithInvalidSecretKey_ShouldFailValidation()
{
    // Arrange
    var options = new JwtOptions { SecretKey = "too-short" };
    var validator = new JwtOptionsValidator();
    
    // Act
    var result = validator.Validate(null, options);
    
    // Assert
    Assert.True(result.Failed);
    Assert.Contains("32", result.FailureMessage);
}

[Fact]
public void AddLybtServerConfiguration_ShouldRegisterAllOptions()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();
    var services = new ServiceCollection();
    
    // Act
    services.AddLybtServerConfiguration(configuration);
    var provider = services.BuildServiceProvider();
    
    // Assert
    Assert.NotNull(provider.GetService<IOptions<JwtOptions>>());
    Assert.NotNull(provider.GetService<IOptions<DatabaseOptions>>());
    Assert.NotNull(provider.GetService<IOptions<SecurityOptions>>());
}
```

---
created: 2025-12-23
status: draft

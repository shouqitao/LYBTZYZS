# LYBT.Shared.Configuration 代码知识

统一的配置选项类型定义、验证器和 DI 注册扩展，为 Server 和 Client 提供强类型配置绑定。

## 代码文件结构

```
Constants/
└── ConfigurationSections.cs     # 配置节名称常量，所有 Options 的 SectionName 引用此处

Options/
├── Common/
│   └── JwtOptions.cs            # JWT 认证配置 (Server/Client 共用)
├── Server/
│   ├── DatabaseOptions.cs       # 数据库配置 (含连接池/监控/重试子类)
│   ├── SecurityOptions.cs       # 安全配置 (含速率限制子类层次)
│   ├── SessionOptions.cs        # 服务端会话配置
│   ├── LoggingOptions.cs        # 日志配置 (含日志清理子类)
│   ├── SystemAdminOptions.cs    # 系统管理员配置
│   ├── PasswordPolicyOptions.cs # 密码策略配置 [SUSPECT]
│   ├── DefaultPasswordOptions.cs# 默认密码配置
│   ├── UserManagementOptions.cs # 用户管理配置 [SUSPECT]
│   ├── MemoryCacheOptions.cs    # 内存缓存配置
│   ├── SwaggerOptions.cs        # Swagger API 文档配置
│   └── JsonOptions.cs           # JSON 序列化配置 [DEAD]
├── Client/
│   ├── ApiClientOptions.cs      # API 客户端配置
│   ├── ClientSessionOptions.cs  # 客户端会话配置
│   ├── FeatureToggleOptions.cs  # 功能开关配置
│   ├── ClinicSettingsOptions.cs # 诊所设置配置
│   ├── PrescriptionOptions.cs   # 处方配置
│   └── SyncOptions.cs           # 数据同步配置

Validation/
├── JwtOptionsValidator.cs       # JWT 配置自定义验证器
├── DatabaseOptionsValidator.cs  # 数据库配置验证器
└── SecurityOptionsValidator.cs  # 安全配置验证器

Extensions/
├── ServerConfigurationExtensions.cs  # 服务端配置 DI 注册
└── ClientConfigurationExtensions.cs  # 客户端配置 DI 注册
```

### Constants/ConfigurationSections.cs
**ConfigurationSections** (static class) | 所有配置节名称的集中定义，避免魔法字符串

所有 Options 类通过 `public const string SectionName = ConfigurationSections.Xxx` 引用。包含: Jwt, Database, Swagger, Json, Security, Session, Logging, UserManagement, SystemAdmin, PasswordPolicy, DefaultPasswords, MemoryCache, ApiClient, Sync, ClientSession, FeatureToggles, ClinicSettings, Prescription。

### Options/Common/JwtOptions.cs
**JwtOptions** | JWT 认证配置，Server 和 Client 共用

属性: SecretKey (Base64编码签名密钥), Issuer, Audience, AccessTokenExpirationMinutes (默认30), RefreshTokenExpirationDays (默认7), ClockSkewSeconds (默认300)。使用 DataAnnotations 进行基本验证。

### Options/Server/DatabaseOptions.cs
**DatabaseOptions** | 数据库配置，包含连接字符串、迁移设置和三个嵌套配置类

属性: ConnectionString (可选，有 fallback 链), AutoMigrate, EnsureCreatedInDevelopment, MigrationTimeoutSeconds, ConnectionPool, Monitoring, RetryPolicy。

**ConnectionPoolOptions** | 连接池配置: MaxConnections(20)/MinConnections(2)/ConnectionTimeoutSeconds(30)/CommandTimeoutSeconds(30)

**MonitoringOptions** | 数据库监控: Enabled/LogAllQueries/SlowQueryThresholdMs(1000)

**RetryPolicyOptions** | 重试策略: MaxRetryCount(3)/BaseDelayMs(1000)/MaxDelayMs(10000)

### Options/Server/SecurityOptions.cs
**SecurityOptions** | 安全配置，包含速率限制和审计保留天数

属性: RateLimiting (嵌套), AuditRetentionDays (默认365)。

**RateLimitingOptions** | 速率限制总配置: Enabled, GlobalLimit, LoginLimit, ApiLimit, WhitelistedIPs

**RateLimitOptions** | 速率限制基类: PermitLimit/WindowSeconds/QueueLimit

**LoginRateLimitOptions** : RateLimitOptions | 登录速率限制，增加 InternalPermitLimit(20)，默认 PermitLimit=5

**ApiRateLimitOptions** : RateLimitOptions | API 速率限制，增加 AdminPermitLimit(200)

### Options/Server/SessionOptions.cs
**SessionOptions** | 服务端会话配置: TimeoutMinutes(120)/AllowConcurrentSessions(false)/SlidingExpiration(true)

### Options/Server/LoggingOptions.cs
**LoggingOptions** | 日志配置，包含清理子配置

**LogCleanupOptions** | 日志清理: Enabled/RetentionDays(90)/CleanupIntervalHours(24)/InitialDelayMinutes(5)/BatchSize(1000)

### Options/Server/SystemAdminOptions.cs
**SystemAdminOptions** | 系统管理员初始化配置: UserName/Email/DisplayName/AutoCreateOnStartup/SessionTimeoutMinutes(240)

### Options/Server/PasswordPolicyOptions.cs
**PasswordPolicyOptions** | 密码策略: MinLength(8)/RequireDigit/RequireLowercase/RequireUppercase/RequireSpecialChar

### Options/Server/DefaultPasswordOptions.cs
**DefaultPasswordOptions** | 默认密码配置: SysAdminPassword/NewUserPassword/ForceChangeOnFirstLogin/EnableInDevelopment/AllowInProduction(false)/OnlyWhenDatabaseEmpty/ExpiryDays(30)

### Options/Server/UserManagementOptions.cs
**UserManagementOptions** | 用户管理: DefaultRole("Staff")/AllowSelfRegistration(false)/RequireEmailConfirmation/EnableUserCache/MaxBatchOperationSize(100)

### Options/Server/MemoryCacheOptions.cs
**MemoryCacheOptions** | 内存缓存: Enabled/SizeLimit(100MB)/CompactionPercentage(0.05)/ExpirationScanFrequencySeconds(60)/DefaultExpirationMinutes(5)

### Options/Server/SwaggerOptions.cs
**SwaggerOptions** | Swagger API 文档配置: Title/Description/Contact 信息/License/EnableXmlComments/RoutePrefix/DocumentTitle/EnableInProduction(false)

### Options/Server/JsonOptions.cs
**JsonOptions** | JSON 序列化配置: UnsafeRelaxedEscaping/PropertyNamingPolicy/IgnoreReadOnlyProperties/AllowTrailingCommas

### Options/Client/ApiClientOptions.cs
**ApiClientOptions** | API 客户端: BaseUrl/TimeoutSeconds(60)/IgnoreSslErrors(false)

### Options/Client/ClientSessionOptions.cs
**ClientSessionOptions** | 客户端会话: InactivityTimeoutMinutes(15)/WarningBeforeTimeoutMinutes(2)/ActivityCheckIntervalSeconds(30)

### Options/Client/FeatureToggleOptions.cs
**FeatureToggleOptions** | 功能开关，按模块分组

包含 Consultation (Create/Edit/Delete/ViewDetail/Search)、Prescription (Create/Delete/Clone/Export/ViewDetail/Search)、MedicalCase (Create/Edit/Delete/ViewDetail/Search) 模块的布尔开关，以及 CardReaderEnabled 硬件设备开关。

### Options/Client/ClinicSettingsOptions.cs
**ClinicSettingsOptions** | 诊所设置: Name/Address/Phone/Department("中医科")

### Options/Client/PrescriptionOptions.cs
**PrescriptionOptions** | 处方配置: DuplicateHerbMergeStrategy("Max"，可选 Max/Sum/First)

### Options/Client/SyncOptions.cs
**SyncOptions** | 数据同步: OverwriteConflicts(false)

### Validation/JwtOptionsValidator.cs
**JwtOptionsValidator** : IValidateOptions\<JwtOptions\> | JWT 配置自定义验证

| 方法 | 说明 |
|------|------|
| Validate(name, options) | 验证 SecretKey 为有效 Base64 且解码后>=32字节；验证 AccessToken 过期时间 < RefreshToken |

### Validation/DatabaseOptionsValidator.cs
**DatabaseOptionsValidator** : IValidateOptions\<DatabaseOptions\> | 数据库配置验证

| 方法 | 说明 |
|------|------|
| Validate(name, options) | 验证 MinConnections <= MaxConnections；验证 BaseDelayMs <= MaxDelayMs |

### Validation/SecurityOptionsValidator.cs
**SecurityOptionsValidator** : IValidateOptions\<SecurityOptions\> | 安全配置验证

| 方法 | 说明 |
|------|------|
| Validate(name, options) | 验证 LoginLimit.InternalPermitLimit >= PermitLimit；验证 ApiLimit.AdminPermitLimit >= PermitLimit |

### Extensions/ServerConfigurationExtensions.cs
**ServerConfigurationExtensions** (static class) | 服务端配置 DI 注册入口

| 方法 | 说明 |
|------|------|
| AddLybtServerConfiguration(services, configuration) | 注册全部服务端 Options + 验证器，绑定 IConfiguration，启用 ValidateOnStart (Logging 除外，支持热更新) |

### Extensions/ClientConfigurationExtensions.cs
**ClientConfigurationExtensions** (static class) | 客户端配置 DI 注册入口

| 方法 | 说明 |
|------|------|
| AddLybtClientConfiguration(services, configuration) | 注册全部客户端 Options + JWT 验证器，绑定 IConfiguration，FeatureToggles/Prescription/Sync 支持热更新 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| JsonOptions | [DEAD] | 仅在 ServerConfigurationExtensions 外由 WebAPI ServiceCollectionExtensions 引用一次，但 ServerConfigurationExtensions 自身未注册此 Options | 确认 WebAPI 侧是否独立注册，若未注册则清理 |
| PasswordPolicyOptions | [SUSPECT] | 仅通过 ServerConfigurationExtensions 注册到 DI，未见任何 Service 注入使用 | 确认是否有 Service 通过 IOptions\<PasswordPolicyOptions\> 使用 |
| UserManagementOptions | [SUSPECT] | 仅通过 ServerConfigurationExtensions 注册到 DI，未见任何 Service 注入使用 | 确认是否有 Service 通过 IOptions\<UserManagementOptions\> 使用 |
| LoggingOptions / LogCleanupOptions | [SUSPECT] | 仅通过 ServerConfigurationExtensions 注册，未见消费端 | 确认日志清理后台任务是否使用 |
| AddLybtServerConfiguration | [SUSPECT] | 仅在 WebAPI Program.cs 调用一次，但部分 Options (PasswordPolicy/UserManagement/Logging) 注册后无消费端 | 审查消费端注入情况 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| JsonOptions.cs | ServerConfigurationExtensions 未注册此 Options | WebAPI 通过自己的 ServiceCollectionExtensions 单独使用，但如果其未调用 AddOptions 绑定则为死代码 | 统一到 ServerConfigurationExtensions 或确认独立注册 |
| SecurityOptions.cs | 类层次嵌套较深 (4 个 class 在同一文件) | RateLimitOptions -> LoginRateLimitOptions/ApiRateLimitOptions 继承链合理但文件较大 (73 行) | 可接受，嵌套配置类放同一文件是常见模式 |
| DatabaseOptions.cs | 同文件包含 4 个类 | ConnectionPoolOptions/MonitoringOptions/RetryPolicyOptions 作为 DatabaseOptions 的子配置，逻辑内聚 | 可接受 |
| ServerConfigurationExtensions | 使用非标准的 Validate 委托模式 | `.Validate<TValidator>((options, validator) => ...)` 而非 `.Services.AddSingleton<IValidateOptions<T>, TValidator>()` | 两种模式均可，当前方式需额外注册 Singleton，稍显冗余 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| LoggingOptions 不使用 ValidateOnStart | 日志配置需要支持运行时热更新，启动时验证会锁定配置 | 有意设计，不要添加 ValidateOnStart |
| FeatureToggleOptions/PrescriptionOptions/SyncOptions 不使用 ValidateOnStart | 客户端需要支持配置热更新 | 有意设计 |
| MemoryCacheOptions 与 Microsoft.Extensions.Caching.Memory.MemoryCacheOptions 同名 | 命名空间不同但可能造成引用混淆 | 使用时需注意 using 指定完整命名空间 |
| DefaultPasswordOptions 包含敏感信息 | SysAdminPassword/NewUserPassword 存储在配置中 | 生产环境 AllowInProduction 默认为 false，应通过环境变量注入而非配置文件 |

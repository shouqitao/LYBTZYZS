# 配置架构

## 概述

系统使用 .NET Options 模式 (`IOptions<T>` / `IOptionsMonitor<T>`) 进行强类型配置管理。大部分配置类集中在 `LYBT.Shared.Configuration` 项目中，Server 和 Client 各有独立的 DI 注册扩展方法。少数仅单一项目使用的 Options (如 `JsonOptions`) 直接定义在消费方项目中。

配置管道: `appsettings.json` → 环境覆盖 → Options 绑定 → DataAnnotations 验证 → `IValidateOptions<T>` 自定义验证 → `ValidateOnStart` 启动时验证。

## 验证管道

```mermaid
graph LR
    A[appsettings.json] --> B[IConfiguration]
    B --> C[AddOptions<T>.Bind]
    C --> D[ValidateDataAnnotations]
    D --> E[IValidateOptions<T>]
    E --> F[ValidateOnStart]
```

### 三层验证策略

| 层级 | 机制 | 用途 | 示例 |
|------|------|------|------|
| 1. DataAnnotations | `[Required]`, `[Range]`, `[MinLength]` | 基本格式和范围校验 | `SecretKey` 不能为空 |
| 2. IValidateOptions&lt;T&gt; | 自定义验证器类 | 跨字段业务规则验证 | AccessToken 过期 < RefreshToken 过期 |
| 3. ValidateOnStart | 启动时触发全部验证 | 快速失败，避免运行时错误 | 无效配置阻止应用启动 |

### 自定义验证器

验证器通过 `IValidateOptions<T>` 接口实现，注册为 DI 单例:

```csharp
services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
```

| 验证器 | 目标 Options | 验证规则 |
|--------|-------------|----------|
| JwtOptionsValidator | JwtOptions | SecretKey 为有效 Base64 且解码后 ≥ 32 字节；AccessToken 过期 < RefreshToken 过期 |
| DatabaseOptionsValidator | DatabaseOptions | MinConnections ≤ MaxConnections；BaseDelayMs ≤ MaxDelayMs |
| SecurityOptionsValidator | SecurityOptions | LoginLimit.InternalPermitLimit ≥ PermitLimit；ApiLimit.AdminPermitLimit ≥ PermitLimit |

## 环境分层

系统使用三层配置文件，后加载的覆盖先前的:

```
appsettings.json                  # 基础配置 (所有环境共享的默认值)
├── appsettings.Development.json  # 开发环境覆盖
└── appsettings.Production.json   # 生产环境覆盖
```

### 环境差异示例

| 配置项 | 基础值 | Development | Production |
|--------|--------|-------------|------------|
| Database:AutoMigrate | false | true | (不覆盖) |
| Security:RateLimiting:Enabled | true | false | (不覆盖) |
| DefaultPasswords:EnableInDevelopment | false | true | (不覆盖) |
| Jwt:SecretKey | 开发用密钥 | (不覆盖) | `${JWT_SECRET_KEY}` 环境变量 |
| Database:ConnectionString | (空) | (不覆盖) | `${DB_CONNECTION_STRING}` 环境变量 |

**约束**: 生产环境敏感配置 (密钥、连接字符串) 必须通过环境变量注入，不得硬编码在配置文件中。

## 热更新支持

部分 Options 有意跳过 `ValidateOnStart()`，以支持运行时配置热更新 (`IOptionsMonitor<T>`):

| Options | 跳过 ValidateOnStart | 原因 |
|---------|:-------------------:|------|
| LoggingOptions (Server) | ✓ | 日志级别需要运行时动态调整，不能被启动验证锁定 |
| FeatureToggleOptions (Client) | ✓ | 功能开关、处方配置、同步策略等需要运行时切换，无需重启 |

其余所有 Options 均启用 `ValidateOnStart()`，确保无效配置在启动时快速失败。

## Server 端注册

`AddLybtServerConfiguration(services, configuration)` 注册 8 个 Options 和 3 个验证器:

| Options | 配置节 | 验证器 | ValidateOnStart |
|---------|--------|--------|:---------------:|
| JwtOptions | `Jwt` | JwtOptionsValidator | ✓ |
| DatabaseOptions | `Database` | DatabaseOptionsValidator | ✓ |
| SecurityOptions | `Security` | SecurityOptionsValidator | ✓ |
| SessionOptions | `Session` | — | ✓ |
| LoggingOptions | `Logging` | — | ✗ (热更新) |
| SystemAdminOptions | `SystemAdmin` | — | ✓ |
| DefaultPasswordOptions | `DefaultPasswords` | — | ✓ |
| MemoryCacheOptions | `MemoryCache` | — | ✓ |

## Client 端注册

`AddLybtClientConfiguration(services, configuration)` 注册 5 个 Options 和 1 个验证器:

| Options | 配置节 | 验证器 | ValidateOnStart |
|---------|--------|--------|:---------------:|
| JwtOptions | `Jwt` | JwtOptionsValidator | ✓ |
| ApiClientOptions | `ApiClient` | — | ✓ |
| ClientSessionOptions | `ClientSession` | — | ✓ |
| FeatureToggleOptions | `FeatureToggles` | — | ✗ (热更新) |
| ClinicSettingsOptions | `ClinicSettings` | — | ✓ |

## 配置节命名约定

每个 Options 类内联定义配置节名称常量:

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";
    // ...
}
```

绑定时直接引用:

```csharp
services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection(JwtOptions.SectionName))
```

## 测试覆盖

| 测试类 | 测试数 | 覆盖范围 |
|--------|--------|----------|
| ServerConfigurationExtensionsTests | 4 | DI 注册正确性 (JwtOptions, DatabaseOptions, SecurityOptions, SessionOptions) |
| ValidateOnStartTests | 9 | 验证失败场景 (无效 JWT 密钥、短密钥、过期时间矛盾、连接池范围、重试延迟范围、速率限制范围) + 成功场景 |

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-04-03 | 初始版本: Options 模式重构完成后的架构文档 |
| 2026-04-03 | PrescriptionOptions + SyncOptions 合并入 FeatureToggleOptions；JsonOptions 从 Shared.Configuration 迁移至 WebAPI 项目；客户端注册从 7 → 5 个 Options |

## 相关文档

- [配置需求规格 (PRD)](../02-requirements/configuration.md) — Options 类清单、功能需求、决策记录
- [服务端配置运维指南](../06-operations/configuration.md) — Server 端 appsettings.json 配置项详细说明

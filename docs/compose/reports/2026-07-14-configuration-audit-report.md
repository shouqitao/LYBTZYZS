# Configuration System Audit Report

> **Date:** 2026-07-14
> **Scope:** LYBT.Shared.Configuration project +全项目配置使用模式
> **Status:** 待修复

## Executive Summary

LYBT.Shared.Configuration 项目结构良好（13 Options 类 + 3 验证器 + DI 扩展），但全项目有 20+ 生产文件绕过强类型 Options，直接注入 IConfiguration 读取字符串键。需要修复的反模式分为 4 个优先级。

## P0 — 安全风险

| 位置 | 问题 | 影响 |
|------|------|------|
| `EmbeddedLocalWebApiService.cs:51-53` | 硬编码回退密码 `"SysAdmin@2026!"` | 密码泄露到源码 |
| `EmbeddedLocalWebApiService.cs:18-19` | 硬编码完整 LocalDB 连接字符串 | 连接信息泄露 |
| `LocalJwtConfig.cs:37` + `AuthController.cs:165` | 硬编码 JWT SecretKey 回退 | Token 签名风险 |

## P1 — 缺失 Options 类型（6 个配置节无强类型）

| 配置节 | 使用位置 | 当前读取方式 |
|--------|----------|--------------|
| `LocalJwt` | LocalWebAPI AuthController, LocalJwtConfig | `configuration["LocalJwt:SecretKey"]` |
| `Cors` | ApiServiceCollectionExtensions | 手动 `GetSection("Cors")` |
| `DesktopUpdate` | UnifiedMiddlewareConfiguration | `GetValue<bool>("DesktopUpdate:Enabled")` |
| `OfflineMode` | appsettings.json 有定义 | 无 Options 类 |
| `ApiSettings` | ApplicationStateService | `configuration["ApiSettings:BaseUrl"]` |
| `CardReader` | PrismConfigurationExtensions | 已有 `CardReaderOptions` 但在 Desktop 未在 Shared |

## P2 — IConfiguration 直接注入（20+ 文件）

### Server 端 (7 个模块 + WebAPI)

| 文件 | 使用 IConfiguration 的原因 | 修复方案 |
|------|---------------------------|----------|
| `UsersModule.cs` | `GetConnectionString("DefaultConnection")` | 改用 `IOptions<DatabaseOptions>` |
| `ReportsModule.cs` | 同上 | 同上 |
| `HerbsModule.cs` | 同上 | 同上 |
| `PatientsModule.cs` | 同上 | 同上 |
| `FormulaModule.cs` | 同上 | 同上 |
| `RegistrationModule.cs` | 同上 | 同上 |
| `AuthModule.cs` | 同上 | 同上 |
| `JwtService.cs` | 读 `ASPNETCORE_ENVIRONMENT` | 改用 `IWebHostEnvironment` |
| `Program.cs:264-268` | 读 DefaultPasswords/SystemAdmin | 改用 `IOptions<T>` |
| `DatabaseStartupDiagnostics.cs` | `GetConnectionString` | 改用 `IOptions<DatabaseOptions>` |
| `SqlServerHealthCheck.cs` | `GetConnectionString` | 同上 |
| `UnifiedApplicationInitialization.cs` | 运行时 resolve IConfiguration | 改用 `IOptions<DatabaseOptions>` |
| `ProductionConfigurationValidator.cs` | 读多个键做启动校验 | 保留但注释说明为何用 IConfiguration |

### Client 端 (Desktop)

| 文件 | 问题 | 修复方案 |
|------|------|----------|
| `EmbeddedLocalWebApiService.cs` | 硬编码密码+连接串 | 改用 Options |
| `ConnectionSettingsService.cs` | 手动读 `ApiClient:BaseUrl` 等 | 改用 `IOptions<ApiClientOptions>` |
| `ClinicSettingsService.cs` | 手动绑定 `ClinicSettings` 节 | 改用 `IOptions<ClinicSettingsOptions>` |
| `LocalTokenValidator.cs` | 手动 `GetSection().Bind()` | 改用 DI 注入 `IOptions<JwtOptions>` |
| `TokenRefreshHandler.cs` | 手动 `GetSection().Bind()` | 改用 DI 注入 `IOptions<ApiClientOptions>` |
| `ApiHealthCheckService.cs` | 手动 `GetSection().Bind()` | 改用 DI 注入 `IOptions<ApiClientOptions>` |
| `ApplicationStateService.cs` | `configuration["ApiSettings:BaseUrl"]` | 改用 `IOptions<ApiClientOptions>` |
| `LoginCoordinator.cs` | 注入 IConfiguration | 确认是否真正需要 |

## P3 — 低优先级

| 位置 | 问题 |
|------|------|
| `SwaggerOptions` | 无验证属性，无 ValidateOnStart |
| `JwtService.cs` | 同时注入 IOptions<JwtOptions> 和 IConfiguration（冗余） |
| `PrismConfigurationExtensions.cs:40` | `CardReaderOptions` 定义在 Desktop 不在 Shared |
| 3 处 `http://localhost:5300` | 应共享常量 |
| `ServiceCollectionExtensions.cs` (多个) | 扩展方法签名接受 IConfiguration（基础设施层合理但应限制） |

## Configuration Metrics

| 指标 | 当前值 | 目标值 |
|------|--------|--------|
| 强类型 Options 类 | 13 (Shared) + 3 (Desktop) | 19+ (全部配置节) |
| IValidateOptions 验证器 | 3 | 5+ |
| 直接 IConfiguration 注入 | ~20 文件 | <5 文件（仅基础设施层） |
| 硬编码魔法值 | 12+ | <3 |
| 缺失 Options 类型 | 6 | 0 |

## Task Dependency Map

```
Task 1 (Report) → Task 2 (LocalJwt) → Task 5 (Modules)
                → Task 3 (Desktop fixes) → Task 7 (Verify)
                → Task 4 (Remaining Options) → Task 6 (Client cleanup) → Task 7
```

# LYBT.Desktop.Foundation

> Desktop端技术基础层 | HTTP通信/缓存/配置/安全/性能

## 项目定位

- **层级**: Client Core层
- **职责**: 提供平台无关的技术基础能力(无WPF依赖)，支持跨平台复用

## 目录结构

```
LYBT.Desktop.Foundation/
├── Api/Managers/                 # API管理
├── Caching/                      # 缓存服务
│   └── CacheService.cs
├── Configuration/                # 配置管理
│   └── ConfigurationService.cs
├── Diagnostics/                  # 诊断服务
├── Exceptions/                   # 异常处理(5文件)
├── Extensions/                   # 扩展方法(3文件)
├── HealthCheck/                  # 健康检查(2文件)
├── Http/                         # HTTP客户端(3文件)
├── Modules/                      # 模块加载(2文件)
├── Performance/                  # 性能优化(2文件)
├── Repositories/                 # 仓储基类
│   └── BaseApiRepository.cs
├── Security/                     # 安全服务(9文件)
└── Settings/                     # 设置管理
```

## 核心服务

| 服务 | 方法数 | 说明 |
|------|--------|------|
| IAuthenticationService | 8 | 登录/登出/Token管理/密码修改 |
| ICacheService | 7 | 缓存CRUD/GetOrCreate |
| IConfigurationService | 10 | 配置读写/用户设置 |
| IApiService | 15 | RESTful CRUD/文件操作 |
| BaseApiRepository<T> | 8 | API仓储基类(CRUD+分页+搜索) |
| IApiHealthCheckService | 2 | API健康状态检查 |
| IStartupOptimizationService | 7 | 启动优化/预加载/预热 |
| IExceptionHandler | 4 | 异常处理/SafeExecute |

## 设计特点

| 特点 | 说明 |
|------|------|
| 平台无关 | 无WPF依赖，纯.NET 8技术栈 |
| Polly集成 | 重试/熔断/超时弹性策略 |
| DPAPI加密 | 安全凭证存储 |
| 三层HTTP抽象 | IApiService → ApiService → BaseApiRepository |

## 设计依据

- 与Infrastructure分离，保持平台无关性(无WPF依赖)，为未来跨平台(MAUI等)复用奠定基础
- BaseApiRepository提供统一的CRUD+分页基类，各模块仓储继承后只需关注业务差异
- 集成Polly弹性策略(重试/熔断/超时)，将网络不稳定性的处理统一在基础层，业务层无需关心
- DPAPI加密凭证存储，确保Token等敏感数据不以明文形式保存在本地

## 依赖关系

### 依赖
- LYBT.Shared.Models (共享DTO)
- LYBT.Shared.Utilities (共享工具)
- Microsoft.Extensions.Http (8.0.x)
- Microsoft.Extensions.Caching.Memory (8.0.x)
- Polly (8.x)

### 被依赖
- LYBT.Desktop.Infrastructure (WPF基础设施)
- LYBT.Desktop.Shell (DI注册)
- 所有Desktop业务模块

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-01-29 | Foundation与Infrastructure职责分离 |

## 开发笔记

# LYBT.Desktop.Foundation 代码知识

Desktop 端技术基础设施层，提供 HTTP 通信、安全认证、缓存、健康检查、模块加载、启动优化等底层服务。

**定位**: 纯技术基础设施，不包含业务逻辑和 UI 组件。与 Infrastructure 层的区别是 Foundation 不涉及 XAML 资源/样式，专注于 HTTP、安全、缓存等运行时服务。

**依赖**: LYBT.Desktop.Contracts (接口契约) + LYBT.Shared.Models + LYBT.Shared.Utilities + LYBT.Shared.ExceptionHandling + LYBT.Shared.Configuration

---

## 架构决策

| 决策 | 原因 | 日期 | 关联 OpenSpec |
|------|------|------|--------------|
| Token 纯内存存储 (不持久化) | 医疗系统合规要求：每次启动必须输入密码，进程退出自动清除 | Issue #1907 | - |
| 客户端 JWT 自验证 (LocalTokenValidator) | 移除 Server API 依赖 (/api/v1/auth/validate)，减少网络调用 | Issue #1863 | - |
| CredentialVault 使用 DPAPI + HMAC | 只有当前 Windows 用户能解密，HMAC 防篡改 | - | refactor-login-authentication (CVT-001, CVT-002) |
| TokenRefreshHandler 使用独立 HttpClient | 避免与 AuthorizationMessageHandler 循环依赖 | Issue #1838 | refactor-token-sliding-expiration (AUTH-002) |
| AuthenticationStateMachine 转换表驱动 | 合并原 LoginStateMachine + LoginFlowState 双状态机，线程安全 | - | refactor-auth-role-system (Phase 1.1) |
| LogoutService 本地/服务端分离 | 本地登出立即生效，服务端登出可重试，保证用户体验 | - | refactor-login-authentication (Phase 2.3) |
| ApiService Polly 组合策略 (重试+超时+熔断) | 统一的 HTTP 弹性策略，不重试 500 (非幂等安全) | Issue #1262 | - |
| 登出时保留 AutoLoginToken | 用户主动登出不清除自动登录凭据，只有取消勾选时才清除 | - | simplify-auth-architecture |
| TokenRefresh 失败时 AutoLogin 降级 | RefreshToken 过期/撤销/无效时，尝试 CredentialVault 中的 AutoLoginToken | - | refactor-login-authentication |

---

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| TokenRefreshHandler 不能使用 Refit IAuthApi | 会触发 AuthorizationMessageHandler 形成循环依赖 | 使用独立的 `_refreshHttpClient` 直接调用 HTTP 端点 |
| ITokenStorageService 异步方法在 WPF 属性 getter 中死锁 | `GetTokenAsync().Result` 在 UI 线程死锁 | 使用同步方法 `GetToken()` / `GetLoginResponse()` (底层为内存操作) |
| ShouldRetry 不重试 500 InternalServerError | POST/PUT/DELETE 非幂等，重试会重复执行 | Issue #1262: 只重试 502/503/504/408/429 |
| CredentialVault DPAPI 数据不可跨机器 | DataProtectionScope.CurrentUser 绑定当前 Windows 用户 | 设计如此，迁移机器需重新登录 |
| TokenStorageService.SaveAuthenticationAsync 的 rememberMe 参数被忽略 | Issue #1907: 医疗系统始终使用 Session 存储 | 参数保留以兼容接口签名，实际不做持久化 |
| AuthorizationMessageHandler 匿名端点列表硬编码 | /health, /api/auth/login 等不需要 Token | 修改端点时需同步更新 `IsAnonymousEndpoint` 方法 |
| TokenLifecycleService 在 Warning 状态触发 `Task.Run` 自动刷新 | 如果刷新失败，保持 Warning 状态等待用户操作 | 不会自动重试，避免刷新风暴 |

---

## OpenSpec 追踪

| OpenSpec ID | 内容 | 涉及文件 |
|-------------|------|----------|
| refactor-login-authentication | 登录认证重构 (CVT-001/002, TKM-001/002, Phase 1.4/2.3/3.1/3.2) | AuthenticationService, CredentialVault, TokenManager, TokenRefreshHandler, LogoutService, AuthEvents |
| refactor-token-sliding-expiration | Token 滑动过期 (AUTH-002) - 仅用户活跃时刷新 | TokenRefreshHandler |
| refactor-startup-connection-resilience | 启动连接韧性 - 事件驱动状态更新 | ApplicationStateService, ApiStatusChangedEventArgs |
| unify-event-system | 统一事件系统 (Phase 2.1/2.3/2.4) - Prism PubSubEvent | TokenRefreshHandler, LogoutService, TokenLifecycleService |
| refactor-auth-role-system | 认证角色系统重构 (Phase 1.1/1.2) - 统一状态机 | AuthenticationStateMachine, AuthenticationService |
| simplify-auth-architecture | 简化认证架构 - 移除 SessionExpiringEvent，登出保留 AutoLoginToken | AuthEvents, AuthenticationService |
| redesign-login-remember-password | 重新设计记住密码 - CredentialVault 密码存储功能 | ICredentialVault, CredentialVault |

---

## 安全基础设施层次

```
AuthenticationService (IAuthenticationService)
  |-- 登录/登出/Token验证/AutoLogin
  |-- 依赖: IAuthApi (Refit), ITokenStorageService, ITokenValidator, ICredentialVault
  |
AuthenticationStateMachine (IAuthenticationStateMachine)
  |-- 转换表驱动的认证状态机 (11 状态, 线程安全)
  |-- 状态: Idle -> Authenticating -> LoadingProfile -> LoadingModules -> Navigating -> Authenticated
  |-- 异常: Failed, SessionExpired, RefreshingToken, LoggingOut
  |
TokenStorageService (ITokenStorageService)
  |-- 内存级 Token 存储 (Session 级别)
  |-- 提供异步 + 同步方法 (避免 WPF 死锁)
  |
TokenManager (ITokenManager)
  |-- 新接口，同步方法，只管理 Token 字符串 + 过期时间
  |-- 与 ITokenStorageService 的区别: 不包含 LoginResponse
  |
LocalTokenValidator (ITokenValidator)
  |-- 客户端 JWT 自验证 (签名/Issuer/Audience/Claims)
  |-- 配置来源: appsettings.json 的 Lybt:Jwt 节
  |
CredentialVault (ICredentialVault)
  |-- DPAPI 加密 + HMAC-SHA256 完整性校验
  |-- 存储: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
  |-- 功能: AutoLoginToken 存储 + 密码存储 (记住密码)
  |
UsernameStorageService (IUsernameStorageService)
  |-- JSON 文件存储用户名和 "记住用户名" 设置
  |-- 存储: %LOCALAPPDATA%\LYBT\Desktop\username.json
  |
TokenLifecycleService (ITokenLifecycleService)
  |-- 状态机: NotAuthenticated -> Active -> Warning -> Expired
  |-- 定时器监控 (30秒间隔)，警告阈值 5 分钟
  |-- Warning 状态时自动尝试 Token 刷新
  |
LogoutService (ILogoutService)
  |-- 可靠登出: 本地立即生效 + 服务端可重试 (ConcurrentQueue)
  |-- 最大重试 3 次，间隔 1s/5s/15s
```

---

## HTTP 基础设施层次

```
ApiService (IApiService)
  |-- 统一 HTTP 调用抽象 (GET/POST/PUT/PATCH/DELETE/Download/Upload)
  |-- 内置: MemoryCache (GET 缓存) + RequestDeduplicator (去重) + Polly (弹性)
  |-- JSON: camelCase + JsonStringEnumConverter
  |-- 响应解包: 自动处理 ApiResponse<T> 信封格式
  |
AuthorizationMessageHandler (DelegatingHandler)
  |-- 自动注入 Bearer Token 到 HTTP 请求头
  |-- 跳过匿名端点 (/health, /api/auth/login, /api/auth/refresh)
  |
TokenRefreshHandler (DelegatingHandler + ITokenRefreshHandler)
  |-- 拦截 HTTP 请求，Access Token 过期前 5 分钟自动刷新
  |-- 滑动过期: 仅用户活跃时刷新 (依赖 IUserActivityState)
  |-- SemaphoreSlim 防止并发刷新
  |-- 失败策略: 3 次指数退避重试 -> AutoLogin 降级 -> 发布失败事件
  |-- 独立 HttpClient 调用刷新端点，避免循环依赖
  |
RetryPolicyExtensions
  |-- Polly 组合策略工厂: 重试 + 熔断器 + 超时
  |-- 重试: 指数退避，仅重试 502/503/504/408/429 (不重试 500)
  |-- 熔断器: 5 次失败后开启，持续 1 分钟
  |-- 超时: 默认 30 秒
```

---

## 事件体系

```
AuthEvents (Prism PubSubEvent)
  |-- LoginSucceededEvent     -> LoginSucceededPayload (User, TokenExpiresAt, IsAutoLogin)
  |-- LoginFailedEvent        -> LoginFailedPayload (Reason enum: 8 种失败类型)
  |-- LogoutCompletedEvent    -> LogoutCompletedPayload (Local/Server 分离状态)
  |-- ServerLogoutFailedEvent -> ServerLogoutFailedPayload (重试队列信息)
  |-- PendingLogoutsClearedEvent -> PendingLogoutsClearedPayload
  |-- PasswordChangedEvent    -> PasswordChangedPayload (Issue #1906)
  |-- TokenRefreshSucceededEvent -> TokenRefreshSucceededPayload
  |-- TokenRefreshFailedEvent -> TokenRefreshFailedPayload (Reason, 可重试/需重登)
  |-- SessionExpiredEvent     -> SessionExpiredPayload (4 种过期原因)

AuthStateChangedPubSubEvent   -> AuthStateChangedEventArgs (跨模块状态机变更)
TokenLifecycleStateChangedEvent -> TokenLifecycleStateChangedEventArgs
```

注意: `SessionExpiringEvent` 和 `ExpiringEvent` 已被 `simplify-auth-architecture` 移除，不再显示过期警告。`TokenEvents` 已于 2026-03-01 清理 (死代码，所有嵌套事件均未被订阅/发布)。

---

## 其他模块

| 模块 | 接口 | 实现 | 职责 |
|------|------|------|------|
| Application | IApplicationStateService | ApplicationStateService | API 健康状态管理，事件驱动状态更新 |
| HealthCheck | IApiHealthCheckService | ApiHealthCheckService | /health 端点健康检查 |
| Caching | IDesktopCacheManager | DesktopCacheManager | MemoryCache 清理 + CacheEvents 发布 |
| Modules | IModuleLoadingService | ModuleLoadingService | Prism 模块加载管理 |
| Performance | IStartupOptimizationService | StartupOptimizationService | 启动预热和耗时统计 |
| Settings | ISettingsService | SettingsService | 内存字典式设置管理 (Theme/Language/AutoSave) |

---

## 目录结构

```
LYBT.Desktop.Foundation/
├── Application/              # 应用状态管理
│   ├── IApplicationStateService.cs
│   ├── ApplicationStateService.cs
│   └── ApiStatusChangedEventArgs.cs
├── Caching/
│   └── DesktopCacheManager.cs  # 统一缓存失效管理
├── HealthCheck/
│   ├── IApiHealthCheckService.cs
│   └── ApiHealthCheckService.cs
├── Http/
│   ├── ApiService.cs           # 统一 HTTP 调用 + 去重 + 缓存
│   ├── AuthorizationMessageHandler.cs  # Bearer Token 自动注入
│   ├── TokenRefreshHandler.cs  # Token 自动刷新 + AutoLogin 降级
│   └── RetryPolicyExtensions.cs  # Polly 弹性策略
├── Modules/
│   ├── IModuleLoadingService.cs
│   └── ModuleLoadingService.cs
├── Performance/
│   ├── IStartupOptimizationService.cs
│   └── StartupOptimizationService.cs
├── Security/                  # 安全基础设施 (最大子模块)
│   ├── IAuthenticationService.cs / AuthenticationService.cs
│   ├── AuthenticationStateMachine.cs  # 转换表驱动状态机
│   ├── AuthEvents.cs           # 认证事件 + Payload 定义
│   ├── ICredentialVault.cs / CredentialVault.cs  # DPAPI 凭据保险库
│   ├── ILogoutService.cs / LogoutService.cs      # 可靠登出
│   ├── ITokenManager.cs / TokenManager.cs        # 内存级 Token 管理
│   ├── ITokenStorageService.cs / TokenStorageService.cs  # Session 级 Token 存储
│   ├── ITokenValidator.cs / LocalTokenValidator.cs  # 客户端 JWT 验证
│   ├── ITokenLifecycleService.cs / TokenLifecycleService.cs  # Token 生命周期监控
│   ├── ITokenRefreshHandler.cs   # 刷新接口 + 结果/事件类型
│   ├── TokenRefreshFailureReason.cs  # 7 种刷新失败原因枚举
│   ├── TokenLifecycleState.cs    # NotAuthenticated/Active/Warning/Expired
│   ├── TokenLifecycleStateChangedEvent.cs  # Prism PubSubEvent
│   ├── IUsernameStorageService.cs / UsernameStorageService.cs  # 记住用户名
│   └── ILogoutService.cs (含 LogoutResult, ServerLogoutFailureReason)
└── Settings/
    └── SettingsService.cs
```

---

## 代码文件结构

### Application/ - 应用状态管理

#### IApplicationStateService.cs
接口 `IApplicationStateService` -- 应用程序全局状态管理

| 成员 | 类型 | 说明 |
|------|------|------|
| `IsApiHealthy` | 属性 | API 是否健康 |
| `ApiBaseUrl` | 属性 | API 基础 URL |
| `ConnectionStatus` | 属性 | 连接状态描述 |
| `LastHealthCheckTime` | 属性 | 最后一次健康检查时间 |
| `LastError` | 属性 | 最后一次错误信息 |
| `StatusChanged` | 事件 | API 状态变更事件 |
| `CheckApiHealthAsync(int)` | 方法 | 执行 API 健康检查，默认超时 10 秒 |

#### ApplicationStateService.cs
类 `ApplicationStateService : IApplicationStateService` -- 应用状态服务实现

| 方法 | 说明 |
|------|------|
| `CheckApiHealthAsync(int)` | 调用 IApiHealthCheckService 执行健康检查，根据返回状态更新属性并触发 StatusChanged 事件 |

依赖: `IApiHealthCheckService?`, `IConfiguration`, `ILogger`

#### ApiStatusChangedEventArgs.cs
类 `ApiStatusChangedEventArgs : EventArgs` -- API 状态变更事件参数，包含 IsHealthy/ConnectionStatus/LastError/CheckTime 四个只读属性

---

### Caching/ - 缓存管理

#### DesktopCacheManager.cs
类 `DesktopCacheManager : IDesktopCacheManager` (sealed) -- 统一管理 ApiService GET 缓存失效

| 方法 | 说明 |
|------|------|
| `InvalidatePatientCaches()` | 清理 `GET:/api/v1/patients` 前缀缓存，发布 CacheEvents.InvalidatedEvent (Domain=Patients) |
| `InvalidateMedicalCaseCaches()` | 清理 `GET:/api/v1/medicalcases` 前缀缓存，发布 CacheEvents.InvalidatedEvent (Domain=MedicalCases) |
| `InvalidateAll()` | 清理全部缓存，发布 CacheEvents.InvalidatedEvent (Domain=All) |

依赖: `IMemoryCache`, `IEventAggregator`, `ILogger`

---

### HealthCheck/ - 健康检查

#### IApiHealthCheckService.cs
接口 `IApiHealthCheckService` + 枚举 `ApiHealthStatus` (Checking/Healthy/Unhealthy)

| 方法 | 说明 |
|------|------|
| `CheckHealthAsync(int)` | 异步检查 WebAPI 连接状态，默认超时 5000ms |
| `LastErrorMessage` | 属性，最后一次检查的错误信息 |

#### ApiHealthCheckService.cs
类 `ApiHealthCheckService : IApiHealthCheckService` -- 调用 /health 端点检查 API 健康状态

依赖: `HttpClient`, `IConfiguration`

---

### Http/ - HTTP 基础设施

#### ApiService.cs
接口 `IApiService` -- 统一 HTTP 调用抽象

| 方法 | 说明 |
|------|------|
| `GetAsync<TResponse>(string, object?, CancellationToken)` | GET 请求，支持 MemoryCache 缓存 + RequestDeduplicator 去重 |
| `PostAsync<TRequest, TResponse>(string, TRequest, CancellationToken)` | POST 请求 |
| `PutAsync<TRequest, TResponse>(string, TRequest, CancellationToken)` | PUT 请求 |
| `PatchAsync<TRequest, TResponse>(string, TRequest, CancellationToken)` | PATCH 请求 |
| `DeleteAsync(string, CancellationToken)` | DELETE 请求 |
| `DownloadAsync(string, CancellationToken)` | 文件下载 |
| `UploadAsync<TResponse>(string, Stream, string, Dictionary?, CancellationToken)` | 文件上传 (multipart/form-data) |

类 `ApiService : IApiService` -- 实现，内置 Polly 组合策略 (重试+超时+熔断)，JSON 序列化使用 camelCase + JsonStringEnumConverter

内部类 `RequestDeduplicator` -- 请求去重器，防止并发重复 GET 请求，带 5 分钟自动过期清理

#### AuthorizationMessageHandler.cs
类 `AuthorizationMessageHandler : DelegatingHandler` -- 自动注入 Bearer Token 到 HTTP 请求头

| 方法 | 说明 |
|------|------|
| `SendAsync(HttpRequestMessage, CancellationToken)` | 重写: 匿名端点直接放行，其余端点从 ITokenStorageService 获取 Token 注入 Authorization header |
| `IsAnonymousEndpoint(string)` | 静态私有: 判断是否为匿名端点 (/health, /api/auth/login, /api/auth/refresh 等) |

依赖: `ITokenStorageService`, `ILogger`

#### TokenRefreshHandler.cs
类 `TokenRefreshHandler : DelegatingHandler, ITokenRefreshHandler` -- Token 自动刷新 + AutoLogin 降级

| 方法 | 说明 |
|------|------|
| `SendAsync(HttpRequestMessage, CancellationToken)` | 重写: 拦截请求，Access Token 过期前 5 分钟自动刷新 |
| `RefreshTokenAsync()` | 主动刷新 Token (ITokenRefreshHandler 接口方法)，含 3 次指数退避重试 |
| `TryAutoLoginFallbackAsync()` | RefreshToken 失败时尝试 CredentialVault 中的 AutoLoginToken 降级登录 |

依赖: `ITokenStorageService`, `ICredentialVault`, `IUserActivityState?`, `IConfiguration`, `IEventAggregator?`, `ILogger`

#### RetryPolicyExtensions.cs
静态类 `RetryPolicyExtensions` -- Polly 弹性策略工厂

| 方法 | 说明 |
|------|------|
| `CreateHttpRetryPolicy(...)` | 创建 HTTP 重试策略 (指数退避) |
| `CreateTimeoutPolicy(...)` | 创建超时策略 |
| `CreateCircuitBreakerPolicy(...)` | 创建熔断器策略 (默认 5 次失败后开启，持续 1 分钟) |
| `CreateCompositePolicy(...)` | 创建组合策略: 重试 -> 熔断器 -> 超时 |

类 `RetryPolicyOptions` -- 重试策略配置选项 (RetryCount/BaseDelay/Timeout/CircuitBreakerThreshold/CircuitBreakerDuration/EnableRetry/EnableCircuitBreaker/EnableTimeout)

---

### Modules/ - 模块管理

#### IModuleLoadingService.cs
接口 `IModuleLoadingService` -- Prism 模块加载管理

| 方法 | 说明 |
|------|------|
| `LoadModuleAsync(string)` | 加载指定模块 |
| `LoadAllModulesAsync()` | 加载所有可用模块 |
| `LoadModulesAsync(IEnumerable<string>?)` | 加载模块集合 |
| `GetLoadedModules()` | 获取已加载的模块名列表 |
| `IsModuleLoaded(string)` | 检查指定模块是否已加载 |
| `ModuleLoaded` | 事件: 模块加载完成 |

#### ModuleLoadingService.cs
类 `ModuleLoadingService : IModuleLoadingService` -- 通过 Prism IModuleManager 加载模块，线程安全 (lock + HashSet)

依赖: `IModuleManager`, `IModuleCatalog`, `ILogger`

---

### Performance/ - 启动优化

#### IStartupOptimizationService.cs
接口 `IStartupOptimizationService` -- 启动性能优化

| 方法 | 说明 |
|------|------|
| `WarmupAsync()` | 启动预热 |
| `PreloadCriticalResourcesAsync()` | 预加载关键资源 |
| `OptimizeStartupAsync()` | 优化启动流程 |
| `WarmupApplicationAsync()` | 综合预热 (调用 Warmup + Preload) |
| `GetStartupDuration()` | 获取启动耗时 |
| `ClearStartupCache()` | 清理启动缓存 |
| `OptimizationCompleted` | 事件: 优化完成。**注意**: 从未被订阅，实现中已 `#pragma warning disable CS0067` |

#### StartupOptimizationService.cs
类 `StartupOptimizationService : IStartupOptimizationService` -- 启动优化实现

额外方法 `LogStartupMetrics()` 不在接口中定义，仅在本文件使用。**疑似死代码**。

依赖: `ILogger`

---

### Settings/ - 设置管理

#### SettingsService.cs
接口 `ISettingsService` + 类 `SettingsService : ISettingsService` -- 内存字典式设置管理，同文件定义

| 方法 | 说明 |
|------|------|
| `GetSetting<T>(string)` | 获取设置值 |
| `SaveSettingAsync<T>(string, T)` | 保存设置 |
| `ResetToDefaultsAsync()` | 重置为默认值 (Theme=Light, Language=zh-CN, AutoSave=true) |
| `HasSetting(string)` | 检查设置是否存在 |

依赖: `ILogger`

---

### Security/ - 安全基础设施

#### IAuthenticationService.cs / AuthenticationService.cs
接口 `IAuthenticationService` + 类 `AuthenticationService : IAuthenticationService` -- 认证服务

| 方法 | 说明 |
|------|------|
| `IsLoggedInAsync()` | 检查是否已登录 (Token 非空) |
| `LoginAsync(LoginRequest)` | 用户登录，调用 IAuthApi |
| `LogoutAsync()` | 用户登出，Token 过期时跳过服务端调用 |
| `GetCurrentUserAsync()` | 异步获取当前用户 |
| `GetCurrentUser()` | 同步获取当前用户 (避免 WPF 死锁) |
| `GetToken()` | 同步获取当前 Token |
| `ValidateTokenAsync(string)` | 客户端 JWT 自验证 (Issue #1864) |
| `ClearAuthInfo()` | 同步清除认证信息 |
| `CheckConnectionAsync()` | 检查连接状态 (本地 Token + 健康检查 API) |
| `LoginWithAutoTokenAsync(AutoLoginRequest)` | AutoLoginToken 自动登录 |

依赖: `IAuthApi`, `ITokenStorageService`, `ITokenValidator`, `ICredentialVault`, `ILogger`

#### AuthenticationStateMachine.cs
类 `AuthenticationStateMachine : IAuthenticationStateMachine` -- 转换表驱动认证状态机 (11 个状态, 线程安全)

| 方法 | 说明 |
|------|------|
| `CurrentState` | 属性: 当前认证状态 |
| `IsAuthenticated` | 属性: 是否已认证 |
| `IsTransitioning` | 属性: 是否在过渡状态 |
| `StatusMessage` | 属性: 当前状态消息 |
| `CanFire(AuthEvent)` | 检查当前状态是否允许触发指定事件 |
| `Fire(AuthEvent, string?)` | 触发状态转换 |
| `FireAsync(AuthEvent, string?)` | 异步触发 (包装同步方法) |
| `Reset()` | 重置到 Idle 状态 |
| `GetPermittedEvents()` | 获取当前状态允许的事件列表 |
| `ForceState(AuthState, string?)` | internal: 强制设置状态 (仅恢复场景) |

类 `AuthStateChangedPubSubEvent : PubSubEvent<AuthStateChangedEventArgs>` -- Prism PubSubEvent 跨模块状态变更通知

依赖: `ILogger`, `IEventAggregator?`

#### AuthEvents.cs
静态类 `AuthEvents` -- 认证事件定义 (9 个 PubSubEvent + 对应 Payload record)

嵌套事件类:
- `LoginSucceededEvent` -> `LoginSucceededPayload` (User, TokenExpiresAt, IsAutoLogin)
- `LoginFailedEvent` -> `LoginFailedPayload` (Reason: LoginFailureReason 枚举 8 种)
- `LogoutCompletedEvent` -> `LogoutCompletedPayload` (Local/Server 分离状态)
- `ServerLogoutFailedEvent` -> `ServerLogoutFailedPayload` (重试队列信息)
- `PendingLogoutsClearedEvent` -> `PendingLogoutsClearedPayload`
- `PasswordChangedEvent` -> `PasswordChangedPayload`
- `TokenRefreshSucceededEvent` -> `TokenRefreshSucceededPayload`
- `TokenRefreshFailedEvent` -> `TokenRefreshFailedPayload`
- `SessionExpiredEvent` -> `SessionExpiredPayload` (SessionExpiredReason 枚举 4 种)

枚举 `LoginFailureReason` (8 值) / `SessionExpiredReason` (4 值) -- 同文件定义

#### ICredentialVault.cs / CredentialVault.cs
接口 `ICredentialVault` + 类 `CredentialVault : ICredentialVault` -- DPAPI 加密凭据保险库

| 方法 | 说明 |
|------|------|
| `SavePasswordAsync(string, string)` | 保存密码 (DPAPI 加密) |
| `GetPasswordAsync(string)` | 获取已保存的密码 |
| `HasSavedPasswordAsync(string)` | 检查是否存在已保存密码 |
| `ClearPasswordAsync(string)` | 清除已保存密码 |
| `SaveAutoLoginTokenAsync(string, string)` | 保存 AutoLoginToken |
| `GetAutoLoginTokenAsync(string)` | 获取 AutoLoginToken (含 HMAC 完整性校验) |
| `ClearCredentialsAsync(string?)` | 清除凭据 (null=清除所有) |
| `VerifyIntegrityAsync(string)` | 验证数据完整性 (HMAC 校验) |
| `MigrateOldFormatAsync()` | 迁移旧格式 credentials.dat 到 vault.dat |
| `HasValidTokenAsync(string)` | 检查是否存在有效 AutoLoginToken |

存储: `%LOCALAPPDATA%\LYBT\Desktop\vault.dat`，私有数据结构 VaultStorage/VaultEntry/OldCredentialFormat

依赖: `ILogger`

#### ILogoutService.cs / LogoutService.cs
接口 `ILogoutService` + 类 `LogoutService : ILogoutService, IDisposable` -- 可靠登出服务

| 方法 | 说明 |
|------|------|
| `LogoutAsync()` | 完整登出流程: 本地立即生效 + 服务端异步执行 |
| `ExecuteLocalLogoutAsync()` | 仅本地登出: 清除 Token 和会话状态 |
| `ProcessPendingServerLogoutsAsync()` | 处理重试队列中的服务端登出 |
| `PendingServerLogoutCount` | 属性: 待处理的服务端登出数量 |

record `LogoutResult` -- 登出结果 (Success, LocalLogoutCompleted, ServerLogoutCompleted, ServerLogoutQueued)，含工厂方法 FullSuccess/LocalSuccessServerQueued/LocalSuccessOnly

类 `ServerLogoutFailedEventArgs : EventArgs` -- 服务端登出失败事件参数

枚举 `ServerLogoutFailureReason` (6 值) -- Unknown/NetworkUnavailable/ServerError/Timeout/TokenInvalid/MaxRetriesExceeded

依赖: `ITokenStorageService`, `IAuthApi`, `IAuthenticationStateMachine`, `IEventAggregator?`, `ILogger`

#### ITokenStorageService.cs / TokenStorageService.cs
接口 `ITokenStorageService` + 类 `TokenStorageService : ITokenStorageService` -- 内存级 Token 存储 (Session 级别)

| 方法 | 区域 | 说明 |
|------|------|------|
| `SaveAuthenticationAsync(LoginResponse, bool)` | 异步 | 保存认证信息到内存 (rememberMe 参数被忽略) |
| `GetTokenAsync()` | 异步 | 获取 AccessToken |
| `GetRefreshTokenAsync()` | 异步 | 获取 RefreshToken |
| `GetLoginResponseAsync()` | 异步 | 获取完整登录响应 |
| `ClearAuthenticationAsync()` | 异步 | 清除认证信息 |
| `IsTokenExpiredAsync()` | 异步 | 检查 Token 是否过期 (含 5 分钟缓冲) |
| `GetToken()` | 同步 | 属性访问用 |
| `GetLoginResponse()` | 同步 | 属性访问用 |
| `ClearAuthentication()` | 同步 | 同步清除 |

依赖: `ILogger`

#### ITokenManager.cs / TokenManager.cs
接口 `ITokenManager` + 类 `TokenManager : ITokenManager` -- 同步 Token 管理器

| 方法 | 说明 |
|------|------|
| `AccessToken` | 属性: 当前 AccessToken |
| `RefreshToken` | 属性: 当前 RefreshToken |
| `AccessTokenExpiry` | 属性: 过期时间 |
| `SetTokens(string, string, DateTime)` | 设置 Token |
| `ClearTokens()` | 清除 Token |
| `IsTokenValid()` | 检查 Token 是否有效 |
| `IsTokenExpiringSoon(TimeSpan)` | 检查是否即将过期 |

**疑似死代码**: 已注册为 Singleton (ServiceCollectionExtensions)，但 `src/` 中无任何 ViewModel 或 Service 注入使用。与 ITokenStorageService 功能重叠。

依赖: `ILogger`

#### ITokenValidator.cs / LocalTokenValidator.cs
接口 `ITokenValidator` + 类 `LocalTokenValidator : ITokenValidator` -- 客户端 JWT 自验证

| 方法 | 说明 |
|------|------|
| `ValidateTokenAsync(string)` | 验证 JWT Token (签名/Issuer/Audience/Expiration/Claims) |
| `ValidateAndGetUserInfoAsync(string)` | 验证并提取用户信息 |

类 `TokenValidationResult` -- 验证结果 (IsValid, ErrorMessage, UserInfo)

类 `TokenUserInfo` -- Token 中的用户信息 (UserId, UserName, Role, UserType)

依赖: `IConfiguration` (Lybt:Jwt 节), `ILogger`

#### ITokenLifecycleService.cs / TokenLifecycleService.cs
接口 `ITokenLifecycleService : IDisposable` + 类 `TokenLifecycleService : ITokenLifecycleService` -- Token 生命周期监控

| 方法 | 说明 |
|------|------|
| `CurrentState` | 属性: 当前生命周期状态 |
| `RemainingTime` | 属性: 剩余有效时间 |
| `WarningThreshold` | 属性: 警告阈值 (默认 5 分钟) |
| `StartMonitoring(DateTime)` | 启动监控 (30 秒间隔定时器) |
| `StopMonitoring()` | 停止监控 |
| `UpdateExpiration(DateTime)` | 更新过期时间 (Token 刷新后) |
| `TryRefreshTokenAsync()` | 尝试刷新 Token (调用 IAuthApi) |
| `Reset()` | 重置为未认证状态 |

依赖: `IAuthApi`, `ITokenStorageService`, `IEventAggregator`, `ILogger`

#### ITokenRefreshHandler.cs
接口 `ITokenRefreshHandler` -- Token 刷新处理器接口

| 方法 | 说明 |
|------|------|
| `RefreshTokenAsync()` | 主动刷新 Token |

类 `TokenRefreshFailedEventArgs : EventArgs` -- Token 刷新失败事件参数，含 6 个静态工厂方法 (NetworkError/RefreshTokenExpired/RefreshTokenRevoked/RefreshTokenInvalid/ServerError/UserDisabled)

类 `TokenRefreshResult` -- Token 刷新结果 (Success/FailureReason/ErrorMessage)，含工厂方法 Succeeded/Failed

#### TokenRefreshFailureReason.cs
枚举 `TokenRefreshFailureReason` (8 值) -- Unknown/NetworkError/RefreshTokenExpired/RefreshTokenRevoked/RefreshTokenInvalid/ServerError/UserDisabled/NotLoggedIn

#### TokenLifecycleState.cs
枚举 `TokenLifecycleState` (4 值) -- NotAuthenticated/Active/Warning/Expired

#### TokenLifecycleStateChangedEvent.cs
类 `TokenLifecycleStateChangedEvent : PubSubEvent<TokenLifecycleStateChangedEventArgs>` -- Prism PubSubEvent

类 `TokenLifecycleStateChangedEventArgs` -- 状态变更事件参数 (PreviousState, CurrentState, RemainingTime, Timestamp, RequiresUserInteraction, RequiresReLogin)

#### IUsernameStorageService.cs / UsernameStorageService.cs
接口 `IUsernameStorageService` + 类 `UsernameStorageService : IUsernameStorageService` -- JSON 文件存储用户名

| 方法 | 说明 |
|------|------|
| `SaveUsernameAsync(string, bool)` | 保存用户名 (rememberMe=false 时删除文件) |
| `GetSavedUsernameAsync()` | 获取已保存的用户名 (内存缓存优先) |
| `IsRememberMeEnabledAsync()` | 检查是否启用记住用户名 |
| `ClearUsernameAsync()` | 清除已保存的用户名 |

存储: `%LOCALAPPDATA%\LYBT\Desktop\username.json`，私有数据结构 UsernameStorage

依赖: `ILogger`

---

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| `ApiService<TEntity>` (泛型版本) | [已清理] 2026-03-01 | 各模块使用 Refit 接口 (IAuthApi 等) | 已移除 |
| `TokenEvents` (文件 + 嵌套事件类 4 个) | [已清理] 2026-03-01 | 实际发布使用 AuthEvents 中同名事件 | 已移除 |
| `ITokenManager` / `TokenManager` | 疑似死代码 | ITokenStorageService 覆盖同等功能 | 确认 OpenSpec 计划，决定保留或移除 |
| `StartupOptimizationService.LogStartupMetrics()` | 疑似死代码 | 不在接口定义，仅本文件引用 | 移除或添加到接口 |
| `IStartupOptimizationService.OptimizationCompleted` | 从未订阅 | 已 #pragma disable | 移除或实现 |
| `RetryPolicyOptions.EnableRetry/EnableCircuitBreaker/EnableTimeout` | 未使用 | ApiService 构造函数不检查这些开关 | 实现开关逻辑或移除属性 |

---

## 设计分析

### ITokenStorageService vs ITokenManager 双接口问题

项目中存在两个功能重叠的 Token 管理接口:

- **ITokenStorageService**: 遗留接口，异步+同步方法，管理完整 LoginResponse (含 Token/RefreshToken/UserInfo)。被 AuthenticationService、AuthorizationMessageHandler、TokenRefreshHandler、LogoutService、TokenLifecycleService 等广泛使用。
- **ITokenManager**: 新接口 (OpenSpec: refactor-login-authentication)，纯同步方法，仅管理 Token 字符串+过期时间。已注册为 Singleton 但无实际消费者。

建议: ITokenManager 应作为 ITokenStorageService 的最终替代方案推进，或者在确认不再需要时移除。

### 安全设计: 三层凭据存储

1. **TokenStorageService** -- 内存级，Session Token (JWT)，进程退出即清除
2. **CredentialVault** -- 文件级，DPAPI 加密 AutoLoginToken + 密码，持久化到 vault.dat
3. **UsernameStorageService** -- 文件级，明文 JSON 存储用户名，持久化到 username.json

三者职责清晰分离: Token 是会话级数据不持久化 (医疗合规)，凭据用 DPAPI+HMAC 加密持久化 (自动登录)，用户名是非敏感数据直接 JSON 存储。

---

## 模块演进记录

- **Issue #1907**: Token 改为内存存储，移除文件持久化 (医疗合规)
- **Issue #1863/1864**: Token 认证安全重构，实现客户端 JWT 自验证
- **Issue #1838**: Token 自动刷新处理器
- **Issue #1823**: API 健康检查前置优化
- **Issue #1906**: 密码修改成功事件，PasswordChangedEvent 导航到登录界面
- **Issue #1262**: 移除对 500 InternalServerError 的重试
- **Issue #861**: 用户名存储服务
- **Issue #2262**: ChangePasswordAsync 从 AuthenticationService 移除，职责分离到 IUserRepository
- **Issue #1114**: Foundation 层从 Infrastructure 拆分 (Phase 1)

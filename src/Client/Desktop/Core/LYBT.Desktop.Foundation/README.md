# LYBT.Desktop.Foundation

> Contracts 接口的运行时实现层 — HTTP 通信、安全认证、配置、缓存、健康监控、模块加载

## 项目定位

- **层级**: Core
- **职责**: 实现 Contracts 层所有接口，提供 Remote/Local 双模式运行时基础设施
- **状态**: Active
- **设计依据**: Foundation 位于 Contracts（纯接口）和 Infrastructure（WPF 服务）之间，提供两种模式共享的运行时管道

## 目录结构

```
LYBT.Desktop.Foundation/
├── Http/                   # API 客户端基础设施
│   ├── SwitchingApiClient  # 运行时可切换的 API 客户端代理
│   ├── RefitApiClient      # 远程模式 (Refit)
│   ├── HttpClientApiClient # 本地模式 (HttpClient)
│   ├── Clients/            # 领域 API 适配器 (Auth, User, Patient, Herb, Formula, MedicalCase, Registration, Reports)
│   ├── AuthorizationMessageHandler  # 自动注入 Bearer Token
│   ├── TokenRefreshHandler          # 自动刷新 Token
│   ├── ApiService                   # 通用 API 服务 (缓存+重试+熔断)
│   └── ApiErrorHandler              # 统一错误处理
├── Security/               # 认证与 Token 管理
│   ├── AuthenticationStateMachine   # 11 状态认证状态机
│   ├── AuthenticationService        # 认证服务
│   ├── TokenStorageService          # 内存 Token 存储
│   ├── TokenLifecycleService        # Token 生命周期监控
│   ├── LogoutService                # 可靠登出
│   ├── CredentialVault              # DPAPI 加密凭证存储
│   └── LocalTokenValidator          # 本地 JWT 验证
├── Modules/                # Prism 模块加载
├── Services/               # 连接管理
├── Caching/                # 缓存管理
├── Application/            # 应用状态
├── HealthCheck/            # API 健康检查
├── Performance/            # 启动性能优化
├── Settings/               # 设置服务
└── Utilities/              # Excel 工具
```

## 核心组件

### Http/ — API 客户端基础设施

#### `SwitchingApiClient : IApiClient, IDisposable`
**设计依据**: 运行时代理，根据连接 URL 自动切换底层实现。`localhost`/`127.0.0.1` → `HttpClientApiClient`（本地），其他 → `RefitApiClient`（远程）。缓存活动客户端，URL 变更时销毁旧客户端。线程安全（lock）。

| 方法 | 说明 |
|------|------|
| `Auth` / `Users` / `Patients` / ... | 委托给当前活动客户端 |
| `Dispose()` | 释放当前客户端 |

#### `AuthorizationMessageHandler : DelegatingHandler`
**设计依据**: 自动为所有请求添加 `Bearer` Token，匿名端点除外（`/health`, `/api/auth/login`, `/api/auth/refresh`）。

#### `TokenRefreshHandler : DelegatingHandler`
**设计依据**: 检测 Token 过期（5 分钟阈值），自动通过 `POST /api/v1/auth/refresh` 刷新。`SemaphoreSlim` 防止并发刷新。指数退避重试（3 次）。刷新失败时降级到 `AutoLogin`。仅在用户活跃时刷新（滑动过期）。

#### `ApiService : IApiService`
**设计依据**: 通用 API 服务，内置缓存（GET 请求 5 分钟绝对过期）、请求去重、Polly 重试 + 熔断器 + 超时策略。自动解包 `ApiResponse<T>` 信封。

| 方法 | 说明 |
|------|------|
| `GetAsync<T>(url)` | GET 请求（带缓存） |
| `PostAsync<TReq, TRes>(url, body)` | POST 请求 |
| `PutAsync<TReq, TRes>(url, body)` | PUT 请求 |
| `DeleteAsync(url)` | DELETE 请求 |
| `DownloadAsync(url)` | 文件下载 |
| `UploadAsync<T>(url, content)` | 文件上传 |

#### `RetryPolicyExtensions` (static)
**设计依据**: Polly 策略工厂。组合策略：重试 → 熔断器 → 超时。仅重试瞬态错误（502, 503, 504, 408, 429），不重试 500（非幂等 POST 安全性）。

### Security/ — 认证与 Token 管理

#### `AuthenticationStateMachine : IAuthenticationStateMachine`
**设计依据**: 表驱动状态机，25 个转换。线程安全（lock）。事件在 lock 外发布避免死锁。`ForceState()` 用于恢复场景。

| 状态 | 说明 |
|------|------|
| `Idle` | 初始状态 |
| `Authenticating` | 正在认证 |
| `ValidatingToken` | 验证 Token |
| `LoadingProfile` | 加载用户资料 |
| `LoadingModules` | 加载模块 |
| `Navigating` | 导航到主页 |
| `Authenticated` | 已认证 |
| `Failed` | 认证失败 |
| `LoggingOut` | 正在登出 |
| `SessionExpired` | 会话过期 |
| `RefreshingToken` | 刷新 Token |

#### `AuthenticationService : IAuthenticationService`
**设计依据**: ADR-0002 合规。直接调用 HTTP API（无服务器端服务依赖）。使用本地 JWT 验证（Issue #1864）而非服务器 API 验证 Token。

#### `TokenStorageService : ITokenStorageService`
**设计依据**: 内存级会话存储。医疗系统安全要求：进程退出 = 自动清除 Token。无磁盘持久化。线程安全。

#### `TokenLifecycleService : ITokenLifecycleService`
**设计依据**: 状态机（NotAuthenticated → Active → Warning → Expired）。30 秒监控间隔。Warning 状态自动刷新。发布 `TokenLifecycleStateChangedEvent`。

#### `CredentialVault : ICredentialVault`
**设计依据**: DPAPI 加密凭证存储，路径 `%LOCALAPPDATA%\LYBT\Desktop\vault.dat`。HMAC-SHA256 完整性验证。多用户支持。篡改检测清除受损数据。

#### `LogoutService : ILogoutService`
**设计依据**: 可靠登出，本地/服务器分离。本地登出始终成功（清除内存）。服务器登出可重试（`ConcurrentQueue<PendingServerLogout>`）。失败的服务器登出排队等待重试。

### Modules/ — Prism 模块加载

#### `ModuleLoadingService : IModuleLoadingService`
**设计依据**: 线程安全模块加载，去重（`HashSet`）。事件在 lock 外触发（UltraThink 修复）。单个模块失败继续加载其余模块。

### Services/ — 连接管理

#### `ConnectionModeService : IConnectionModeService`
**设计依据**: 封装 URL 设置 + 健康探测。`SemaphoreSlim` 序列化检测。Auto 模式：先探测远程，降级到本地。订阅 `UrlChanged` 重新推导模式。

### Caching/

#### `DesktopCacheManager : IDesktopCacheManager`
**设计依据**: 按 URL 前缀模式清除 `IMemoryCache`，发布 `CacheEvents.InvalidatedEvent` 供模块级缓存订阅者响应。

### Utilities/

#### `ExcelHelper` (static)
**设计依据**: NPOI 实现，泛型类型映射。自动检测 `[Display]` 和 `[DisplayName]` 属性作为列头。支持类型转换（enum, DateTime, bool, 数值）。

| 方法 | 说明 |
|------|------|
| `ExportToExcel<T>(data, columns, filePath)` | 导出 Excel |
| `ImportFromExcel(filePath, hasHeader)` | 导入 Excel |
| `CreateTemplate(columns, filePath)` | 创建模板 |

## 依赖关系

- **依赖**: `LYBT.Desktop.Contracts`（接口定义）, `LYBT.Shared.Models`（DTO）
- **被依赖**: Infrastructure, Modules, Shell

## 设计决策

| 决策 | 原因 | 日期 |
|------|------|------|
| SwitchingApiClient 透明切换 | 仓储层无感知 Remote/Local 模式差异 | 2025-12 |
| Token 仅内存存储 | 医疗系统安全：进程退出自动清除 | 2025-12 |
| DPAPI 加密凭证 | Windows 原生加密，无需外部密钥管理 | 2025-12 |
| 本地 JWT 验证 | 减少服务器调用，离线模式可用 | 2026-01 |
| Polly 组合策略 | 重试→熔断→超时，不重试 500 | 2025-12 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| Token 刷新并发 | 多个请求同时触发刷新 | SemaphoreSlim 单次刷新 |
| 旧 HttpClient 泄漏 | URL 切换时未释放 | SwitchingApiClient 自动 Dispose |
| DPAPI 跨用户不可用 | DPAPI 绑定 Windows 用户 | 每用户独立 vault.dat |

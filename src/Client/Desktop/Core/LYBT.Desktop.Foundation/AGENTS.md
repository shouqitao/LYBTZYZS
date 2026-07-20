# LYBT.Desktop.Foundation - Desktop Foundation Layer

**Purpose**: HTTP client, security, configuration for Desktop client.

## Structure

```
LYBT.Desktop.Foundation/
├── Security/            # 24 security-related files (JWT, tokens, encryption)
├── Http/                # HTTP client setup, RetryPolicyExtensions
├── Application/         # Application lifecycle
├── Caching/             # Cache services
├── HealthCheck/         # Health check integration
├── Modules/             # Module loading
├── Performance/         # Performance monitoring
├── Settings/            # Application settings
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Security | `Security/` | JWT handling, token storage, encryption |
| HTTP client | `Http/` | Refit client setup, retry policies |
| Retry policies | `Http/RetryPolicyExtensions.cs` | HTTP retry configuration |

## CONVENTIONS

- **Refit HTTP** — All API calls through Refit interfaces
- **Token management** — JWT + RefreshToken + AutoLoginToken lifecycle

## ANTI-PATTERNS

- **Direct HttpClient** — Use Refit interfaces from Contracts layer
- **Hardcoded URLs** — Use configuration for API endpoints

## 职责边界

**Foundation = 无头运行时层**，不依赖 WPF/Prism。

| 文件夹 | 职责 |
|--------|------|
| Http/ | HTTP 客户端（SwitchingApiClient, Refit clients, ApiClients） |
| Security/ | Token 管理、认证状态机、凭证存储 |
| Caching/ | 桌面端缓存管理 |
| HealthCheck/ | API 健康检查 |
| Application/ | 应用状态服务 |
| Modules/ | 模块加载服务 |
| Repositories/ | API 客户端仓储基类 |
| Services/ | 连接模式服务 |

**规则**：Foundation 中的代码不得引用 WPF 类型（Window, UserControl, DependencyObject 等）。

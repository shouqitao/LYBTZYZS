# 设计 05：HttpClient 池化与弹性（AddHttpClient + Polly）

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：审计报告 L2-02 / L2-08 · 属**技术引入（T3）**，须先过 `docs/00-governance/03-technical-adoption-governance.md`

## 1. 问题描述（实测）

| 项 | 现状 | 证据 |
|---|---|---|
| `AddHttpClient` | 桌面端 **0 处** | 全仓 grep |
| `Microsoft.Extensions.Http` | Foundation **已引用但未使用** | `LYBT.Desktop.Foundation.csproj:33` |
| `Microsoft.Extensions.Http.Polly` / `Polly` | 仅中央版本声明（8.0.26 / 8.8.6），**无项目引用** | `Directory.Packages.props:79,152` |
| `IHttpClientFactory` 注册 | **无**：`container.IsRegistered<IHttpClientFactory>()` 恒 false → `TokenRefreshHandler` 的「Polly + 15s」工厂分支是**死代码**，注释宣称的弹性策略不成立 | `UnifiedApiClientExtensions.cs:79-81` |
| 重试/熔断/连接池 | 无退避重试、无熔断、无 `PooledConnectionLifetime` | — |
| 手工 `new HttpClient` | 2 处（`TokenRefreshHandler` 回退链、`UnifiedApiClientExtensions` 远程工厂），每次模式切换重建整条 handler 链 | 同上 |

### 关键约束（决定方案形态）

**桌面 DI 是 Prism.DryIoc（`IContainerRegistry`），不是 Microsoft.Extensions.DependencyInjection。** 因此**不能**直接 `services.AddHttpClient(...)` 注册进主容器。

## 2. 设计方案

**在 `AddUnifiedApiClient` 内部构建一个「私有 MS DI 容器」，只用于生产 `IHttpClientFactory`。**

```
var internalServices = new ServiceCollection();
internalServices.AddLogging();
internalServices.AddHttpClient("RemoteApi", c => { BaseAddress; Timeout })
                .ConfigurePrimaryHttpMessageHandler(...)
                .SetHandlerLifetime(TimeSpan.FromMinutes(2))
                .AddHttpMessageHandler(sp => ... AuthorizationMessageHandler)
                .AddStandardResilienceHandler();   // 或 AddPolicyHandler(...) 自定义
internalServices.AddHttpClient("RefreshToken", ...);
internalServices.AddHttpClient("LocalApi", ...).ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler { PooledConnectionLifetime = ... });
var provider = internalServices.BuildServiceProvider();
containerRegistry.RegisterInstance<IHttpClientFactory>(provider.GetRequiredService<IHttpClientFactory>());
```

- 远程/本地工厂改为从该 `IHttpClientFactory.CreateClient("RemoteApi"/"LocalApi")` 取客户端（保留现有 `AuthorizationMessageHandler`/`TokenRefreshHandler`/`LoggingHttpHandler` 语义）。
- `TokenRefreshHandler` 的 `httpClientFactory` 分支由死代码变为活路径（`CreateClient("RefreshToken")`）。
- 本地模式 client 名改为按当前 `baseUrl` 动态配置——`AddHttpClient(name).ConfigureHttpClient(c => c.BaseAddress = ...)` 在模式切换后重建。

### 需新增的包引用（T3）

| 包 | 版本（中央已声明） | 用途 |
|---|---|---|
| `Microsoft.Extensions.DependencyInjection` | 8.0.x | `ServiceCollection` / `BuildServiceProvider` |
| `Microsoft.Extensions.Http.Polly` | 8.0.26 | `AddPolicyHandler`（含 Polly） |

## 3. 影响范围

`LYBT.Desktop.Foundation.csproj`、`Shell/Extensions/UnifiedApiClientExtensions.cs`、`Foundation/Http/TokenRefreshHandler.cs`（去掉死分支与失真注释）、`Foundation/README.md` + `AGENTS.md`（纠正已不存在的 `ApiService`/`RetryPolicyExtensions` 描述）。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| 技术引入治理未走（新增 2 个包） | **高** | 先出 ADR + 技术引入记录，用户已在本任务中明确要求该项 |
| **重试语义与写操作冲突**（POST 重试可能重复创建） | **高** | 重试仅对**幂等**方法（GET/PUT/DELETE）启用；POST 仅重试连接层失败（Polly 的 `HttpRequestException` 且未收到响应）或**不重试**；明确写进策略 |
| 私有容器与主容器生命周期不同步（Dispose 顺序） | 中 | 由 `SwitchingApiClient.Dispose` / Shell 退出时释放 `provider` |
| 与既有「模式切换重建整条链」语义冲突 | 中 | 工厂按名 + 每模式重建；`SetHandlerLifetime` 由工厂管理，不手工 `Dispose` handler |
| 包体增大 / Runtime 依赖增加 | 低 | Desktop-only 引用 |

## 5. 实施步骤

1. ADR + 技术引入记录（依据四标准：许可/职责/维护活跃/无重叠）。
2. Foundation 加包引用；构建私有 `IHttpClientFactory`。
3. 三个具名客户端（RemoteApi / LocalApi / RefreshToken）+ 弹性策略（幂等限定）。
4. `TokenRefreshHandler` 接线到具名客户端；删除死分支与失真注释。
5. 文档纠正（Foundation/README、AGENTS）。
6. build + 模式切换 E2E（`ModeSwitchE2ETests`）+ 传输契约测试。

# ADR: 桌面 HTTP 传输层池化与弹性（引入 IHttpClientFactory + Polly）

> 状态: Accepted | 日期: 2026-09-16 | 相关: 设计 01/05（架构优化设计稿（已清理，git 历史可查））、ADR-0021（SwitchingApiClient 生命周期）、`docs/00-governance/03-technical-adoption-governance.md`

## 背景

- 桌面客户端全仓 **0 处 `AddHttpClient`**；`Microsoft.Extensions.Http` 已在 `LYBT.Desktop.Foundation` 引用但从未使用；`Microsoft.Extensions.Http.Polly`(8.0.26) 与 `Polly`(8.6.6) 仅存在于 `Directory.Packages.props`，无项目引用。
- `TokenRefreshHandler` 内 `container.IsRegistered<IHttpClientFactory>()` 恒为 false → 「Polly + 15s Timeout」分支是**死代码**，注释与实现不符（架构审查 L2-02）。
- 无重试/退避/熔断、无 `PooledConnectionLifetime`、无 handler 生命周期回收；模式切换时手工重建整条 handler 链且不释放。
- 关键约束：**桌面主容器是 Prism.DryIoc（`IContainerRegistry`），不是 Microsoft.Extensions.DependencyInjection**，不能直接 `services.AddHttpClient(...)` 注册进主容器。

## 决策

1. **引入 `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Http.Polly`**（版本沿用中央声明）到 `LYBT.Desktop.Foundation`（Desktop-only，不影响 Server/LocalWebAPI）。
2. **构建私有传输容器**：`DesktopHttpTransportFactory.Build(...)` 用一个仅含传输层的 `ServiceCollection`（`AddLogging` + 3 个具名 `AddHttpClient`）产出 `IHttpClientFactory`，再由 Shell 层 `RegisterInstance<IHttpClientFactory>` 注入 DryIoc。Foundation 不依赖 DryIoc —— 自定义 handler 经 `IDesktopHttpHandlerProvider` 注入，实现位于 Shell（`DryIocHttpHandlerProvider`）。
3. **具名客户端与链**（首个 `AddHttpMessageHandler` 为最外层）：
   - `RemoteApi`：Logging → Caching → Authorization → TokenRefresh → SocketsHttpHandler
   - `LocalApi`：Caching → Authorization → SocketsHttpHandler（**不含** TokenRefresh：本地续期由 Shell `TokenLifecycleService` 显式驱动，TokenRefreshHandler 固定打向远程地址）
   - `RefreshToken`：裸链，30s 超时
4. **池化与生命周期**：`SocketsHttpHandler` + `PooledConnectionLifetime = 2min` + `PooledConnectionIdleTimeout = 1min`；`SetHandlerLifetime(2min)` 由工厂回收 handler 链。撤销原先「调用方持有并手工装配 handler 链」的模式。
5. **弹性策略**：`HandleTransientHttpError().OrResult(429)` + 3 次指数退避（200/400/800ms），**仅对幂等方法**（GET/HEAD/OPTIONS/PUT/DELETE）启用；`POST`/`PATCH` 返回 `Policy.NoOpAsync` —— 避免「服务端已成功但响应丢失」时重试导致重复创建。
6. `IHttpClientFactory` 进入主容器后，`TokenRefreshHandler` 的工厂分支由死代码变为活路径，其刷新请求经 `RefreshToken` 具名客户端发出。

## 后果

- **正向**：连接池化与 DNS/服务端变更自适应；瞬时故障对读操作自愈；handler 链由工厂回收（消除模式切换泄漏）；`HttpClient` 由工厂产出后，调用方 `using` 释放包装符合 `IHttpClientFactory` 契约。
- **代价**：Desktop 增加 2 个包引用；存在一个仅用于传输的二级 `ServiceProvider`（其 `ILoggerFactory` 与主日志管道相互独立，仅用于内部管道日志）。
- **风险与缓解**：POST 不重试（已按方法限定）；handler 由工厂管理，禁止再手工 `Dispose`/设置 `InnerHandler`。

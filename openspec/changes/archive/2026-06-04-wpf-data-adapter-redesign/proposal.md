## Why

当前 WPF 桌面端数据对接层存在双模式（远程 WebAPI / 本地 LocalWebAPI）下两套 HTTP 客户端实现（Refit vs 原生 HttpClient），导致代码重复、维护成本高、模式切换不够统一。随着 LocalWebAPI 覆盖率已达 100%，需要重新设计统一的数据对接层，实现"接口统一、实现可插拔"的架构。

## What Changes

- **统一 API 客户端抽象层**：创建 `IApiClient` 接口，统一 Refit (Remote) 和 HttpClient (LocalWebAPI) 两种实现
- **HttpClient 工厂化**：引入 `IHttpClientFactory` 统一管理连接池、超时、重试、BaseAddress 配置
- **消除 HttpXxxRepository 重复代码**：LocalWebAPI 的 HttpXxxRepository 从直接使用 HttpClient 改为通过统一 `IApiClient` 接口调用
- **配置驱动的模式切换**：通过 `appsettings.json` 中的 `ApiMode` 配置决定使用哪种 API Client 实现，DI 层自动解析
- **统一错误处理管道**：将 Refit ApiException 和 HttpClient 的 HttpRequestException 统一映射为 `ServiceResult<T>`，消除两种模式下的错误处理差异
- **统一认证注入**：HttpClient 和 Refit 统一通过 `DelegatingHandler` 管道注入 JWT Token，而不是分散在 Repository 中手动添加 Header

## Capabilities

### New Capabilities
- `unified-api-client`: 统一的 API 客户端抽象层，支持 Remote 和 LocalWebAPI 两种实现

### Modified Capabilities
<!-- None - this is a new architecture component, not modifying existing specs -->

## Impact

- **Affected code**: `LYBT.Desktop.Contracts` (IApi 接口), `LYBT.Desktop.Foundation` (HttpClient 配置), `LYBT.Desktop.Infrastructure` (DI 注册), 所有模块的 Repository 实现, `LocalWebAPI` 项目的 HttpXxxRepository
- **Breaking changes**: HttpXxxRepository 需要重构为通过统一 IApiClient 调用；Refit IApi 接口需统一到 IApiClient 抽象下
- **Dependencies**: 移除对各 Repository 层分散的 HttpClient 配置；新增依赖 `Microsoft.Extensions.Http` (IHttpClientFactory)
